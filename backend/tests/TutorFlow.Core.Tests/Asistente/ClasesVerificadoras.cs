using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

internal static class ClasesVerificadoras
{
    public static Usuario Usuario(string id) =>
        new(id.StartsWith("COORD", StringComparison.Ordinal) ? Rol.Coordinador : Rol.Estudiante, id);

    public static async Task<string?> MotivoAsync(string id, string texto, params string[] leidos)
    {
        var usuario = Usuario(id);
        var api = new ApiAcademicaDoble(DatosReferencia.Cargar());
        var directorio = (await ConstructorDirectorio.ConstruirAsync(usuario, api, [], CancellationToken.None))!;
        var resumenes = new Dictionary<string, ResumenEstudiante>();
        foreach (var leido in leidos)
        {
            resumenes[leido] = await ResumenesAcademicos.ResumirAsync(api, leido, CancellationToken.None);
        }

        var autorizados = new PoliticaAcceso().IdsPersonasAutorizadas(usuario, directorio);
        var bloques = BloquesDeRespuesta.De(texto);
        var catalogo = CatalogoDePrueba.Asignaturas;
        var contexto = new ContextoDeAfirmaciones(usuario, autorizados, directorio, resumenes);
        return new AfirmacionesDePerdida(catalogo).Motivo(bloques, usuario, autorizados, directorio, resumenes)
            ?? new AfirmacionesDeEstado(catalogo).Motivo(bloques, contexto)
            ?? new DefinitivaPorAsignatura(catalogo).Motivo(bloques, usuario, autorizados, directorio, resumenes)
            ?? CamposLigados.Motivo(bloques, contexto);
    }

    public static async Task AfirmarFallaAsync(string id, string texto, params string[] leidos) =>
        Assert.NotNull(await MotivoAsync(id, texto, leidos));

    public static async Task AfirmarPasaAsync(string id, string texto, params string[] leidos) =>
        Assert.Null(await MotivoAsync(id, texto, leidos));
}
