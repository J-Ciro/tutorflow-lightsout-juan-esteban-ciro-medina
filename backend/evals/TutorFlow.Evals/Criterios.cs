namespace TutorFlow.Evals;

public static class Criterios
{
    public const string Codigo = "código";
    public const string Cifra = "cifra";
    public const string Dato = "dato completo";
    public const string Citas = "citas";
    public const string Texto = "texto";

    public static IReadOnlyList<string> DeLaTabla { get; } = [Codigo, Cifra, Dato, Citas];

    public static string Etiquetar(string criterio, string mensaje) => $"[{criterio}] {mensaje}";

    public static string DeLaCifraOElDato(CasoEval caso, string texto) =>
        EsCifra(caso, texto) ? Cifra : Dato;

    public static string De(string fallo)
    {
        var cierre = fallo.IndexOf(']', StringComparison.Ordinal);
        return fallo.StartsWith('[') && cierre > 0 ? fallo[1..cierre] : Texto;
    }

    public static bool Aplica(CasoEval caso, string criterio) =>
        criterio switch
        {
            Cifra => caso.Cifras is { Count: > 0 },
            Dato => TieneDatos(caso),
            Citas => TieneCitas(caso),
            _ => true
        };

    private static bool EsCifra(CasoEval caso, string texto) => caso.Cifras?.Contains(texto) ?? false;

    private static bool TieneDatos(CasoEval caso) =>
        caso.Contiene.Any(texto => !EsCifra(caso, texto)) || caso.Parejas is { Count: > 0 } || caso.ContieneAlguna is { Count: > 0 };

    private static bool TieneCitas(CasoEval caso) =>
        caso.CitasIncluye.Count > 0 || caso.CitasProhibidas.Count > 0 || caso.CitasVacias || TieneCitasExactas(caso);

    private static bool TieneCitasExactas(CasoEval caso) =>
        caso.CitasExactas is not null || caso.CitasDocumentoExactas is not null;
}
