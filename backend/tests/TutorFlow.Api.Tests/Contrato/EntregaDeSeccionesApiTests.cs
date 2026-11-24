using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class EntregaDeSeccionesApiTests
{
    private const string Seccion2 = "evaluacion_y_calificaciones.md · Sección 2";
    private const string Seccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Seccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Beca31 = "bienestar_y_becas.md · Sección 3.1";
    private const string Bienestar1 = "bienestar_y_becas.md · Sección 1";
    private const string Articulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string Articulo7 = "reglamento_academico_2025.md · Artículo 7";
    private const string Articulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string Articulo16 = "reglamento_academico_2025.md · Artículo 16";
    private const string Articulo20 = "reglamento_academico_2025.md · Artículo 20";
    private const string Articulo21 = "reglamento_academico_2025.md · Artículo 21";
    private const string Articulo22 = "reglamento_academico_2025.md · Artículo 22";
    private const string PuntajeMinimo = "Conocimiento:PuntajeRelativoMinimo";
    private const string PuntajeRelevante = "Conocimiento:PuntajeRelativoRelevante";
    private const string PreguntaP1 = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaP4 = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string RespuestaSinCitas = "Revisa el detalle.";

    public static TheoryData<string, string, string, string, string[]> ConjuntosDeESC198a => new()
    {
        { "Q10 #1", "estudiante", "EST-001", PreguntaP1, [Articulo5, Seccion3] },
        { "Q10 #2", "estudiante", "EST-001", "¿Cuál es mi promedio ponderado del periodo?", [Seccion4] },
        { "Q10 #3", "estudiante", "EST-001", "¿Cuántas faltas tengo en Bases de Datos?", [Articulo22] },
        { "Q10 #4", "estudiante", "EST-001", PreguntaP4, [Seccion3, Seccion2, Articulo5] },
        { "Q10 #5", "estudiante", "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", [Seccion3, Seccion2, Articulo5] },
        { "Q10 #6", "coordinador", "COORD-02", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?", [Articulo11, Articulo7, Articulo21] },
        { "Q10 #7", "estudiante", "EST-001", "¿Cuál es el horario de atención de Bienestar?", [Bienestar1] },
        { "Q10 #8", "estudiante", "EST-004", "¿Qué requisitos tiene la beca por rendimiento académico?", [Beca31] },
        { "cuántos estudiantes", "coordinador", "COORD-01", "¿Cuántos estudiantes tiene mi programa?", [Articulo22, Articulo11] },
        { "promedio de Valentina", "coordinador", "COORD-02", "¿Cuál es el promedio de Valentina Castro?", [Seccion4, Seccion3, Articulo16] },
        { "prueba académica coordinador", "coordinador", "COORD-02", "¿Qué estudiantes están en prueba académica?", [Articulo20] },
        { "prueba académica propia", "estudiante", "EST-005", "¿Estoy en prueba académica?", [Articulo20] }
    };

    public static TheoryData<string, string, string, string, string, string[]> UmbralesConfigurados => new()
    {
        { "Q10 #4 con el mínimo en 60 y sin regla de relevantes", "60", "100", "EST-001", PreguntaP4, [Seccion3, Seccion2] },
        { "Q10 #4 con el mínimo en 51 (Artículo 5 a 51.1%)", "51", "100", "EST-001", PreguntaP4, [Seccion3, Seccion2, Articulo5] },
        { "Q10 #4 con el mínimo en 52 (Artículo 5 a 51.1%)", "52", "100", "EST-001", PreguntaP4, [Seccion3, Seccion2] },
        { "Q10 #1 con el mínimo en 50 y sin regla de relevantes", "50", "100", "EST-001", PreguntaP1, [Articulo5] },
        { "Q10 #1 con el mínimo en 49 (Artículo 16 a 49.6%)", "49", "100", "EST-001", PreguntaP1, [Articulo5, Articulo16] },
        { "Q10 #1 con el relevante en 46 (Sección 3 a 46.5% con 2 raíces)", "100", "46", "EST-001", PreguntaP1, [Articulo5, Seccion3] },
        { "Q10 #1 con el relevante en 47 (Sección 3 a 46.5% con 2 raíces)", "100", "47", "EST-001", PreguntaP1, [Articulo5] }
    };

    private static IReadOnlyList<string> Documentos(FabricaApi fabrica) =>
        [.. GuionesLlm.Bloques(Assert.Single(fabrica.Llm.Solicitudes))
            .Select(b => b.Fuente)
            .Where(f => f.Contains(".md · ", StringComparison.Ordinal))];

    [Theory]
    [MemberData(nameof(ConjuntosDeESC198a))]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-54")]
    public async Task ESC_198a_Con_el_indice_real_se_entregan_exactamente_las_secciones_de_la_tabla_en_el_orden_del_ranking(string caso, string rol, string id, string pregunta, string[] esperadas)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => RespuestaSinCitas), conocimientoReal: true);

        await fabrica.PreguntarAsync(rol, id, pregunta);

        Assert.Equal(esperadas, Documentos(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("ESC", "ESC-009")]
    [Trait("D", "D-54")]
    public async Task ESC_198a_Q10_9_no_tiene_ninguna_seccion_relevante_y_responde_COMP_03_sin_llamar_al_LLM()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(new InvalidOperationException("llm")), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Qué dice el reglamento sobre el uso de parqueaderos?");

        Afirmaciones.SinInformacionSinLlm(respuesta, fabrica);
    }

    [Theory]
    [MemberData(nameof(UmbralesConfigurados))]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-54")]
    public async Task ESC_198a_Los_umbrales_de_puntaje_relativo_se_leen_de_la_configuracion_y_deciden_las_secciones_entregadas(string caso, string minimo, string relevante, string id, string pregunta, string[] esperadas)
    {
        Assert.NotEmpty(caso);
        var configuracion = new Dictionary<string, string?> { [PuntajeMinimo] = minimo, [PuntajeRelevante] = relevante };
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => RespuestaSinCitas), conocimientoReal: true, configuracion: configuracion);

        await fabrica.PreguntarEstudianteAsync(id, pregunta);

        Assert.Equal(esperadas, Documentos(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-54")]
    public async Task ESC_198a_La_primera_seccion_relevante_se_entrega_aunque_ningun_umbral_la_deje_pasar()
    {
        var configuracion = new Dictionary<string, string?> { [PuntajeMinimo] = "100", [PuntajeRelevante] = "100" };
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => RespuestaSinCitas), conocimientoReal: true, configuracion: configuracion);

        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaP4);

        Assert.Equal([Seccion3], Documentos(fabrica));
    }
}
