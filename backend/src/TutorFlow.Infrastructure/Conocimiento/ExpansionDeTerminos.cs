namespace TutorFlow.Infrastructure.Conocimiento;

internal static class ExpansionDeTerminos
{
    private static readonly Dictionary<string, string[]> TerminosRelacionados = new Dictionary<string, string[]>
    {
        ["defi"] = ["cort", "peso"],
        ["pond"] = ["defi", "cred"],
        ["prom"] = ["defi", "cred"]
    };

    public static IReadOnlyList<string> Expandir(IEnumerable<string> raices)
    {
        var originales = raices.ToList();
        return [.. originales.Concat(originales.SelectMany(Relacionados)).Distinct()];
    }

    private static IEnumerable<string> Relacionados(string raiz) =>
        TerminosRelacionados.TryGetValue(raiz, out var relacionados) ? relacionados : [];
}
