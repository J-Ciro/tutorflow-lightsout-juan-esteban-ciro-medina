using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Conocimiento;

public static class ConsultaDeRecuperacion
{
    public const int LongitudDeRaiz = 4;

    public static string DeTexto(string texto) =>
        string.Join(
            ' ',
            NormalizadorTexto.Normalizar(texto)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(palabra => palabra.Length >= LongitudDeRaiz)
                .Select(palabra => palabra[..LongitudDeRaiz])
                .Distinct());
}
