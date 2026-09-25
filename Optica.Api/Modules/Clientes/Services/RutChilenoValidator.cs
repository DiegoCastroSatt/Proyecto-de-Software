namespace Optica.Api.Modules.Clientes.Services;

public static class RutChilenoValidator
{
    public static string Normalizar(string? rut) =>
        (rut ?? string.Empty)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

    public static bool EsValido(string? rut)
    {
        if (string.IsNullOrWhiteSpace(rut))
        {
            return false;
        }

        var limpio = Normalizar(rut);

        if (limpio.Length is < 8 or > 9)
        {
            return false;
        }

        var cuerpo = limpio[..^1];
        var digitoIngresado = limpio[^1];
        if (!cuerpo.All(char.IsAsciiDigit))
        {
            return false;
        }

        var suma = 0;
        var multiplo = 2;
        for (var indice = cuerpo.Length - 1; indice >= 0; indice--)
        {
            suma += (cuerpo[indice] - '0') * multiplo;
            multiplo = multiplo == 7 ? 2 : multiplo + 1;
        }

        var digitoEsperado = 11 - suma % 11;
        var digitoCalculado = digitoEsperado switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + digitoEsperado)
        };

        return digitoCalculado == digitoIngresado;
    }
}