using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed record ContextoDePoda(
    DirectorioAcademico Directorio,
    IReadOnlyList<AsignaturaCatalogo> Catalogo,
    IReadOnlyCollection<Cita> Escritas,
    IReadOnlyList<string> NombresDelBloque);

internal static partial class PodaDeOraciones
{
    private static readonly HashSet<string> Introducciones = new(StringComparer.Ordinal)
    {
        "estos son tus datos", "aqui estan tus datos", "aqui tienes tus datos", "este es tu detalle",
        "aqui esta el detalle de tus faltas", "estos son los datos solicitados", "este es el listado",
        "este es el listado de tu programa"
    };

    private static readonly string[] ExpresionesDeTuPrograma = [" tu programa ", " tus estudiantes ", " tu coordinador "];

    private static readonly HashSet<string> SegundaPersona = new(StringComparer.Ordinal)
    {
        "tu", "tus", "te", "ti", "usted", "tienes", "estas", "llevas", "vas", "cumples", "eres", "perdiste", "aprobaste",
        "reprobaste", "pasaste", "ganaste", "sacaste", "faltaste", "obtuviste", "cursaste", "quedaste"
    };

    private static readonly HashSet<string> Pronombres = new(StringComparer.Ordinal) { "ella", "ellas", "ellos" };

    private static readonly HashSet<string> MarcadoresImpersonales = new(StringComparer.Ordinal)
    {
        "se", "quien", "quienes", "cada", "toda", "todo"
    };

    private static readonly string[] ExpresionesImpersonales = [" un estudiante ", " el estudiante ", " los estudiantes ", " una asignatura "];

    private static readonly HashSet<string> NumerosEnLetras = new(StringComparer.Ordinal)
    {
        "cero", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve", "diez", "once", "doce", "trece", "catorce",
        "quince", "dieciseis", "diecisiete", "dieciocho", "diecinueve", "veinte", "treinta", "cuarenta", "cincuenta",
        "sesenta", "setenta", "ochenta", "noventa", "cien", "ciento"
    };

    public static string EnModoBloque(string texto, ContextoDePoda contexto) =>
        Podar(texto, oracion => EsIntroduccion(oracion, contexto) || EsNormaCitada(oracion, contexto));

    public static string FueraDelBloque(string texto, ContextoDePoda contexto) =>
        Podar(texto, oracion => !MencionaNombreOId(oracion, contexto));

    public static string SinCitasEntreParentesis(string texto) => CitaEntreParentesis().Replace(texto, string.Empty);

    private static string Podar(string texto, Func<string, bool> conservar) =>
        string.Join("\n", SaltoDeLinea().Split(texto)
            .Select(linea => string.Concat(CorteDeOracion().Split(linea).Where(oracion => oracion.Trim().Length == 0 || conservar(oracion.Trim()))).Trim())
            .Where(linea => linea.Length > 0)).Trim();

    private static bool EsIntroduccion(string oracion, ContextoDePoda contexto)
    {
        var normalizada = NormalizadorTexto.Normalizar(oracion);
        return Introducciones.Contains(normalizada)
            || contexto.NombresDelBloque.Any(nombre => normalizada == $"estos son los datos de {nombre}" || normalizada == $"este es el detalle de {nombre}");
    }

    private static bool EsNormaCitada(string oracion, ContextoDePoda contexto)
    {
        var sinCitas = ExtractorCitas.SinCitas(oracion);
        var tokens = TokenizadorTexto.Valores(sinCitas);
        var texto = $" {string.Join(' ', tokens)} ";
        return ExtractorCitas.De(oracion).Any(contexto.Escritas.Contains)
            && EsImpersonal(tokens, texto)
            && !MencionaPersona(oracion, texto, contexto)
            && !MencionaAsignatura(sinCitas, contexto)
            && !tokens.Any(NumerosEnLetras.Contains);
    }

    private static bool MencionaAsignatura(string texto, ContextoDePoda contexto) =>
        CatalogoAsignaturas.Apariciones(TokenizadorTexto.Tokenizar(texto), contexto.Catalogo).Count > 0;

    private static bool EsImpersonal(IReadOnlyList<string> tokens, string texto) =>
        tokens.Any(MarcadoresImpersonales.Contains) || ExpresionesImpersonales.Any(expresion => texto.Contains(expresion, StringComparison.Ordinal));

    private static bool MencionaPersona(string original, string texto, ContextoDePoda contexto)
    {
        var sinTuPrograma = ExpresionesDeTuPrograma.Aggregate(texto, (actual, expresion) => actual.Replace(expresion, " ", StringComparison.Ordinal));
        var palabras = sinTuPrograma.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palabras.Any(palabra => SegundaPersona.Contains(palabra) || Pronombres.Contains(palabra))
            || ConTildeEl().IsMatch(original)
            || MencionaNombreOId(original, contexto);
    }

    private static bool MencionaNombreOId(string oracion, ContextoDePoda contexto)
    {
        var tokens = TokenizadorTexto.Valores(oracion);
        var nombres = NombresSueltos(contexto);
        return DetectorTerceros.PersonasMencionadas(string.Join(' ', tokens), contexto.Directorio).Count > 0
            || tokens.Any(nombres.Contains);
    }

    private static HashSet<string> NombresSueltos(ContextoDePoda contexto) =>
        [.. contexto.Directorio.Estudiantes.Select(estudiante => estudiante.Nombre)
            .Concat(contexto.Directorio.Coordinadores.Select(coordinador => coordinador.Nombre))
            .SelectMany(TokenizadorTexto.Valores)];

    [GeneratedRegex(@"\s*\(\s*[\p{L}\p{N}_-]+\.md\s*·[^)]*\)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CitaEntreParentesis();

    [GeneratedRegex(@"\r?\n", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SaltoDeLinea();

    [GeneratedRegex(@"(?<=[.!?])(?=\s|$)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CorteDeOracion();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[Éé]l(?![\p{L}\p{N}])", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ConTildeEl();
}
