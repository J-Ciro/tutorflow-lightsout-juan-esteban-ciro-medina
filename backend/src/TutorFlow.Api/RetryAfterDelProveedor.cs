using System.Globalization;

namespace TutorFlow.Api;

internal static class RetryAfterDelProveedor
{
    private const int MinimoSegundos = 1;
    private const int MaximoSegundos = 3600;
    private const string FormatoFechaHttp = "r";

    public static int? Segundos(string? valor, DateTimeOffset ahora)
    {
        var segundos = SegundosEnteros(valor) ?? SegundosHastaFecha(valor, ahora);
        return segundos is >= MinimoSegundos and <= MaximoSegundos ? (int)segundos : null;
    }

    private static long? SegundosEnteros(string? valor) =>
        !string.IsNullOrEmpty(valor) && valor.All(char.IsAsciiDigit) && long.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var segundos)
            ? segundos
            : null;

    private static long? SegundosHastaFecha(string? valor, DateTimeOffset ahora) =>
        DateTimeOffset.TryParseExact(valor, FormatoFechaHttp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fecha)
            ? (long)Math.Ceiling((fecha - ahora).TotalSeconds)
            : null;
}
