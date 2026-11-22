using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class FalsosPositivosApiTests
{
    public static TheoryData<string, string, string, string> PreguntasLegitimas => new()
    {
        { "estudiante", "EST-006", "¿Cuál es mi nota en EST-103?", "ESC-120" },
        { "estudiante", "EST-004", "¿Cuántas notas rojas tengo?", "ESC-121" },
        { "estudiante", "EST-003", "¿Cómo le pido a Marcela Ríos la revisión de mis faltas de Inglés III?", "ESC-122" },
        { "estudiante", "EST-001", "¿Quién es mi coordinador?", "ESC-123" },
        { "coordinador", "COORD-02", "¿Cuántos estudiantes de Administración de Empresas están en prueba académica?", "ESC-124" },
        { "coordinador", "COORD-01", "¿Qué trámite administrativo se hace para cancelar una asignatura?", "ESC-125" },
        { "estudiante", "EST-005", "¿Puedo ir a tutorías con mis compañeros?", "ESC-126" },
        { "estudiante", "EST-001", "¿Qué promedio mínimo pide la beca?", "ESC-127" },
        { "estudiante", "EST-004", "Mi hermano se llama Mateo, ¿cuál es mi promedio?", "ESC-128" },
        { "coordinador", "COORD-01", "¿Cómo va Andrés Martínez?", "ESC-129" },
        { "estudiante", "EST-001", "¿Cuál es el promedio de Laura Gómez?", "ESC-034" },
        { "estudiante", "EST-001", "¿Andrés pasó Cálculo?", "ESC-138" }
    };

    [Theory]
    [MemberData(nameof(PreguntasLegitimas))]
    [Trait("CA", "CA-12")]
    public async Task Pregunta_legitima_llega_al_llm_y_responde_200(string rol, string id, string pregunta, string referencia)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarAsync(rol, id, pregunta);

        Assert.True(respuesta.Estado == 200, referencia);
        Assert.Equal(1, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-120")]
    public async Task ESC_120_Est_103_es_la_asignatura_y_las_herramientas_leen_los_datos_propios()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-006", "¿Cuál es mi nota en EST-103?");

        Assert.Equal(200, respuesta.Estado);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains("2.4", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Equal(0, fabrica.Api.LecturasDatosDe("EST-103"));
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-123")]
    public async Task ESC_123_El_estudiante_puede_conocer_el_nombre_de_su_coordinador()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Quién es mi coordinador?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("Marcela Ríos", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Datos del estudiante · Datos básicos", respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-138")]
    public async Task ESC_138_Nombre_suelto_de_un_tercero_no_filtra_datos_porque_el_estudiante_no_tiene_herramientas_ajenas()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Andrés pasó Cálculo?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(0, fabrica.Api.LecturasDatosDe("EST-002"));
        Assert.DoesNotContain("EST-002", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("2.28", respuesta.Respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-129")]
    public async Task ESC_129_Coordinador_consulta_a_un_estudiante_de_su_programa_por_nombre()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos("EST-002"));

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", "¿Cómo va Andrés Martínez?");

        Assert.Equal(200, respuesta.Estado);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains("2.28", respuesta.Respuesta, StringComparison.Ordinal);
    }
}

public sealed class MetadatosApiTests
{
    public static TheoryData<string, string, bool> UsoDeDatos => new()
    {
        { "EST-001", "¿Cómo se calcula la nota definitiva de una asignatura?", false },
        { "EST-004", "¿Qué requisitos tiene la beca por rendimiento académico?", false },
        { "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", true }
    };

    [Fact]
    [Trait("CA", "CA-11")]
    public async Task Costo_se_calcula_con_los_precios_configurados()
    {
        var llm = ClienteLlmDoble.Texto("Respuesta de prueba.", new UsoTokens(1000, 200));
        using var fabrica = new FabricaApi(llm: llm, configuracion: FabricaApi.Precios("1.0", "5.0"));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(1200, respuesta.Tokens);
        Assert.Equal(0.002m, respuesta.CostoUsd);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("ESC", "ESC-106")]
    public async Task ESC_106_Tokens_y_costo_suman_todas_las_invocaciones_al_llm()
    {
        var llm = ClienteLlmDoble.ConsultaDatos(uso: new UsoTokens(1000, 200));
        using var fabrica = new FabricaApi(llm: llm, configuracion: FabricaApi.Precios("1.0", "5.0"));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(fabrica.Llm.Llamadas * 1200, respuesta.Tokens);
        Assert.Equal(fabrica.Llm.Llamadas * 0.002m, respuesta.CostoUsd);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("ESC", "ESC-106")]
    public async Task ESC_106_El_403_previo_reporta_tokens_y_costo_cero()
    {
        using var fabrica = new FabricaApi(configuracion: FabricaApi.Precios("1.0", "5.0"));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?");

        Assert.Equal(0, respuesta.Tokens);
        Assert.Equal(0m, respuesta.CostoUsd);
    }

    [Theory]
    [MemberData(nameof(UsoDeDatos))]
    [Trait("CA", "CA-11")]
    [Trait("SEG", "COMP-04")]
    public async Task Uso_datos_estudiante_es_verdadero_solo_si_se_leyeron_datos(string id, string pregunta, bool esperado)
    {
        var llm = esperado ? ClienteLlmDoble.ConsultaDatos() : ClienteLlmDoble.Texto("Respuesta normativa.");
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync(id, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(esperado, respuesta.UsoDatosEstudiante);
        Assert.Equal(esperado, fabrica.Api.Llamadas.Any(l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-009")]
    public async Task ESC_009_Sin_informacion_responde_200_con_el_texto_exacto_sin_citas_ni_datos()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto(Textos.SinInformacion));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Qué dice el reglamento sobre el uso de parqueaderos?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("No encuentro esa información en los documentos disponibles.", respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-094")]
    public async Task ESC_094_Pregunta_sin_contenido_no_es_400()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto(Textos.SinInformacion));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "???");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
    }
}
