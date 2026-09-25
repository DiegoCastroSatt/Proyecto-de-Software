using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Clientes.Services;
public class ReservaService : IReservaService
{
    private readonly IReservaRepository _reservaRepository;

    public ReservaService(
        IReservaRepository reservaRepository)
    {
        _reservaRepository = reservaRepository;
    }

    public async Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto)
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

        var rutNormalizado = RutChilenoValidator.Normalizar(dto.Rut);
        if (!string.IsNullOrWhiteSpace(dto.Correo)
            && await _reservaRepository.ExisteCorreoEnOtroCliente(dto.Correo, rutNormalizado))
        {
            throw new ArgumentException("Ese correo ya está registrado con otro cliente. Ingresa otro correo o revisa el RUT.");
        }

        var reservaCreada = await _reservaRepository.CrearReserva(dto);
        return new ReservaResponseDto
        {
            Id = reservaCreada.Id,
            IdHorario = reservaCreada.HorarioId,
            Fecha = reservaCreada.Fecha,
            Hora = reservaCreada.Hora,
            Estado = reservaCreada.Estado
        };
    }

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles() =>
        _reservaRepository.ObtenerHorariosDisponibles();
}