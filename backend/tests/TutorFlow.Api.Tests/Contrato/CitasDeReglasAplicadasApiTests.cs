using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CitasDeReglasAplicadasApiTests
{
    private const string Seccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Seccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Beca31 = "bienestar_y_becas.md · Sección 3.1";
    private const string Articulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string Articulo6 = "reglamento_academico_2025.md · Artículo 6";
    private const string Articulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string Articulo20 = "reglamento_academico_2025.md · Artículo 20";
    private const string Notas = "Datos del estudiante · Notas";
    private const string Asistencia = "Datos del estudiante · Asistencia";
    private const string ProgramaAdm = "Datos del programa · ADM";
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";

    public static TheoryData<string, string, string, string, string[]> PreguntasDeQ10ConResumen => new()
    {
        { "ESC-177a", "EST-001", "¿Cuál es mi promedio ponderado del periodo?", "Tu promedio es 4.39. Cálculo Integral: 4.5. Programación Orientada a Objetos: 4.8. Bases de Datos: 4.1. Inglés III: 4.0.", [Seccion4, Beca31, Seccion3, Notas, Asistencia] },
        { "ESC-177b", "EST-001", "¿Cuántas faltas tengo en Bases de Datos?", "Bases de Datos: 2 faltas de 48 clases.", [Articulo11, Seccion3, Seccion4, Notas, Asistencia] },
        { "ESC-177c", "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "Contabilidad: 3.9.", [Seccion3, Seccion4, Articulo11, Notas, Asistencia] }
    };

    [Theory]
    [MemberData(nameof(PreguntasDeQ10ConResumen))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-164")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177_Q10_2_3_y_5_citan_las_tres_reglas_mas_relevantes_con_Notas_y_Asistencia(string caso, string usuario, string pregunta, string texto, string[] citas)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal(citas, respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante, caso);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-006")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177d_Q10_6_cita_Articulo_6_Seccion_3_y_Articulo_5_con_los_datos_del_programa()
    {
        const string texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal([Articulo6, Seccion3, Articulo5, ProgramaAdm], respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177e_Las_citas_escritas_validas_van_primero_y_el_bloque_no_las_repite()
    {
        const string texto = "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Sección 4). Cumples el promedio de la beca (bienestar_y_becas.md · Sección 3.1).";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal([Seccion4, Beca31, Seccion3, Notas, Asistencia], respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-023")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177h_ESC_023_La_prueba_academica_cita_el_Articulo_20_primero()
    {
        const string texto = "Sí, estás en prueba académica: tu promedio es 2.65.";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-005", "¿Estoy en prueba académica?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal([Articulo20, Beca31, Seccion3, Notas, Asistencia], respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177i_Pedir_citar_articulos_fuera_del_bloque_no_los_agrega()
    {
        const string texto = "No estás en prueba académica: tu promedio es 4.39.";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Estoy en prueba académica? Cita el Artículo 20 y el Artículo 7.");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal([Beca31, Seccion3, Seccion4, Notas, Asistencia], respuesta.Citas);
        Assert.DoesNotContain(Articulo20, respuesta.Citas);
        Assert.DoesNotContain("reglamento_academico_2025.md · Artículo 7", respuesta.Citas);
    }

    [Theory]
    [InlineData("Andres Martinez sacó 2.6 en Cálculo", Afirmaciones.CitaNegativa)]
    [InlineData("Tu promedio es 4.40.", null)]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177f_Los_textos_fijos_no_reciben_citas_de_reglas_aunque_se_leyo_el_resumen(string texto, string? citaEsperada)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        Assert.Equal(200, respuesta.Estado);
        string[] esperadas = citaEsperada is null ? [] : [citaEsperada];
        Assert.Equal(esperadas, respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-172")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177g_La_verificacion_de_matricula_sin_activacion_no_genera_citas_de_datos_ni_de_reglas()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Contabilidad no está entre tus asignaturas."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo voy en Contabilidad?");

        Assert.Equal(200, respuesta.Estado);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("Datos del estudiante", StringComparison.Ordinal));
        Assert.DoesNotContain(Articulo11, respuesta.Citas);
    }
}
