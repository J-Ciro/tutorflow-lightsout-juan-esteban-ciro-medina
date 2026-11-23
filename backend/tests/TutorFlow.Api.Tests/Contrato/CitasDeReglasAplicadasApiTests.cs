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

    public static TheoryData<string, string, string, string> PreguntasDeQ10ConResumen => new()
    {
        { "ESC-177a", "EST-001", "¿Cuál es mi promedio ponderado del periodo?", "Tu promedio es 4.39. Cálculo Integral: 4.5. Programación Orientada a Objetos: 4.8. Bases de Datos: 4.1. Inglés III: 4.0." },
        { "ESC-177b", "EST-001", "¿Cuántas faltas tengo en Bases de Datos?", "Bases de Datos: 2 faltas de 48 clases." },
        { "ESC-177c", "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "Contabilidad: 3.9." }
    };

    [Theory]
    [MemberData(nameof(PreguntasDeQ10ConResumen))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-164")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177_Q10_2_3_y_5_citan_Seccion_3_Seccion_4_y_Articulo_11_con_Notas_y_Asistencia(string caso, string usuario, string pregunta, string texto)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal([Seccion3, Seccion4, Articulo11, Notas, Asistencia], respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante, caso);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-006")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_177d_Q10_6_cita_Articulo_5_6_y_20_con_los_datos_del_programa()
    {
        const string texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Equal([Articulo5, Articulo6, Articulo20, ProgramaAdm], respuesta.Citas);
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
        Assert.Equal([Seccion4, Beca31, Seccion3, Articulo11, Notas, Asistencia], respuesta.Citas);
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
