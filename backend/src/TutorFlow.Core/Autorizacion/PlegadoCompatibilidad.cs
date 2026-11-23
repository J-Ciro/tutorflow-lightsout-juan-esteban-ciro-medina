using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace TutorFlow.Core.Autorizacion;

internal static class PlegadoCompatibilidad
{
    private const string NombreDelRecurso = "TutorFlow.Core.Autorizacion.PlegadoUnicode.tsv";
    private const char SeparadorDeColumnas = '\t';
    private const char SeparadorDePuntos = ' ';
    private const int LineaDeVersionUnicode = 0;
    private const int LineaDeVersionConfusables = 1;
    private const int ColumnasPorFila = 3;

    private static readonly string[] TiposDespuesDeLaDescomposicion = ["mn", "conf", "trazo", "nd", "cf"];

    private static readonly List<string> Lineas = LeerRecurso();

    private static readonly Dictionary<string, Dictionary<int, string>> PorTipo = Cargar(Lineas);

    private static readonly ConcurrentDictionary<int, string> Resueltos = new();

    public static string VersionUnicode { get; } = VersionDe(Lineas[LineaDeVersionUnicode]);

    public static string VersionConfusables { get; } = VersionDe(Lineas[LineaDeVersionConfusables]);

    public static string Aplicar(string texto) => string.Concat(texto.EnumerateRunes().Select(De));

    public static string De(Rune rune) => Resueltos.GetOrAdd(rune.Value, Resolver);

    private static string Resolver(int punto)
    {
        var descompuesto = PorTipo["desc"].GetValueOrDefault(punto) ?? char.ConvertFromUtf32(punto);
        return string.Concat(descompuesto.EnumerateRunes().Select(rune => Plegar(rune.Value)));
    }

    private static string Plegar(int punto) =>
        TiposDespuesDeLaDescomposicion.Select(tipo => PorTipo[tipo].GetValueOrDefault(punto)).FirstOrDefault(destino => destino is not null)
            ?? char.ConvertFromUtf32(punto);

    private static string VersionDe(string linea) => linea.Split(SeparadorDeColumnas)[1];

    private static List<string> LeerRecurso()
    {
        using var flujo = typeof(PlegadoCompatibilidad).Assembly.GetManifestResourceStream(NombreDelRecurso)
            ?? throw new InvalidOperationException($"Falta el recurso incrustado {NombreDelRecurso}.");
        using var lector = new StreamReader(flujo, Encoding.UTF8);
        return [.. lector.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static Dictionary<string, Dictionary<int, string>> Cargar(IReadOnlyList<string> lineas) =>
        lineas.Where(linea => !linea.StartsWith('#'))
            .Select(linea => linea.Split(SeparadorDeColumnas))
            .Where(columnas => columnas.Length == ColumnasPorFila)
            .GroupBy(columnas => columnas[0])
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToDictionary(columnas => Punto(columnas[1]), columnas => Destino(columnas[2])));

    private static int Punto(string hexadecimal) => int.Parse(hexadecimal, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static string Destino(string puntos) =>
        string.Concat(puntos.Split(SeparadorDePuntos, StringSplitOptions.RemoveEmptyEntries).Select(punto => char.ConvertFromUtf32(Punto(punto))));
}
