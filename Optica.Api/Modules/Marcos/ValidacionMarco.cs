using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Optica.Api.Modules.Marcos;

public static class ValidacionMarco
{
    // Los dibujos se recorren tal como llegan; solo se excluye contenido activo o externo.
    private static readonly HashSet<string> ContenidoActivo = new(StringComparer.OrdinalIgnoreCase)
    { "script", "foreignObject", "animate", "animateTransform", "animateMotion", "set", "discard", "style", "iframe", "object", "embed", "audio", "video", "handler", "listener" };

    public static string Normalizar(string valor) => Regex.Replace(string.Concat(valor.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        .ToLowerInvariant().Replace('-', ' ').Replace('_', ' '), @"\s+", " ").Trim();

    public static string Categoria(string valor)
    {
        var texto = Regex.Replace(Normalizar(valor), @"^(marcos?|armazon|armazones|monturas?)\s+", "");
        return texto switch
        {
            "rectangulares" => "rectangular", "redondos" => "redondo", "ovalados" => "ovalado",
            "cuadrados" => "cuadrado", "aviadores" => "aviador", "geometricos" => "geometrico",
            "transparentes" => "transparente", "rectangulares altos" => "rectangular alto",
            "semi montura" => "semimontura", _ => texto
        };
    }

    public static bool EsArmazon(string categoria) => Normalizar(categoria) is "armazon" or "armazones";

    public static string ValidarSvg(string svg)
    {
        if (string.IsNullOrWhiteSpace(svg) || Encoding.UTF8.GetByteCount(svg) > 100_000)
            throw new ArgumentException("Selecciona una ilustración de hasta 100 KB o usa el editor visual.");
        try
        {
            using var lector = XmlReader.Create(new StringReader(svg), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000 });
            var documento = XDocument.Load(lector);
            XNamespace ns = "http://www.w3.org/2000/svg";
            if (documento.Root?.Name != ns + "svg") throw new ArgumentException("No se pudo reconocer la ilustración. Puedes usar un diseño del editor visual.");
            var raiz = documento.Root;
            if (raiz.Attribute("viewBox") is null)
            {
                static double Dimension(string? texto) => double.TryParse((texto ?? "").Replace("px", "", StringComparison.OrdinalIgnoreCase),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var numero) && double.IsFinite(numero) && numero > 0 ? numero : 0;
                var ancho = Dimension(raiz.Attribute("width")?.Value); var alto = Dimension(raiz.Attribute("height")?.Value);
                if (ancho > 0 && alto > 0) raiz.SetAttributeValue("viewBox", FormattableString.Invariant($"0 0 {ancho} {alto}"));
            }
            var vista = raiz.Attribute("viewBox")?.Value.Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (vista is null || vista.Length != 4 || vista.Any(v => !double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n))
                || double.Parse(vista[2], CultureInfo.InvariantCulture) <= 0 || double.Parse(vista[3], CultureInfo.InvariantCulture) <= 0)
                throw new ArgumentException("No pudimos reconocer el tamaño de la ilustración. Elige un diseño del editor visual.");
            foreach (var instruccion in raiz.DescendantNodes().OfType<XProcessingInstruction>().ToList()) instruccion.Remove();
            // Los metadatos de los editores gráficos no forman parte del dibujo.
            foreach (var nodo in raiz.Descendants().Where(e => e.Name.Namespace != ns || e.Name.LocalName == "metadata").ToList()) nodo.Remove();
            foreach (var elemento in raiz.DescendantsAndSelf())
            {
                if (ContenidoActivo.Contains(elemento.Name.LocalName))
                    throw new ArgumentException("La ilustración contiene contenido interactivo que no se puede usar. Elige un diseño del editor visual.");
                var estilo = elemento.Attribute("style");
                if (estilo is not null)
                {
                    ValidarReferencia(estilo.Value);
                    foreach (var regla in estilo.Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var partes = regla.Split(':', 2, StringSplitOptions.TrimEntries);
                        if (partes.Length != 2 || !Regex.IsMatch(partes[0], @"^[a-z][a-z-]*$", RegexOptions.IgnoreCase)
                            || partes[0].StartsWith("on", StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentException("No se pudo preparar la ilustración. Usa un diseño del editor visual.");
                        elemento.SetAttributeValue(partes[0], partes[1]);
                    }
                    estilo.Remove();
                }
                foreach (var atributo in elemento.Attributes().ToList())
                {
                    if (atributo.IsNamespaceDeclaration) continue;
                    var nombre = atributo.Name.LocalName;
                    if (nombre.StartsWith("on", StringComparison.OrdinalIgnoreCase) || nombre.Equals("base", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("La ilustración contiene acciones que no se pueden usar. Elige un diseño del editor visual.");
                    if (atributo.Name.Namespace != XNamespace.None && atributo.Name.NamespaceName != "http://www.w3.org/1999/xlink"
                        && atributo.Name.Namespace != XNamespace.Xml) { atributo.Remove(); continue; }
                    if (nombre.ToLowerInvariant() is "href" or "src" or "data" or "poster")
                    {
                        if (!Regex.IsMatch(atributo.Value, @"^#[a-zA-Z_][a-zA-Z0-9_.:-]*$"))
                            throw new ArgumentException("La ilustración depende de otro archivo. Usa un diseño del editor visual o una ilustración independiente.");
                    }
                    ValidarReferencia(atributo.Value);
                }
            }
            return documento.Root.ToString(SaveOptions.DisableFormatting);
        }
        catch (XmlException) { throw new ArgumentException("No se pudo abrir la ilustración. Prueba con otro archivo o usa el editor visual."); }
    }
    private static void ValidarReferencia(string valor)
    {
        if (Regex.IsMatch(valor, @"[\\@]|expression\s*\(|javascript\s*:|vbscript\s*:|(?:https?|data)\s*:|//", RegexOptions.IgnoreCase))
            throw new ArgumentException("La ilustración contiene referencias externas. Usa un diseño del editor visual.");
        var sinReferenciasLocales = Regex.Replace(valor, "url\\(\\s*(['\"]?)#[a-zA-Z_][a-zA-Z0-9_.:-]*\\1\\s*\\)", "", RegexOptions.IgnoreCase);
        if (Regex.IsMatch(sinReferenciasLocales, @"url\s*\(", RegexOptions.IgnoreCase))
            throw new ArgumentException("La ilustración depende de otro archivo. Usa un diseño del editor visual.");
    }

}
