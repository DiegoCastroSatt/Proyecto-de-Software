using System.Text.RegularExpressions;

namespace Optica.Api.Modules.Clientes.Services;

public static partial class TelefonoChilenoValidator
{
    [GeneratedRegex(@"^(\+?56\s?)?9\s?\d{4}\s?\d{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex PatronTelefono();

    public static bool EsValido(string? telefono) =>
        !string.IsNullOrWhiteSpace(telefono) && PatronTelefono().IsMatch(telefono.Trim());
}