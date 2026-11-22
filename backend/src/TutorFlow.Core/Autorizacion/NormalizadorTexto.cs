namespace TutorFlow.Core.Autorizacion;

public static class NormalizadorTexto
{
    public static string Normalizar(string texto) =>
        string.Join(' ', TokenizadorTexto.Valores(texto));
}
