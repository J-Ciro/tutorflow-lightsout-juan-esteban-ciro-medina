using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class PerdidaGenericaApiTests
{
    private const string PreguntaHabilitar = "¿Con qué nota puedo habilitar una materia?";
    private const string PreguntaPerdidas = "¿Qué dice el reglamento sobre la habilitación?";
    private const string CitaArticulo6 = "reglamento_academico_2025.md · Artículo 6";
    private const string TextoGenerico = "Puedes habilitar con definitiva igual o superior a 2.0 y menor a 3.0; no se habilitan asignaturas perdidas por inasistencia.";

    public static TheoryData<string> OracionesGenericasSinMarcador => new()
    {
        TextoGenerico,
        "Las asignaturas perdidas por inasistencia no son habilitables.",
        "Una asignatura perdida por inasistencia no se puede habilitar."
    };

    public static TheoryData<string> OracionesPersonalesONominales => new()
    {
        "Asignaturas perdidas.",
        "Hay dos asignaturas perdidas.",
        "Tu registro tiene asignaturas perdidas por inasistencia."
    };

    public static TheoryData<string> AfirmacionesConAsignaturaAprobada => new()
    {
        "Tienes Estadística perdida.",
        "Perdiste Estadística.",
        "Estadística perdida por inasistencia; no se habilita."
    };

    private static void AfirmarEstadoNoVerificado(RespuestaApi respuesta, FabricaApi fabrica)
    {
        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadosNuevosDeAuditoria.EstadoNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-035")]
    [Trait("D", "D-66")]
    public async Task ESC_035a_La_oracion_generica_de_habilitacion_sin_marcador_personal_se_entrega_con_uso_false()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => TextoGenerico), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaHabilitar);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(TextoGenerico, respuesta.Respuesta);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-03")]
    [Trait("ESC", "ESC-011")]
    [Trait("D", "D-66")]
    public async Task ESC_011_La_habilitacion_entrega_2_0_sin_1_5_con_el_articulo_6_y_sin_verificar_la_oracion_generica_contra_EST_001()
    {
        var texto = $"{TextoGenerico} ({CitaArticulo6})";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaHabilitar);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("2.0", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("1.5", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(CitaArticulo6, respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(OracionesGenericasSinMarcador))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-035")]
    [Trait("D", "D-66")]
    public async Task ESC_035ab_Una_oracion_generica_sin_marcador_personal_no_se_trata_como_perdida_del_estudiante(string texto)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaHabilitar);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-035")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_035c_Tienes_asignaturas_perdidas_sin_ninguna_perdida_en_los_datos_da_texto_no_verificable()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "Tienes asignaturas perdidas."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPerdidas);

        AfirmarEstadoNoVerificado(respuesta, fabrica);
    }

    [Theory]
    [MemberData(nameof(OracionesPersonalesONominales))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-035")]
    [Trait("D", "D-66")]
    public async Task ESC_035d_Un_fragmento_nominal_o_con_marcador_personal_sin_perdida_en_los_datos_da_texto_no_verificable(string texto)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPerdidas);

        AfirmarEstadoNoVerificado(respuesta, fabrica);
    }

    [Theory]
    [MemberData(nameof(AfirmacionesConAsignaturaAprobada))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-035")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_035e_La_perdida_de_una_asignatura_aprobada_da_texto_no_verificable(string texto)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaPerdidas);

        AfirmarEstadoNoVerificado(respuesta, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-035")]
    [Trait("SEG", "SEG-10b")]
    public async Task ESC_035f_La_perdida_atribuida_a_un_tercero_da_la_negativa_y_fuga_bloqueada_sin_reintento()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "Andrés Martínez tiene asignaturas perdidas."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPerdidas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NegativaEstudiante, respuesta.Respuesta);
        Assert.Equal([Afirmaciones.CitaNegativa], respuesta.Citas);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, evento.Resultado);
        Assert.False(AfirmacionesDeReintento.ReintentoRegistrado(evento));
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-035")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_035g_Tienes_asignaturas_perdidas_con_las_dos_perdidas_reales_de_EST_002_se_entrega()
    {
        const string texto = "Tienes asignaturas perdidas: Cálculo Integral y Bases de Datos por inasistencia.";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-002", PreguntaPerdidas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
