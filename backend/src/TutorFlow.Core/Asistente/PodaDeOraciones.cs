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
        PodarConListasDependientes(texto, oracion => !MencionaNombreOId(oracion, contexto));

    public static string SinCitasEntreParentesis(string texto) => CitaEntreParentesis().Replace(texto, string.Empty);

    private static string Podar(string texto, Func<string, bool> conservar) =>
        string.Join("\n", SaltoDeLinea().Split(texto).Select(linea => PodarLinea(linea, conservar)).Where(linea => linea.Length > 0)).Trim();

    private static string PodarLinea(string linea, Func<string, bool> conservar) =>
        string.Concat(CorteDeOracion().Split(linea).Where(oracion => oracion.Trim().Length == 0 || conservar(oracion.Trim()))).Trim();

    private static string PodarConListasDependientes(string texto, Func<string, bool> conservar)
    {
        var lineas = SaltoDeLinea().Split(texto);
        var resultado = new List<string>();
        var podandoLista = false;
        for (var indice = 0; indice < lineas.Length; indice++)
        {
            var dependiente = podandoLista && EsLineaDeLista(lineas[indice]);
            var salida = dependiente ? string.Empty : PodarLinea(lineas[indice], conservar);
            resultado.Add(salida);
            podandoLista = ContinuaPoda(dependiente, salida, lineas, indice);
        }

        return string.Join("\n", resultado.Where(linea => linea.Length > 0)).Trim();
    }

    private static bool ContinuaPoda(bool dependiente, string salida, string[] lineas, int indice) =>
        dependiente || (salida != lineas[indice].Trim() && EncabezaLista(lineas, indice));

    private static bool EncabezaLista(string[] lineas, int indice) =>
        lineas[indice].TrimEnd().EndsWith(':')
        || (!EsLineaDeLista(lineas[indice]) && indice + 1 < lineas.Length && EsLineaDeLista(lineas[indice + 1]));

    private static bool EsLineaDeLista(string linea) =>
        linea.Trim().Length > 0 && (char.IsWhiteSpace(linea[0]) || ElementoDeLista().IsMatch(linea));

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
        var ids = contexto.Directorio.Estudiantes.Select(estudiante => estudiante.Id).ToHashSet();
        return DetectorTerceros.PersonasMencionadas(string.Join(' ', tokens), contexto.Directorio).Any(ids.Contains)
            || tokens.Any(nombres.Contains);
    }

    private static HashSet<string> NombresSueltos(ContextoDePoda contexto) =>
        [.. contexto.Directorio.Estudiantes.Select(estudiante => estudiante.Nombre)
            .SelectMany(TokenizadorTexto.Valores)];

    [GeneratedRegex(@"\s*\(\s*[\p{L}\p{N}_-]+\.md\s*·[^)]*\)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CitaEntreParentesis();

    [GeneratedRegex(@"^\s*(?:[-*+•]|\d+[.)])\s", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ElementoDeLista();

    [GeneratedRegex(@"\r?\n", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SaltoDeLinea();

    [GeneratedRegex(@"(?<=[.!?])(?=\s|$)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CorteDeOracion();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[Éé]l(?![\p{L}\p{N}])", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ConTildeEl();
}
