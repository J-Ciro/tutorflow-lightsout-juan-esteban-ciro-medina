using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class IdsNoAutorizadosApiTests
{
    private const string PreguntaPerdidasAdm = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaPerdidasIngSis = "¿Qué estudiantes de mi programa perdieron materias?";
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";

    private static void AfirmarNegativaPorFuga(RespuestaApi respuesta, FabricaApi fabrica, string negativa)
    {
        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(negativa, respuesta.Respuesta);
        Assert.Equal([Afirmaciones.CitaNegativa], respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_176a_Un_id_inexistente_sin_separador_se_bloquea_con_la_negativa_del_coordinador()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "EST007 tiene Estadística perdida."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidasAdm);

        AfirmarNegativaPorFuga(respuesta, fabrica, Textos.NegativaCoordinador);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_176b_El_id_de_un_estudiante_de_otro_programa_se_bloquea()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "EST-004 tiene todas sus asignaturas aprobadas."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", PreguntaPerdidasIngSis);

        AfirmarNegativaPorFuga(respuesta, fabrica, Textos.NegativaCoordinador);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Theory]
    [InlineData("Según COORD-07, tu promedio es 4.39.")]
    [InlineData("EST 099 tiene un promedio de 4.39.")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_176cd_Un_id_inventado_en_la_respuesta_a_un_estudiante_se_bloquea(string texto)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPromedio);

        AfirmarNegativaPorFuga(respuesta, fabrica, Textos.NegativaEstudiante);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Theory]
    [InlineData("Mateo Vargas tiene Estadística (EST-103) sin definitiva.")]
    [InlineData("Valentina Castro (EST-005) está en prueba académica.")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_176e_Los_codigos_de_asignatura_y_los_ids_autorizados_no_se_bloquean(string texto)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidasAdm);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-176")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_176_La_salida_es_identica_byte_a_byte_si_el_id_existe_o_no()
    {
        var inexistente = await ResponderAsync("EST007 tiene Estadística perdida.");
        var existente = await ResponderAsync("EST-001 tiene Estadística perdida.");

        Assert.Equal(existente.Bytes, inexistente.Bytes);
        Assert.Equal(Textos.NegativaCoordinador, inexistente.Respuesta);
    }

    private static async Task<RespuestaApi> ResponderAsync(string texto)
    {
        var llm = GuionesLlm.SinHerramientas(_ => texto);
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);
        return await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidasAdm);
    }
}
