using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed record DocumentoEscrito(int Inicio, int Fin, string Nombre);

internal static partial class DocumentosEscritos
{
    private const int MaximoPalabrasHaciaLaIzquierda = 3;
    private const int MaximoPalabrasDeUnNombre = 4;
    private const int MaximoPalabrasDelPrefijo = 2;
    private const string ExtensionMarkdown = ".md";

    private static readonly string[] NombresCanonicos = ["reglamento academico 2025", "evaluacion y calificaciones", "bienestar y becas"];

    private static readonly HashSet<string> PalabrasQueNoSeAbsorben = new(StringComparer.OrdinalIgnoreCase)
    {
        "según", "segun", "ver", "consulta", "en", "de", "del", "con", "a", "al", "el", "la", "los", "las"
    };

    private static readonly HashSet<string> PrefijosDelNombre = new(StringComparer.Ordinal) { "reglamento", "documento" };

    public static IReadOnlyList<DocumentoEscrito> En(string texto) =>
        [.. NombreMarkdown().Matches(texto).Select(nombre => Ampliar(texto, nombre))];

    private static DocumentoEscrito Ampliar(string texto, Match nombre)
    {
        var fin = nombre.Index + nombre.Length;
        var parte = nombre.Value[..^ExtensionMarkdown.Length];
        var inicio = parte.All(char.IsDigit) ? InicioPorDigitos(texto, nombre.Index) : InicioPorNombreConEspacios(texto, nombre.Index, parte);
        return new DocumentoEscrito(inicio, fin, texto[inicio..fin]);
    }

    private static int InicioPorDigitos(string texto, int inicio)
    {
        var palabras = PalabrasALaIzquierda(texto, inicio, MaximoPalabrasHaciaLaIzquierda)
            .TakeWhile(palabra => palabra.Valor.All(char.IsLetter) && !PalabrasQueNoSeAbsorben.Contains(palabra.Valor))
            .ToList();
        return palabras.Count == 0 ? inicio : palabras[^1].Inicio;
    }

    private static int InicioPorNombreConEspacios(string texto, int inicio, string parte)
    {
        var palabras = PalabrasALaIzquierda(texto, inicio, MaximoPalabrasDeUnNombre);
        for (var cantidad = palabras.Count; cantidad >= 1; cantidad--)
        {
            var candidato = string.Join(' ', palabras.Take(cantidad).Reverse().Select(palabra => palabra.Valor).Append(parte));
            if (NombresCanonicos.Contains(NormalizadorTexto.Normalizar(candidato)))
            {
                return ConPrefijo(palabras, cantidad);
            }
        }

        return inicio;
    }

    private static int ConPrefijo(List<PalabraIzquierda> palabras, int cantidad)
    {
        var previas = palabras.Skip(cantidad).Select(palabra => (palabra.Inicio, Valor: NormalizadorTexto.Normalizar(palabra.Valor))).ToList();
        var inicioDelPrefijo = previas.Take(MaximoPalabrasDelPrefijo).Where((previa, posicion) => EsPrefijo(previas, posicion)).Select(previa => previa.Inicio);
        return inicioDelPrefijo.DefaultIfEmpty(palabras[cantidad - 1].Inicio).Last();
    }

    private static bool EsPrefijo(List<(int Inicio, string Valor)> previas, int posicion) =>
        posicion == 0
            ? PrefijosDelNombre.Contains(previas[0].Valor)
            : previas[0].Valor == "de" && previas[1].Valor == "documento";

    private static List<PalabraIzquierda> PalabrasALaIzquierda(string texto, int indice, int maximo)
    {
        var palabras = new List<PalabraIzquierda>();
        var posicion = indice;
        while (palabras.Count < maximo)
        {
            var espacios = EspaciosAnteriores().Match(texto[..posicion]);
            var palabra = espacios.Success ? PalabraAnterior().Match(texto[..espacios.Index]) : Match.Empty;
            if (!palabra.Success)
            {
                break;
            }

            palabras.Add(new PalabraIzquierda(palabra.Index, palabra.Value));
            posicion = palabra.Index;
        }

        return palabras;
    }

    private sealed record PalabraIzquierda(int Inicio, string Valor);

    [GeneratedRegex(@"(?<![\p{L}\p{N}_-])[\p{L}\p{N}_-]+\.md(?![\p{L}\p{N}_-])", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex NombreMarkdown();

    [GeneratedRegex(@" +$", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex EspaciosAnteriores();

    [GeneratedRegex(@"[\p{L}\p{N}_-]+$", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex PalabraAnterior();
}
