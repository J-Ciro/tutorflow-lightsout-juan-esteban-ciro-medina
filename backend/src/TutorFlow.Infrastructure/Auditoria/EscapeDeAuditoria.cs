using System.Globalization;
using System.Text;

namespace TutorFlow.Infrastructure.Auditoria;

internal static class EscapeDeAuditoria
{
    private static readonly HashSet<UnicodeCategory> CategoriasEscapadas =
    [
        UnicodeCategory.Control,
        UnicodeCategory.Format,
        UnicodeCategory.LineSeparator,
        UnicodeCategory.ParagraphSeparator
    ];

    public static string? Aplicar(string? texto)
    {
        if (texto is null)
        {
            return null;
        }

        var resultado = new StringBuilder(texto.Length);
        foreach (var runa in texto.EnumerateRunes())
        {
            AgregarEscapada(resultado, runa);
        }

        return resultado.ToString();
    }

    private static void AgregarEscapada(StringBuilder resultado, Rune runa)
    {
        if (!CategoriasEscapadas.Contains(Rune.GetUnicodeCategory(runa)))
        {
            resultado.Append(runa);
            return;
        }

        Span<char> unidades = stackalloc char[2];
        var cantidad = runa.EncodeToUtf16(unidades);
        foreach (var unidad in unidades[..cantidad])
        {
            resultado.Append(CultureInfo.InvariantCulture, $"\\u{(int)unidad:X4}");
        }
    }
}
