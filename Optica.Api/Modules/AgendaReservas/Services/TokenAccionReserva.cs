using System.Security.Cryptography;
using System.Text;

namespace Optica.Api.Modules.AgendaReservas.Services;

public static class TokenAccionReserva
{
    public static string Crear() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    public static string ObtenerHash(string token) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
