namespace TutorFlow.Core.Conocimiento;

public static class PalabrasVacias
{
    private static readonly HashSet<string> Todas = new(StringComparer.Ordinal)
    {
        "que", "cual", "cuales", "como", "cuando", "donde", "quien", "cuanto", "cuantos", "cuanta", "cuantas",
        "hay", "puedo", "tengo", "tiene", "tienen", "sobre", "para", "por", "con", "del", "los", "las", "una",
        "unos", "unas", "mis", "tus", "sus", "mas", "pero", "esta", "este", "esto", "ese", "esa", "son", "ser",
        "dice", "dicen", "hacer", "hace", "debe", "puede", "pueden", "estoy", "estan", "dime", "dame", "muestra"
    };

    public static bool Contiene(string palabra) => Todas.Contains(palabra);
}
