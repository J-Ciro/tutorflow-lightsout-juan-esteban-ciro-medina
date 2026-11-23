using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public static partial class ReferenciasEscritas
{
    private const char Marca = '';
    private const int PalabrasMinimasPorOracion = 3;
    private const int LimiteRegexMs = 1000;
    private const string DocumentoDerogado = "reglamento_academico_2023.md";
    private const string FinDePalabra = @"(?![\p{L}\p{N}_-])";
    private const string Parada = "(?:según|segun|ver|consulta|en|de|del|con|a|al|el|la|los|las)";
    private const string PalabraIzquierda = @"(?:(?!" + Parada + FinDePalabra + @")[\p{L}_-]+ +)";
    private const string Numero = @"\d+(?:\.\d+)*";
    private const string Documento =
        @"(?<doc>(?<![\p{L}\p{N}_-])(?:" + PalabraIzquierda + @"{1,3}\d+\.md|[\p{L}\p{N}_-]+\.md)" + FinDePalabra + ")";
    private const string Seccion =
        @"(?<sec>(?<![\p{L}\p{N}])(?:secci[oó]n(?:es)?|art[ií]culos?|art\.?|§)\s*" + Numero + @"(?:\s*(?:,|y|e)\s*" + Numero + ")*)";
    private const string Conector =
        @"(?:(?<![\p{L}\p{N}_-])(?:(?:según|segun|de\s+acuerdo\s+con|conforme\s+al?|ver|consulta|se\s+respaldan?\s+en|en|de|del)"
        + @"(?:\s+(?:el|la|los|las))?|(?:el|la|los|las))\s+)?";
    private const string ConectorHaciaDocumento = @"\s+(?:de\s+la|del|de|en\s+el|en)\s+";
    private const string PatronReferencia =
        Conector + "(?:" + Seccion + ConectorHaciaDocumento + Documento
        + "|" + Documento + @"\s*[·,:-]\s*" + Seccion
        + "|" + Documento + ")";

    private static readonly TimeSpan LimiteRegex = TimeSpan.FromMilliseconds(LimiteRegexMs);

    private static readonly (Regex Patron, string Reemplazo)[] ReglasDeSignos =
    [
        (new Regex(@"[\(\[][\s·,;:]*[\)\]]", RegexOptions.CultureInvariant, LimiteRegex), string.Empty),
        (new Regex(@" {2,}", RegexOptions.CultureInvariant, LimiteRegex), " "),
        (new Regex(@" +(?=[.,;:!?)])", RegexOptions.CultureInvariant, LimiteRegex), string.Empty),
        (new Regex(@"(?<!\.)\.\.(?!\.)", RegexOptions.CultureInvariant, LimiteRegex), "."),
        (new Regex(@"[,;:]\.", RegexOptions.CultureInvariant, LimiteRegex), "."),
        (new Regex(@",,", RegexOptions.CultureInvariant, LimiteRegex), ",")
    ];

    public static string Limpiar(string respuesta, IReadOnlyList<Cita> citas)
    {
        var marcada = Referencia().Replace(respuesta, coincidencia => EsValida(coincidencia, citas) ? coincidencia.Value : Marca.ToString());
        return marcada.Contains(Marca, StringComparison.Ordinal) ? Depurar(marcada) : respuesta;
    }

    public static bool TienePalabras(string texto) => PalabraDeLetras().IsMatch(texto);

    private static string Depurar(string marcada) =>
        string.Concat(CorteDeOracion().Split(marcada).Select(DepurarOracion)).Trim();

    private static string DepurarOracion(string oracion) =>
        oracion.Contains(Marca, StringComparison.Ordinal)
            ? DepurarOracionConReferenciaEliminada(oracion.Replace(Marca.ToString(), string.Empty, StringComparison.Ordinal))
            : oracion;

    private static string DepurarOracionConReferenciaEliminada(string oracion) =>
        Palabras(oracion) < PalabrasMinimasPorOracion
            ? string.Empty
            : Capitalizar(ReglasDeSignos.Aggregate(oracion, (texto, regla) => regla.Patron.Replace(texto, regla.Reemplazo)));

    private static int Palabras(string oracion) => PalabraOCifra().Count(oracion);

    private static string Capitalizar(string oracion)
    {
        var inicio = oracion.Length - oracion.TrimStart().Length;
        return inicio < oracion.Length && char.IsLower(oracion[inicio])
            ? string.Concat(oracion.AsSpan(0, inicio), char.ToUpperInvariant(oracion[inicio]).ToString(), oracion.AsSpan(inicio + 1))
            : oracion;
    }

    private static bool EsValida(Match coincidencia, IReadOnlyList<Cita> citas)
    {
        var documento = PlegadoCompatibilidad.Aplicar(coincidencia.Groups["doc"].Value);
        var seccion = coincidencia.Groups["sec"];
        return seccion.Success
            ? NumerosDe(seccion.Value).All(numero => citas.Any(cita => EsCitaDe(cita, documento, numero)))
            : string.Equals(documento, DocumentoDerogado, StringComparison.OrdinalIgnoreCase)
                || citas.Any(cita => MismoDocumento(cita, documento));
    }

    private static IEnumerable<string> NumerosDe(string seccion) =>
        NumeroDeSeccion().Matches(seccion).Select(numero => numero.Value);

    private static bool EsCitaDe(Cita cita, string documento, string numero) =>
        MismoDocumento(cita, documento) && NumeroDeSeccion().Matches(cita.Seccion).LastOrDefault()?.Value == numero;

    private static bool MismoDocumento(Cita cita, string documento) =>
        string.Equals(PlegadoCompatibilidad.Aplicar(cita.Documento), documento, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(PatronReferencia, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, LimiteRegexMs)]
    private static partial Regex Referencia();

    [GeneratedRegex(@"\d+(?:\.\d+)*", RegexOptions.CultureInvariant, LimiteRegexMs)]
    private static partial Regex NumeroDeSeccion();

    [GeneratedRegex(@"\p{L}{2,}", RegexOptions.CultureInvariant, LimiteRegexMs)]
    private static partial Regex PalabraDeLetras();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant, LimiteRegexMs)]
    private static partial Regex PalabraOCifra();

    [GeneratedRegex(@"(?<=\n)|(?<=[.!?;])(?=\s|$)", RegexOptions.CultureInvariant, LimiteRegexMs)]
    private static partial Regex CorteDeOracion();
}
