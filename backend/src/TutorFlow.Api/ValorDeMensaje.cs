namespace TutorFlow.Api;

internal static class ValorDeMensaje
{
    private const int LongitudMaxima = 64;
    private const string PuntosSuspensivos = "…";

    public static string De(string? valor)
    {
        var escapado = string.Concat((valor ?? string.Empty).Select(Escapar));
        return escapado.Length <= LongitudMaxima ? escapado : escapado[..Corte(escapado)] + PuntosSuspensivos;
    }

    private static int Corte(string escapado) =>
        char.IsHighSurrogate(escapado[LongitudMaxima - 1]) ? LongitudMaxima - 1 : LongitudMaxima;

    private static string Escapar(char caracter) => caracter switch
    {
        '"' => "\\\"",
        '\\' => "\\\\",
        '\n' => "\\n",
        '\r' => "\\r",
        '\t' => "\\t",
        _ when char.IsControl(caracter) => $"\\u{(int)caracter:X4}",
        _ => caracter.ToString()
    };
}
