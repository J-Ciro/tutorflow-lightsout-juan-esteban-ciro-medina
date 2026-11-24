namespace TutorFlow.Core.Asistente;

internal static class VentanaDePalabras
{
    public static IEnumerable<int> Posiciones(int cantidad, int indice, int radio)
    {
        var inicio = Math.Max(0, indice - radio);
        var fin = Math.Min(cantidad - 1, indice + radio);
        return Enumerable.Range(inicio, Math.Max(0, fin - inicio + 1));
    }
}
