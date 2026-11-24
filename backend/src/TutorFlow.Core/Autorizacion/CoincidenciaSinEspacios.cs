using System.Text;

namespace TutorFlow.Core.Autorizacion;

internal static class CoincidenciaSinEspacios
{
    public const int MinimoLetrasDelNombre = 8;

    public static bool Aplica(IReadOnlyList<string> nombre) =>
        nombre.Sum(token => token.Length) >= MinimoLetrasDelNombre;

    public static IEnumerable<(int Primero, int Ultimo)> Buscar(IReadOnlyList<TokenTexto> tokens, string objetivo) =>
        Enumerable.Range(0, tokens.Count)
            .Select(inicio => (Primero: inicio, Ultimo: UltimoQueCompleta(tokens, inicio, objetivo)))
            .Where(tramo => tramo.Ultimo >= 0);

    private static int UltimoQueCompleta(IReadOnlyList<TokenTexto> tokens, int inicio, string objetivo)
    {
        var acumulado = new StringBuilder();
        for (var indice = inicio; indice < tokens.Count && acumulado.Length < objetivo.Length; indice++)
        {
            acumulado.Append(tokens[indice].Valor);
            if (acumulado.Length == objetivo.Length && acumulado.ToString() == objetivo)
            {
                return indice;
            }
        }

        return -1;
    }
}
