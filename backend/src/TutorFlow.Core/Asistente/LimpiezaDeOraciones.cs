using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static partial class LimpiezaDeOraciones
{
    private const int PalabrasMinimasPorOracion = 3;
    private const string EspacioEnBlanco = " ";

    private static readonly TimeSpan LimiteRegex = TimeSpan.FromMilliseconds(TiempoLimiteRegex.Milisegundos);

    private static readonly (Regex Patron, string Reemplazo)[] ReglasDeSignos =
    [
        (new Regex(@"[\(\[][\s·,;:]*[\)\]]", RegexOptions.CultureInvariant, LimiteRegex), EspacioEnBlanco),
        (new Regex(@" {2,}", RegexOptions.CultureInvariant, LimiteRegex), EspacioEnBlanco),
        (new Regex(@"(?<!\d) +(?=[.,;:!?)])|(?<=\d) +(?=[;:!?)]|[.,](?!\d))", RegexOptions.CultureInvariant, LimiteRegex), string.Empty),
        (new Regex(@"(?<!\.)\.\.(?!\.)", RegexOptions.CultureInvariant, LimiteRegex), "."),
        (new Regex(@"[,;:]\.", RegexOptions.CultureInvariant, LimiteRegex), "."),
        (new Regex(@",,", RegexOptions.CultureInvariant, LimiteRegex), ","),
        (new Regex(@"^[\s:,;·]+", RegexOptions.CultureInvariant, LimiteRegex), string.Empty)
    ];

    public static string Depurar(string marcada) =>
        string.Concat(CorteDeOracion().Split(marcada).Select(Oracion)).Trim();

    private static string Oracion(string oracion) =>
        oracion.Contains(ReferenciasEscritas.Marca, StringComparison.Ordinal) ? Depurada(oracion) : oracion;

    private static string Depurada(string marcada)
    {
        var texto = marcada.Replace(ReferenciasEscritas.Marca, ' ');
        if (TerminaEnAtribucionHuerfana(texto) || HuecoTrasPalabraIncompleta(marcada) || QuedaCorta(texto))
        {
            return string.Empty;
        }

        var margen = EspaciosIniciales().Match(texto).Value;
        var cola = EspaciosFinales().Match(texto).Value;
        var cuerpo = ReglasDeSignos.Aggregate(texto.Trim(), (actual, regla) => regla.Patron.Replace(actual, regla.Reemplazo)).Trim();
        return margen + Capitalizar(cuerpo, marcada) + cola;
    }

    private static bool TerminaEnAtribucionHuerfana(string texto) =>
        AtribucionHuerfana().IsMatch(PuntuacionFinal().Replace(texto, string.Empty));

    private static bool HuecoTrasPalabraIncompleta(string marcada) =>
        Enumerable.Range(0, marcada.Length)
            .Where(indice => marcada[indice] == ReferenciasEscritas.Marca)
            .Any(indice => PalabraIncompleta().IsMatch(NormalizadorTexto.Normalizar(marcada[..indice]).TrimEnd()));

    private static bool QuedaCorta(string texto) =>
        TerminaConPuntoFinal().IsMatch(texto.TrimEnd()) && PalabraOCifra().Count(texto) < PalabrasMinimasPorOracion;

    private static string Capitalizar(string cuerpo, string marcada)
    {
        var inicio = LetraTrasLaMarca().Match(marcada);
        return inicio.Success && cuerpo.Length > 0 && char.IsLower(cuerpo[0]) && cuerpo[0] == inicio.Groups["letra"].Value[0]
            ? char.ToUpperInvariant(cuerpo[0]) + cuerpo[1..]
            : cuerpo;
    }

    [GeneratedRegex(@"(?<=\n)|(?<=[.!?])(?=\s|$)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CorteDeOracion();

    [GeneratedRegex(@"^\s*", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex EspaciosIniciales();

    [GeneratedRegex(@"\s*$", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex EspaciosFinales();

    [GeneratedRegex(@"[\s.,;:!?)\]]+$", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex PuntuacionFinal();

    [GeneratedRegex(@"[.!?]\z", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex TerminaConPuntoFinal();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex PalabraOCifra();

    [GeneratedRegex(@"^[\s:,;·]*(?<letra>\p{Ll})", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex LetraTrasLaMarca();

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:a|al|con|de|del|en|por|segun|sobre|esta|estan|figuran?|aparecen?|respaldad[oa]s?|se\s+respaldan?"
        + @"|se\s+basan?|basad[oa]s?|se\s+encuentran?|provienen?|se\s+establecen?|establecid[oa]s?|se\s+indican?|consulta|ver)\z",
        RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex PalabraIncompleta();

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:a|al|con|de|del|en|por|según|segun|sobre|respaldad[oa]s?|se\s+respaldan?|se\s+basan?|basad[oa]s?|se\s+encuentran?"
        + @"|provienen?|se\s+establecen?|establecid[oa]s?|se\s+indican?|consulta|ver)\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex AtribucionHuerfana();
}
