using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

internal static class SalidasVerificadas
{
    public static Func<SolicitudLlm, int, RespuestaLlm> LeeElProgramaYResponde(string texto) =>
        (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto(texto);

    public static Func<SolicitudLlm, int, RespuestaLlm> LeeElProgramaYElDetalleYResponde(string texto, string estudianteId) =>
        (_, llamada) => llamada == 0
            ? new RespuestaLlm(
                null,
                [
                    new LlamadaHerramienta("lectura-programa", HerramientasAsistente.Programa, "{}"),
                    new LlamadaHerramienta("lectura-detalle", HerramientasAsistente.Estudiante, $$"""{"{{HerramientasAsistente.ParametroEstudianteId}}":"{{estudianteId}}"}""")
                ],
                new UsoTokens(10, 5),
                false)
            : Texto(texto);

    public static void AfirmarEstadoNoVerificado(ResultadoPregunta resultado, EscenarioAsistente escenario, bool usoDatos)
    {
        var respuesta = resultado.Respuesta!;
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Equal(usoDatos, respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadosNuevosDeAuditoria.EstadoNoVerificado, resultado.Auditoria);
        Assert.Equal(ResultadosNuevosDeAuditoria.EstadoNoVerificado, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }

    public static void AfirmarRespondida(ResultadoPregunta resultado, EscenarioAsistente escenario, string texto)
    {
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }
}
