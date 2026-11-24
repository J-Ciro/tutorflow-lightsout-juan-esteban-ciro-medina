using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static partial class VariantesDeTexto
{
    private const int LongitudMinimaDeBase64 = 16;

    public static IReadOnlyList<string> De(string texto)
    {
        string[] bases = [texto, WebUtility.HtmlDecode(texto)];
        return
        [
            .. bases,
            .. bases.Select(Invertido),
            .. bases.SelectMany(DecodificadosDeBase64)
        ];
    }

    private static string Invertido(string texto) => new([.. texto.AsEnumerable().Reverse()]);

    private static IEnumerable<string> DecodificadosDeBase64(string texto) =>
        CandidatoDeBase64().Matches(texto)
            .Select(coincidencia => Decodificar(coincidencia.Value))
            .OfType<string>();

    private static string? Decodificar(string candidato)
    {
        var buffer = new byte[candidato.Length];
        return Convert.TryFromBase64String(candidato, buffer, out var escritos)
            ? Encoding.UTF8.GetString(buffer, 0, escritos)
            : null;
    }

    [GeneratedRegex(@"[A-Za-z0-9+/]{16,}={0,2}", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CandidatoDeBase64();
}
