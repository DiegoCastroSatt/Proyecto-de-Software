using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.AgendaReservas.Interfaces;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Clientes.Services;
using System.Net.Mail;
public class ReservaService : IReservaService
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IReservaCorreoService _correoService;
    private readonly TimeProvider _proveedorTiempo;
    private readonly ILogger<ReservaService> _logger;

    public ReservaService(
        IReservaRepository reservaRepository,
        IReservaCorreoService correoService,
        TimeProvider proveedorTiempo,
        ILogger<ReservaService> logger)
    {
        _reservaRepository = reservaRepository;
        _correoService = correoService;
        _proveedorTiempo = proveedorTiempo;
        _logger = logger;
    }

    public async Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto) || string.IsNullOrWhiteSpace(dto.Rut) || dto.IdHorario <= 0)
        {
            throw new ArgumentException("Los datos del cliente y el horario son obligatorios.");
        }

        if (!RutChilenoValidator.EsValido(dto.Rut))
        {
            throw new ArgumentException("El RUT o su dígito verificador no es válido.");
        }

        if (!TelefonoChilenoValidator.EsValido(dto.Telefono))
        {
            throw new ArgumentException("El teléfono debe ser un celular válido de 9 dígitos; puedes incluir el prefijo +56.");
        }

        if (string.IsNullOrWhiteSpace(dto.Correo))
        {
            throw new ArgumentException("El correo electrónico es obligatorio para enviar la confirmación de la reserva.");
        }
        try
        {
            var correo = new MailAddress(dto.Correo.Trim());
            if (!string.Equals(correo.Address, dto.Correo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("El correo electrónico no tiene un formato válido.");
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("El correo electrónico no tiene un formato válido.");
        }

        var rutNormalizado = RutChilenoValidator.Normalizar(dto.Rut);
        if (!string.IsNullOrWhiteSpace(dto.Correo)
            && await _reservaRepository.ExisteCorreoEnOtroCliente(dto.Correo, rutNormalizado))
        {
            throw new ArgumentException("Ese correo ya está registrado con otro cliente. Ingresa otro correo o revisa el RUT.");
        }

        var tokenConfirmacion = TokenAccionReserva.Crear();
        var tokenCancelacion = TokenAccionReserva.Crear();
        var reservaCreada = await _reservaRepository.CrearReserva(
            dto,
            TokenAccionReserva.ObtenerHash(tokenConfirmacion),
            TokenAccionReserva.ObtenerHash(tokenCancelacion));

        var correoEnviado = false;
        try
        {
            var detalleCorreo = await _reservaRepository.ObtenerDatosCorreoReserva(reservaCreada.Id, CancellationToken.None);
            correoEnviado = detalleCorreo is not null
                && await _correoService.EnviarReservaCreada(detalleCorreo, tokenConfirmacion, tokenCancelacion, CancellationToken.None);
        }
        catch (Exception)
        {
            _logger.LogError("La reserva {ReservaId} se guardó, pero no se pudo preparar o enviar su correo inicial.", reservaCreada.Id);
        }

        return new ReservaResponseDto
        {
            Id = reservaCreada.Id,
            IdHorario = reservaCreada.HorarioId,
            Fecha = reservaCreada.Fecha,
            Hora = reservaCreada.Hora,
            Estado = reservaCreada.Estado,
            CorreoEnviado = correoEnviado
        };
    }

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null) =>
        _reservaRepository.ObtenerHorariosDisponibles(fecha, excluirReservaId);

    public Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.DTOs.ReservaAgendaResponseDto>> ObtenerAgenda(
        bool historialAtendidas,
        CancellationToken cancellationToken) =>
        _reservaRepository.ObtenerAgenda(historialAtendidas, cancellationToken);

    public async Task CancelarReserva(int id, CancellationToken cancellationToken)
    {
        if (!await _reservaRepository.CancelarReserva(id, cancellationToken))
        {
            throw new KeyNotFoundException("No se encontró la reserva seleccionada.");
        }
    }

    public async Task ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken)
    {
        if (id <= 0 || idHorario <= 0)
        {
            throw new ArgumentException("La reserva y el horario seleccionado deben ser válidos.");
        }

        if (!await _reservaRepository.ReprogramarReserva(id, idHorario, cancellationToken))
        {
            throw new KeyNotFoundException("La reserva no existe.");
        }
    }

    public async Task<ResultadoAccionReserva> EjecutarAccionPorToken(string token, bool confirmar, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenInvalido };
        }

        var ahoraUtc = _proveedorTiempo.GetUtcNow().UtcDateTime;
        return await _reservaRepository.EjecutarAccionPorToken(
            TokenAccionReserva.ObtenerHash(token), confirmar, ahoraUtc, cancellationToken);
    }

}
