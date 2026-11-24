using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class ResumenPropioApiTests
{
    private const string FaltasEnBasesDeDatos = "¿Cuántas faltas tengo en Bases de Datos?";
    private const string CitaAsistencia = "Datos del estudiante · Asistencia";
    private static readonly string[] CodigosDeAsignatura = ["CAL-201", "POO-202", "BD-203", "ING-204", "CON-101", "MIC-102", "EST-103", "COM-104"];

    public static TheoryData<string, string> DefinitivasDeEst001 => new()
    {
        { "Cálculo Integral", "4.5" },
        { "Programación Orientada a Objetos", "4.8" },
        { "Bases de Datos", "4.1" },
        { "Inglés III", "4.0" }
    };

    public static TheoryData<string, string, string> SinActivador => new()
    {
        { "estudiante", "EST-004", "¿Qué requisitos tiene la beca por rendimiento académico?" },
        { "estudiante", "EST-001", "¿Hasta qué semana puedo cancelar una asignatura?" },
        { "coordinador", "COORD-01", "¿Cuál es mi promedio?" }
    };

    private static string TextoEnviado(SolicitudLlm solicitud) =>
        solicitud.PromptSistema + "\n" + string.Join("\n", solicitud.Mensajes.Select(m => m.Contenido));

    private static IEnumerable<string> PersonasAjenasA(string estudianteId)
    {
        var datos = DatosReferencia.Cargar();
        var propio = datos.Estudiantes.Single(e => e.Id == estudianteId);
        var coordinadorPropio = datos.Programas.Single(p => p.Id == propio.ProgramaId).CoordinadorId;
        return datos.Estudiantes.Where(e => e.Id != estudianteId).SelectMany(e => new[] { e.Nombre, e.Id })
            .Concat(datos.Coordinadores.Where(c => c.Id != coordinadorPropio).SelectMany(c => new[] { c.Nombre, c.Id }));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-154")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_154_Un_modelo_que_nunca_llama_herramientas_recibe_en_su_unica_llamada_el_resumen_delimitado()
    {
        var llm = GuionesLlm.SinHerramientas(s => GuionesLlm.LineaDe(s, "Bases de Datos"));
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", FaltasEnBasesDeDatos);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(1, fabrica.Llm.Llamadas);
        var bloque = GuionesLlm.BloqueDeResumen(fabrica.Llm.Solicitudes[0], "Bases de Datos");
        Assert.NotNull(bloque);
        var linea = GuionesLlm.LineaDe(fabrica.Llm.Solicitudes[0], "Bases de Datos");
        Assert.Matches(@"\b2\b", linea);
        Assert.Matches(@"\b48\b", linea);
        Assert.Matches(@"\b7\b", linea);
        Assert.Matches(@"\b5\b", linea);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-154")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_154_La_respuesta_con_las_faltas_pasa_la_verificacion_y_cita_la_asistencia()
    {
        var llm = GuionesLlm.SinHerramientas(s => GuionesLlm.LineaDe(s, "Bases de Datos"));
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", FaltasEnBasesDeDatos);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("2", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("48", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(CitaAsistencia, respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-154")]
    [Trait("SEG", "SEG-09")]
    public async Task ESC_154_El_resumen_no_contiene_nombres_ni_ids_de_otros_estudiantes()
    {
        var llm = GuionesLlm.SinHerramientas(s => GuionesLlm.LineaDe(s, "Bases de Datos"));
        using var fabrica = new FabricaApi(llm: llm);

        await fabrica.PreguntarEstudianteAsync("EST-001", FaltasEnBasesDeDatos);

        var bloque = GuionesLlm.BloqueDeResumen(fabrica.Llm.Solicitudes[0], "Bases de Datos");
        Assert.NotNull(bloque);
        Assert.All(PersonasAjenasA("EST-001"), persona => Assert.DoesNotContain(persona, bloque.Cuerpo, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(DefinitivasDeEst001))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-002")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_002_El_promedio_lista_cada_asignatura_con_su_definitiva_en_la_misma_linea(string asignatura, string definitiva)
    {
        var llm = GuionesLlm.SinHerramientas(s => GuionesLlm.ResumenOSinDatos(s, asignatura));
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("4.39", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(respuesta.Respuesta.Split('\n'), l => l.Contains(asignatura, StringComparison.Ordinal) && l.Contains(definitiva, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-002")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_002_El_promedio_cita_las_secciones_3_y_4_declara_uso_de_datos_y_no_trae_terceros()
    {
        var llm = GuionesLlm.SinHerramientas(s => GuionesLlm.ResumenOSinDatos(s, "Cálculo Integral"));
        using var fabrica = new FabricaApi(llm: llm);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Contains("evaluacion_y_calificaciones.md · Sección 3", respuesta.Citas);
        Assert.Contains("evaluacion_y_calificaciones.md · Sección 4", respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.All(PersonasAjenasA("EST-001"), persona => Assert.DoesNotContain(persona, respuesta.Respuesta, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(SinActivador))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-155")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_155_Sin_activador_no_se_entrega_resumen_ni_se_leen_notas_o_asistencia(string rol, string id, string pregunta)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Respuesta de prueba."));

        var respuesta = await fabrica.PreguntarAsync(rol, id, pregunta);

        Assert.Equal(200, respuesta.Estado);
        var enviado = TextoEnviado(Assert.Single(fabrica.Llm.Solicitudes));
        Assert.All(CodigosDeAsignatura, codigo => Assert.DoesNotContain(codigo, enviado, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Api.Llamadas, l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal) || l.StartsWith("ObtenerAsistencia", StringComparison.Ordinal));
        Assert.False(respuesta.UsoDatosEstudiante);
    }
}
