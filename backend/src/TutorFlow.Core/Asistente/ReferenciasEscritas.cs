using System.Text.RegularExpressions;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal static partial class ReferenciasEscritas
{
    internal const char Marca = '\uE000';
    private const string DocumentoDerogado = "reglamento_academico_2023.md";
    private const string LetrasDeNombre = @"[\p{L}\p{N}_-]";
    private const string Numero = @"\d+(?:\.\d+)*";
    private const string ReferenciaASeccion =
        @"(?<![\p{L}\p{N}])(?:secci[oó]n(?:es)?|art[ií]culos?|art\.?|§)\s*" + Numero + @"(?:\s*(?:,|y|e)\s*" + Numero + ")*";
    private const string ConectorDeReferencia =
        @"(?:(?<!" + LetrasDeNombre + @")(?:seg[uú]n|de\s+acuerdo\s+con|conforme\s+al?|ver|consulta|se\s+respaldan?\s+en|en|del?)(?:\s+(?:el|la|los|las))?"
        + @"|(?<!" + LetrasDeNombre + @")(?:el|la|los|las))\s+$";

    public static string Limpiar(string respuestaOriginal, IReadOnlyList<Cita> citas)
    {
        var respuesta = HtmlEnRespuesta.AMarkdown(respuestaOriginal);
        var marcada = AtribucionGenerica.Marcar(MarcarReferenciasInvalidas(respuesta, citas), citas);
        marcada = FraseFijaAnexada(marcada);
        return marcada.Contains(Marca, StringComparison.Ordinal) ? LimpiezaDeOraciones.Depurar(marcada) : respuesta;
    }

    public static bool TieneContenido(string texto) => PalabraDeLetras().IsMatch(texto) || CifraDeContenido().IsMatch(texto);

    private static string MarcarReferenciasInvalidas(string respuesta, IReadOnlyList<Cita> citas)
    {
        var invalidas = Unir(DocumentosEscritos.En(respuesta).Select(documento => Ampliar(respuesta, documento, citas)))
            .Where(rango => !rango.Valida)
            .Select(rango => rango with { Inicio = ConConector(respuesta, rango.Inicio) })
            .OrderByDescending(rango => rango.Inicio);
        var texto = respuesta;
        foreach (var rango in invalidas)
        {
            texto = string.Concat(texto.AsSpan(0, rango.Inicio), Marca.ToString(), texto.AsSpan(rango.Fin));
        }

        return texto;
    }

    private static RangoDeReferencia Ampliar(string texto, DocumentoEscrito documento, IReadOnlyList<Cita> citas)
    {
        var derecha = SeccionesALaDerecha().Match(texto, documento.Fin);
        var izquierda = SeccionALaIzquierda().Match(texto[..documento.Inicio]);
        var numeros = (izquierda.Success ? NumerosDe(izquierda.Groups["sec"].Value) : [])
            .Concat(derecha.Groups["sec"].Captures.SelectMany(captura => NumerosDe(captura.Value)))
            .ToList();
        var inicio = izquierda.Success ? izquierda.Index : documento.Inicio;
        return new RangoDeReferencia(inicio, derecha.Index + derecha.Length, EsValida(documento.Nombre, numeros, citas));
    }

    private static List<RangoDeReferencia> Unir(IEnumerable<RangoDeReferencia> rangos)
    {
        var unidos = new List<RangoDeReferencia>();
        foreach (var rango in rangos.OrderBy(rango => rango.Inicio))
        {
            unidos.AddRange(unidos.Count > 0 && rango.Inicio <= unidos[^1].Fin ? Fusionar(unidos, rango) : [rango]);
        }

        return unidos;
    }

    private static List<RangoDeReferencia> Fusionar(List<RangoDeReferencia> unidos, RangoDeReferencia rango)
    {
        var ultimo = unidos[^1];
        unidos.RemoveAt(unidos.Count - 1);
        return [new RangoDeReferencia(ultimo.Inicio, Math.Max(ultimo.Fin, rango.Fin), ultimo.Valida && rango.Valida)];
    }

    private static int ConConector(string texto, int inicio)
    {
        var conector = Conector().Match(texto[..inicio]);
        return conector.Success ? conector.Index : inicio;
    }

    private static bool EsValida(string documento, List<string> numeros, IReadOnlyList<Cita> citas) =>
        numeros.Count == 0
            ? string.Equals(documento, DocumentoDerogado, StringComparison.OrdinalIgnoreCase) || citas.Any(cita => MismoDocumento(cita, documento))
            : numeros.All(numero => citas.Any(cita => MismoDocumento(cita, documento) && NumeroDeCita(cita) == numero));

    private static bool MismoDocumento(Cita cita, string documento) =>
        documento.All(char.IsAscii) && string.Equals(cita.Documento, documento, StringComparison.OrdinalIgnoreCase);

    private static string? NumeroDeCita(Cita cita) => NumeroDeSeccion().Matches(cita.Seccion).LastOrDefault()?.Value;

    private static IEnumerable<string> NumerosDe(string seccion) => NumeroDeSeccion().Matches(seccion).Select(numero => numero.Value);

    private static string FraseFijaAnexada(string texto)
    {
        var sinFrase = Regex.Replace(texto, Regex.Escape(Textos.SinInformacion), Marca.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(TiempoLimiteRegex.Milisegundos));
        return TieneContenido(sinFrase.Replace(Marca, ' ')) ? sinFrase : texto;
    }

    private sealed record RangoDeReferencia(int Inicio, int Fin, bool Valida);

    [GeneratedRegex(@"\G(?:\s*[·,:-]\s*(?<sec>" + ReferenciaASeccion + "))*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SeccionesALaDerecha();

    [GeneratedRegex(@"(?<sec>" + ReferenciaASeccion + @")\s+(?:de\s+la|del|de|en\s+el|en)\s+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SeccionALaIzquierda();

    [GeneratedRegex(ConectorDeReferencia, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Conector();

    [GeneratedRegex(@"\d+(?:\.\d+)*", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex NumeroDeSeccion();

    [GeneratedRegex(@"\p{L}{2,}", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex PalabraDeLetras();

    [GeneratedRegex(@"(?<![\d.])\d\.\d{1,2}(?!\d)|(?<![\d.])\d+(?:\.\d{1,2})?\s*%", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CifraDeContenido();
}
