using System.Globalization;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class CamposLigados
{
    private const string Promedio = "promedio";
    private const int DecimalesDelPromedio = 2;
    private const string MotivoDeVoz = "escribe los datos del estudiante en tercera persona, con su nombre";

    public static string? Motivo(IReadOnlyList<BloqueDeTexto> bloques, ContextoDeAfirmaciones contexto) =>
        bloques.Select(bloque => MotivoDelBloque(bloque, contexto)).FirstOrDefault(motivo => motivo is not null);

    private static string? MotivoDelBloque(BloqueDeTexto bloque, ContextoDeAfirmaciones contexto)
    {
        var personas = contexto.PersonasDe(bloque);
        var tokens = TokenizadorTexto.TokenizarConCifras(bloque.Texto);
        return personas.Tramos.Select(tramo => MotivoDelTramo(bloque, tokens, tramo, contexto)).FirstOrDefault(motivo => motivo is not null)
            ?? (VozEquivocada(tokens, personas, contexto.Usuario) ? MotivoDeVoz : null);
    }

    private static string? MotivoDelTramo(BloqueDeTexto bloque, IReadOnlyList<TokenTexto> tokens, TramoDePersona tramo, ContextoDeAfirmaciones contexto)
    {
        if (!contexto.Resumenes.TryGetValue(tramo.PersonaId, out var resumen))
        {
            return null;
        }

        var inicios = tramo.Tokens.Select(token => token.Inicio).ToHashSet();
        var delTramo = tokens.Where(token => inicios.Contains(token.Inicio)).ToList();
        var cifra = PromedioQueNoCuadra(delTramo, resumen) ?? EnteroQueNoCuadra(bloque.Texto, delTramo, resumen);
        return cifra is null ? null : $"el valor {cifra} no coincide con los datos entregados";
    }

    private static string? PromedioQueNoCuadra(List<TokenTexto> tokens, ResumenEstudiante resumen) =>
        Enumerable.Range(0, tokens.Count)
            .Where(indice => tokens[indice].Valor == Promedio && !DefinitivaPorAsignatura.EstaNegada(tokens, indice))
            .Select(indice => DefinitivaPorAsignatura.CifraEfectiva(tokens, indice))
            .OfType<int>()
            .Select(posicion => tokens[posicion].Valor)
            .FirstOrDefault(cifra => !IgualAlPromedio(cifra, resumen.Promedio));

    private static bool IgualAlPromedio(string cifra, decimal? promedio) =>
        promedio is { } valor && decimal.Parse(cifra, CultureInfo.InvariantCulture) == CalculosAcademicos.Redondear(valor, DecimalesDelPromedio);

    private static string? EnteroQueNoCuadra(string texto, List<TokenTexto> tokens, ResumenEstudiante resumen)
    {
        var permitidos = EnterosDeAsistencia.De(resumen);
        return EnterosDeAsistencia.Candidatos(texto, tokens).FirstOrDefault(entero => !permitidos.Contains(entero));
    }

    private static bool VozEquivocada(IReadOnlyList<TokenTexto> tokens, PersonasDeBloque personas, Usuario usuario) =>
        usuario.Rol == Rol.Coordinador && personas is { Ambiguo: false, Tramos.Count: 0 } && VozDeTu.Hay(tokens);
}
