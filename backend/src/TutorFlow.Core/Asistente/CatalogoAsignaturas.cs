using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public sealed record AsignaturaCatalogo(string Codigo, string Nombre);

internal sealed record AparicionDeAsignatura(string Clave, int Inicio, int Fin);

internal static class CatalogoAsignaturas
{
    public static IReadOnlyList<AsignaturaCatalogo> Vigente(IReadOnlyList<AsignaturaCatalogo> catalogo, DirectorioAcademico directorio) =>
        [.. catalogo.Where(asignatura => !EsIdDePersona(asignatura.Codigo, directorio))];

    public static bool EsIdDePersona(string codigo, DirectorioAcademico directorio) =>
        directorio.Estudiantes.Any(estudiante => estudiante.Id == codigo) || directorio.Coordinadores.Any(coordinador => coordinador.Id == codigo);

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

    public static IReadOnlyList<AparicionDeAsignatura> Apariciones(
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<AsignaturaCatalogo> catalogo) =>
        [.. catalogo.SelectMany(asignatura => new[] { asignatura.Nombre, asignatura.Codigo }
            .Select(TokenizadorTexto.Valores)
            .Where(secuencia => secuencia.Count > 0)
            .SelectMany(secuencia => TokenizadorTexto.BuscarSecuencias(tokens, secuencia)
                .Select(inicio => new AparicionDeAsignatura(AmbitoPersonal.Clave(asignatura.Codigo), inicio, inicio + secuencia.Count - 1))))];

    public static List<AparicionDeAsignatura> PrimerasApariciones(IReadOnlyList<TokenTexto> tokens, IReadOnlyList<AsignaturaCatalogo> catalogo) =>
        [.. Apariciones(tokens, catalogo)
            .GroupBy(aparicion => aparicion.Clave)
            .Select(grupo => grupo.MinBy(aparicion => aparicion.Inicio)!)
            .OrderBy(aparicion => aparicion.Inicio)];

    private static bool Aparece(IReadOnlyList<TokenTexto> tokens, string texto)
    {
        var secuencia = TokenizadorTexto.Valores(texto);
        return secuencia.Count > 0 && TokenizadorTexto.BuscarSecuencia(tokens, secuencia) >= 0;
    }
}
