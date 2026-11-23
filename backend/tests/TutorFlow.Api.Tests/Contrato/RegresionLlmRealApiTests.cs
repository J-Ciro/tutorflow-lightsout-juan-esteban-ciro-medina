using System.Text.RegularExpressions;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class RegresionLlmRealApiTests
{
    private const string CitaArticulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string CitaBeca = "bienestar_y_becas.md · Sección 3.1";
    private const string PreguntaNotaMinima = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaContabilidad = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaBeca = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";

    private const string TextoNotaMinima = "Se aprueba una asignatura con una nota definitiva de 3.0 o más, en una escala de 0.0 a 5.0 (reglamento_academico_2025.md · Artículo 5).";
    private const string TextoContabilidad = "Contabilidad: 3.9. Se aprueba con una definitiva de 3.0 o más (reglamento_academico_2025.md · Artículo 5).";

    private const string TextoMateoPerdida = """
        Valentina Castro (ID EST-005) y Mateo Vargas (ID EST-006) tienen asignaturas perdidas. Valentina Castro perdió Microeconomía (1.8) y Estadística (2.3) por inasistencia, mientras que Mateo Vargas perdió Estadística (sin definitiva) por inasistencia.

        Valentina Castro: Asignaturas perdidas: Microeconomía (1.8), Estadística (2.3).
        Mateo Vargas: Asignatura perdida: Estadística.

        Estas perdidas se respaldan en el Artículo 11 del reglamento académico.
        """;

    private const string TextoPerdidasCorrecto = "Valentina Castro (ID EST-005) tiene asignaturas perdidas: Microeconomía con 1.8 y Estadística con 2.3. Mateo Vargas (ID EST-006) tiene una asignatura sin definitiva: Estadística. Camilo Rojas (ID EST-004) no tiene ninguna asignatura perdida.";

    private static readonly Regex MateoConPerdida = new(@"Mateo Vargas[^\n]*perdi", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static TheoryData<string> RequisitosDeBecaCitandoLaSeccion => new()
    {
        "Para obtener la beca por rendimiento académico, se deben cumplir los siguientes requisitos:\n\n- Promedio ponderado del periodo igual o superior a 4.3.\n- Ninguna asignatura perdida, incluidas las perdidas por inasistencia.\n- Matrícula de al menos 12 créditos.\n\nEstos requisitos se respaldan en bienestar_y_becas.md · Sección 3.1.",
        "Se aprueba con un promedio ponderado del periodo igual o superior a 4.3, sin asignaturas perdidas, y matriculando al menos 12 créditos (bienestar_y_becas.md · Sección 3.1).",
        "Para obtener la beca por rendimiento académico, debes cumplir los siguientes requisitos:\n\n- Promedio ponderado del periodo ≥ 4.3\n- Ninguna asignatura perdida, incluyendo las perdidas por inasistencia\n- Matrícula de al menos 12 créditos\n\nEstos requisitos se respaldan en bienestar_y_becas.md · Sección 3.1."
    };

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-001")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_001_La_respuesta_del_modelo_real_con_el_texto_de_la_regla_no_se_bloquea_como_fuga()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => TextoNotaMinima), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNotaMinima);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("3.0", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(CitaArticulo5, respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-005")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_005_La_definitiva_con_la_regla_en_la_misma_respuesta_no_se_bloquea_como_fuga()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => TextoContabilidad), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaContabilidad);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("3.9", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(RequisitosDeBecaCitandoLaSeccion))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-008")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_008_Los_requisitos_de_la_beca_con_4_3_y_la_cita_de_la_seccion_3_1_no_dan_texto_no_verificable(string textoDelModelo)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => textoDelModelo), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaBeca);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("4.3", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(CitaBeca, respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-006")]
    [Trait("RF", "RN-02")]
    public async Task ESC_006_Una_asignatura_sin_definitiva_no_se_presenta_como_perdida_de_un_estudiante_del_programa()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => TextoMateoPerdida), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Assert.Equal(200, respuesta.Estado);
        Assert.DoesNotMatch(MateoConPerdida, respuesta.Respuesta);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-006")]
    [Trait("RF", "RN-02")]
    public async Task ESC_006_La_respuesta_que_distingue_perdida_de_sin_definitiva_se_entrega_con_el_listado_de_valentina()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => TextoPerdidasCorrecto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("Valentina Castro", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("1.8", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("2.3", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains(CitaArticulo5, respuesta.Citas);
        Assert.Contains("Datos del programa · ADM", respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
