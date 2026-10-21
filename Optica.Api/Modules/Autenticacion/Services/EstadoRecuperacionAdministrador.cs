using System.Security.Cryptography;
using System.Text;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class EstadoRecuperacionAdministrador
{
    private static readonly TimeSpan VigenciaCodigo = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan VentanaPorCuenta = TimeSpan.FromHours(1);
    private static readonly TimeSpan VentanaPorIp = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan EsperaEntreSolicitudes = TimeSpan.FromSeconds(60);
    private const int MaximoSolicitudesPorCuenta = 5;
    private const int MaximoSolicitudesPorIp = 10;
    private const int MaximoIntentosCodigo = 3;
    private const int MaximoClavesLimite = 10000;

    private readonly object sincronizacion = new();
    private readonly Dictionary<string, SolicitudCodigo> solicitudes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LimiteCuenta> limitesCuenta = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<DateTimeOffset>> limitesIp = new(StringComparer.Ordinal);
    private readonly byte[] claveHash = RandomNumberGenerator.GetBytes(32);

    public static string NormalizarNombre(string nombreUsuario) => nombreUsuario.Trim().ToUpperInvariant();

    public bool IntentarIniciarSolicitud(string nombreUsuario, string? ipCliente, DateTimeOffset ahora)
    {
        var nombre = NormalizarNombre(nombreUsuario);
        var ip = string.IsNullOrWhiteSpace(ipCliente) ? "ip-desconocida" : ipCliente.Trim();
        lock (sincronizacion)
        {
            LimpiarLimites(ahora);
            var limiteCuentaExiste = limitesCuenta.TryGetValue(nombre, out var cuenta);
            var limiteIpExiste = limitesIp.TryGetValue(ip, out var solicitudesIp);
            var esperaActiva = limiteCuentaExiste && cuenta!.UltimaSolicitud is { } ultima
                && ahora - ultima < EsperaEntreSolicitudes;
            var limiteCuentaAlcanzado = limiteCuentaExiste
                && cuenta!.Solicitudes.Count >= MaximoSolicitudesPorCuenta;
            var limiteIpAlcanzado = limiteIpExiste && solicitudesIp!.Count >= MaximoSolicitudesPorIp;

            if (limiteIpAlcanzado)
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

            if (esperaActiva || limiteCuentaAlcanzado)
                return false;

            if (!limiteCuentaExiste)
            {
                if (limitesCuenta.Count >= MaximoClavesLimite)
                    return false;
                cuenta = new LimiteCuenta();
                limitesCuenta[nombre] = cuenta;
            }

            cuenta!.Solicitudes.Enqueue(ahora);
            cuenta.UltimaSolicitud = ahora;
            solicitudes.Remove(nombre);
            return true;
        }
    }

    public byte[] CrearHashCodigo(string codigo)
    {
        using var hmac = new HMACSHA256(claveHash);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(codigo));
    }

    public void GuardarCodigo(string nombreUsuario, int idAdministrador, byte[] hashCodigo, DateTimeOffset ahora)
    {
        lock (sincronizacion)
        {
            solicitudes[NormalizarNombre(nombreUsuario)] = new SolicitudCodigo(
                idAdministrador, hashCodigo, ahora.Add(VigenciaCodigo));
        }
    }

    public ResultadoValidacionCodigo ValidarCodigo(string nombreUsuario, byte[] hashCodigo, DateTimeOffset ahora)
    {
        lock (sincronizacion)
        {
            var nombre = NormalizarNombre(nombreUsuario);
            if (!solicitudes.TryGetValue(nombre, out var solicitud))
                return ResultadoValidacionCodigo.Invalido;
            if (solicitud.ExpiraUtc <= ahora)
            {
                solicitudes.Remove(nombre);
                return ResultadoValidacionCodigo.Invalido;
            }
            if (solicitud.Procesando)
                return new ResultadoValidacionCodigo(false, solicitud.IdAdministrador, EnProceso: true);
            if (!CryptographicOperations.FixedTimeEquals(solicitud.HashCodigo, hashCodigo))
            {
                solicitud.IntentosIncorrectos++;
                if (solicitud.IntentosIncorrectos >= MaximoIntentosCodigo)
                    solicitudes.Remove(nombre);
                return ResultadoValidacionCodigo.Invalido;
            }

            solicitud.Procesando = true;
            return new ResultadoValidacionCodigo(true, solicitud.IdAdministrador);
        }
    }

    public void FinalizarCambio(string nombreUsuario, bool exitoso)
    {
        lock (sincronizacion)
        {
            var nombre = NormalizarNombre(nombreUsuario);
            if (exitoso)
            {
                solicitudes.Remove(nombre);
            }
            else if (solicitudes.TryGetValue(nombre, out var solicitud))
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

        foreach (var (nombre, limite) in limitesCuenta.ToArray())
        {
            while (limite.Solicitudes.Count > 0 && ahora - limite.Solicitudes.Peek() >= VentanaPorCuenta)
                limite.Solicitudes.Dequeue();
            if (limite.Solicitudes.Count == 0)
            {
                limite.UltimaSolicitud = null;
                if (!solicitudes.ContainsKey(nombre))
                    limitesCuenta.Remove(nombre);
            }
        }

        foreach (var (nombre, solicitud) in solicitudes.ToArray())
        {
            if (solicitud.ExpiraUtc <= ahora && !solicitud.Procesando)
                solicitudes.Remove(nombre);
        }
    }

    private sealed class SolicitudCodigo(int idAdministrador, byte[] hashCodigo, DateTimeOffset expiraUtc)
    {
        public int IdAdministrador { get; } = idAdministrador;
        public byte[] HashCodigo { get; } = hashCodigo;
        public DateTimeOffset ExpiraUtc { get; } = expiraUtc;
        public int IntentosIncorrectos { get; set; }
        public bool Procesando { get; set; }
    }

    private sealed class LimiteCuenta
    {
        public Queue<DateTimeOffset> Solicitudes { get; } = new();
        public DateTimeOffset? UltimaSolicitud { get; set; }
    }
}

public readonly record struct ResultadoValidacionCodigo(bool Valido, int IdAdministrador = 0, bool EnProceso = false)
{
    public static ResultadoValidacionCodigo Invalido => new(false);
}
