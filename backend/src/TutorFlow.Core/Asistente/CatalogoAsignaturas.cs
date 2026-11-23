using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public sealed record AsignaturaCatalogo(string Codigo, string Nombre);

internal static class CatalogoAsignaturas
{
    public static IReadOnlyList<AsignaturaCatalogo> PorDefecto { get; } =
    [
        new("CAL-201", "Cálculo Integral"),
        new("POO-202", "Programación Orientada a Objetos"),
        new("BD-203", "Bases de Datos"),
        new("ING-204", "Inglés III"),
        new("CON-101", "Contabilidad"),
        new("MIC-102", "Microeconomía"),
        new("EST-103", "Estadística"),
        new("COM-104", "Comunicación Oral y Escrita")
    ];

    public static IReadOnlyList<string> CodigosNombrados(
        string pregunta,
        IReadOnlyList<AsignaturaCatalogo> catalogo,
        IReadOnlyCollection<string> codigosDelDirectorio)
    {
        var tokens = TokenizadorTexto.Tokenizar(pregunta);
        var porNombre = catalogo.Where(a => Aparece(tokens, a.Nombre)).Select(a => a.Codigo);
        var porCodigo = codigosDelDirectorio.Concat(catalogo.Select(a => a.Codigo)).Where(codigo => Aparece(tokens, codigo));
        return [.. porNombre.Concat(porCodigo).Select(AmbitoPersonal.Clave).Distinct(StringComparer.Ordinal)];
    }

    private static bool Aparece(IReadOnlyList<TokenTexto> tokens, string texto)
    {
        var secuencia = TokenizadorTexto.Valores(texto);
        return secuencia.Count > 0 && TokenizadorTexto.BuscarSecuencia(tokens, secuencia) >= 0;
    }
}
