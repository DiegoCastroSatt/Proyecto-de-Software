using System.Security.Cryptography;
using System.Text;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class EstadoRecuperacionCliente
{
    private static readonly TimeSpan VigenciaToken = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan VentanaPorCuenta = TimeSpan.FromHours(1);
    private static readonly TimeSpan VentanaPorIp = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan EsperaEntreSolicitudes = TimeSpan.FromSeconds(60);
    private const int MaximoSolicitudesPorCuenta = 5;
    private const int MaximoSolicitudesPorIp = 10;
    private const int MaximoClavesLimite = 10000;

    private readonly object sincronizacion = new();
    private readonly Dictionary<string, SolicitudToken> solicitudes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LimiteCuenta> limitesCuenta = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<DateTimeOffset>> limitesIp = new(StringComparer.Ordinal);
    private readonly byte[] claveHash = RandomNumberGenerator.GetBytes(32);

    public static string NormalizarCorreo(string correo) => correo.Trim().ToUpperInvariant();

    public bool IntentarIniciarSolicitud(string correoCliente, string? ipCliente, DateTimeOffset ahora)
    {
        var correo = NormalizarCorreo(correoCliente);
        var ip = string.IsNullOrWhiteSpace(ipCliente) ? "ip-desconocida" : ipCliente.Trim();

        lock (sincronizacion)
        {
            LimpiarLimites(ahora);

            var limiteCuentaExiste = limitesCuenta.TryGetValue(correo, out var cuenta);
            var limiteIpExiste = limitesIp.TryGetValue(ip, out var solicitudesIp);

            var esperaActiva = limiteCuentaExiste && cuenta!.UltimaSolicitud is { } ultima
                && ahora - ultima < EsperaEntreSolicitudes;
            var limiteCuentaAlcanzado = limiteCuentaExiste
                && cuenta!.Solicitudes.Count >= MaximoSolicitudesPorCuenta;
            var limiteIpAlcanzado = limiteIpExiste && solicitudesIp!.Count >= MaximoSolicitudesPorIp;

            if (limiteIpAlcanzado || esperaActiva || limiteCuentaAlcanzado)
                return false;

            if (limiteIpExiste)
            {
                solicitudesIp!.Enqueue(ahora);
            }
            else if (limitesIp.Count < MaximoClavesLimite)
            {
                limitesIp[ip] = new Queue<DateTimeOffset>([ahora]);
            }
            else
            {
                return false;
            }

            if (!limiteCuentaExiste)
            {
                if (limitesCuenta.Count >= MaximoClavesLimite)
                    return false;
                cuenta = new LimiteCuenta();
                limitesCuenta[correo] = cuenta;
            }

            cuenta!.Solicitudes.Enqueue(ahora);
            cuenta.UltimaSolicitud = ahora;
            return true;
        }
    }

    public string GenerarToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public byte[] CrearHashToken(string token)
    {
        using var hmac = new HMACSHA256(claveHash);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(token.Trim()));
    }

    public void GuardarSolicitud(string token, int idCliente, string correo, DateTimeOffset ahora)
    {
        var hash = Convert.ToHexString(CrearHashToken(token));
        lock (sincronizacion)
        {
            // Remover tokens anteriores asociados a la misma cuenta
            foreach (var (k, s) in solicitudes.Where(kvp => kvp.Value.IdCliente == idCliente).ToArray())
            {
                solicitudes.Remove(k);
            }

            solicitudes[hash] = new SolicitudToken(idCliente, correo, ahora.Add(VigenciaToken));
        }
    }

    public ResultadoValidacionToken ValidarToken(string token, DateTimeOffset ahora)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ResultadoValidacionToken.Invalido;

        var hash = Convert.ToHexString(CrearHashToken(token));
        lock (sincronizacion)
        {
            if (!solicitudes.TryGetValue(hash, out var solicitud))
                return ResultadoValidacionToken.Invalido;

            if (solicitud.ExpiraUtc <= ahora)
            {
                solicitudes.Remove(hash);
                return ResultadoValidacionToken.Invalido;
            }

            if (solicitud.Procesando)
                return new ResultadoValidacionToken(false, solicitud.IdCliente, solicitud.Correo, EnProceso: true);

            solicitud.Procesando = true;
            return new ResultadoValidacionToken(true, solicitud.IdCliente, solicitud.Correo);
        }
    }

    public void FinalizarCambio(string token, bool exitoso)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        var hash = Convert.ToHexString(CrearHashToken(token));
        lock (sincronizacion)
        {
            if (exitoso)
            {
                solicitudes.Remove(hash);
            }
            else if (solicitudes.TryGetValue(hash, out var solicitud))
            {
                solicitud.Procesando = false;
            }
        }
    }

    private void LimpiarLimites(DateTimeOffset ahora)
    {
        foreach (var (ip, marcas) in limitesIp.ToArray())
        {
            while (marcas.Count > 0 && ahora - marcas.Peek() >= VentanaPorIp)
                marcas.Dequeue();
            if (marcas.Count == 0)
                limitesIp.Remove(ip);
        }

        foreach (var (correo, limite) in limitesCuenta.ToArray())
        {
            while (limite.Solicitudes.Count > 0 && ahora - limite.Solicitudes.Peek() >= VentanaPorCuenta)
                limite.Solicitudes.Dequeue();
            if (limite.Solicitudes.Count == 0)
            {
                limite.UltimaSolicitud = null;
                limitesCuenta.Remove(correo);
            }
        }

        foreach (var (hash, solicitud) in solicitudes.ToArray())
        {
            if (solicitud.ExpiraUtc <= ahora && !solicitud.Procesando)
                solicitudes.Remove(hash);
        }
    }

    private sealed class SolicitudToken(int idCliente, string correo, DateTimeOffset expiraUtc)
    {
        public int IdCliente { get; } = idCliente;
        public string Correo { get; } = correo;
        public DateTimeOffset ExpiraUtc { get; } = expiraUtc;
        public bool Procesando { get; set; }
    }

    private sealed class LimiteCuenta
    {
        public Queue<DateTimeOffset> Solicitudes { get; } = new();
        public DateTimeOffset? UltimaSolicitud { get; set; }
    }
}

public readonly record struct ResultadoValidacionToken(
    bool Valido,
    int IdCliente = 0,
    string Correo = "",
    bool EnProceso = false)
{
    public static ResultadoValidacionToken Invalido => new(false);
}