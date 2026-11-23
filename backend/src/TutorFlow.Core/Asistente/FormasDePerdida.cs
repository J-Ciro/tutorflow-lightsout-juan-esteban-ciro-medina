namespace TutorFlow.Core.Asistente;

internal sealed record FormaDePerdida(int Inicio, int Fin, bool Nominal, bool Negada, bool PorInasistencia);

internal static class FormasDePerdida
{
    private const string Ninguna = "ninguna";
    private const string PorInasistencia = "por";

    private static readonly HashSet<string> Palabras = new(StringComparer.Ordinal)
    {
        "perdi", "perdio", "perdiste", "perdieron", "perdido", "perdidos", "perdida", "perdidas"
    };

    private static readonly HashSet<string> Verbos = new(StringComparer.Ordinal)
    {
        "tiene", "tienen", "tengo", "tienes", "tener", "hay", "registra", "registran"
    };

    private static readonly HashSet<string> Determinantes = new(StringComparer.Ordinal)
    {
        "una", "unas", "alguna", "algunas", Ninguna, "sus", "dos", "tres", "cuatro"
    };

    private static readonly HashSet<string> Sustantivos = new(StringComparer.Ordinal)
    {
        "asignatura", "asignaturas", "materia", "materias"
    };

    private static readonly HashSet<string> Participios = new(StringComparer.Ordinal) { "perdida", "perdidas" };

    private static readonly HashSet<string> NegacionesPrevias = new(StringComparer.Ordinal)
    {
        "no", "ni", "sin", "ningun", Ninguna, "ninguno", "cero"
    };

    private static readonly HashSet<string> NegacionesPosteriores = new(StringComparer.Ordinal)
    {
        Ninguna, "ninguno", "ningun", "no", "cero", "0"
    };

    internal static readonly HashSet<string> Inasistencias = new(StringComparer.Ordinal) { "inasistencia", "inasistencias" };

    internal static readonly HashSet<string> Auxiliares = new(StringComparer.Ordinal)
    {
        "ha", "han", "has", "he", "hemos", "hay", "tiene", "tienen", "tienes", "tengo", "registra", "registran", "es", "son", "esta", "estan", "fue"
    };

    public static IReadOnlyList<FormaDePerdida> En(IReadOnlyList<string> palabras) =>
        [.. Enumerable.Range(0, palabras.Count).Select(indice => FormaEn(palabras, indice)).OfType<FormaDePerdida>()];

    private static FormaDePerdida? FormaEn(IReadOnlyList<string> palabras, int indice)
    {
        if (EsNominal(palabras, indice))
        {
            return Nominal(palabras, indice);
        }

        return Palabras.Contains(palabras[indice]) ? Construir(palabras, indice, indice, nominal: false) : null;
    }

    private static bool EsNominal(IReadOnlyList<string> palabras, int indice) =>
        Participios.Contains(palabras[indice]) && indice > 0 && Sustantivos.Contains(palabras[indice - 1]);

    private static FormaDePerdida Nominal(IReadOnlyList<string> palabras, int indice)
    {
        var inicio = indice - 1;
        inicio -= inicio > 0 && Determinantes.Contains(palabras[inicio - 1]) ? 1 : 0;
        inicio -= inicio > 0 && Verbos.Contains(palabras[inicio - 1]) ? 1 : 0;
        return Construir(palabras, inicio, indice, nominal: true);
    }

    private static FormaDePerdida Construir(IReadOnlyList<string> palabras, int inicio, int fin, bool nominal) =>
        new(inicio, fin, nominal, EstaNegada(palabras, inicio, fin), nominal && SeguidaDePorInasistencia(palabras, fin));

    private static bool EstaNegada(IReadOnlyList<string> palabras, int inicio, int fin) =>
        NegadaAntes(palabras, inicio)
        || palabras.Skip(inicio).Take(fin - inicio).Contains(Ninguna)
        || (fin + 1 < palabras.Count && NegacionesPosteriores.Contains(palabras[fin + 1]));

    private static bool NegadaAntes(IReadOnlyList<string> palabras, int inicio)
    {
        var anterior = inicio - 1 - (inicio > 0 && Auxiliares.Contains(palabras[inicio - 1]) ? 1 : 0);
        return anterior >= 0 && NegacionesPrevias.Contains(palabras[anterior]);
    }

    private static bool SeguidaDePorInasistencia(IReadOnlyList<string> palabras, int fin) =>
        fin + 2 < palabras.Count && palabras[fin + 1] == PorInasistencia && Inasistencias.Contains(palabras[fin + 2]);
}
