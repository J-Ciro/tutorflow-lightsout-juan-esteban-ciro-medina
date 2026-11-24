using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class AfirmacionesDePerdidaApiTests
{
    private const string PreguntaPerdidas = "¿Qué dice el reglamento sobre la habilitación?";
    private const string PreguntaHabilitacion = "¿Qué dice el reglamento sobre la habilitación?";

    public static TheoryData<string, string> PerdidasNoRespaldadas => new()
    {
        { "a", "Valentina Castro (ID EST-005) y Mateo Vargas (ID EST-006) tienen asignaturas perdidas. Valentina Castro perdió Microeconomía (1.8) y Estadística (2.3) por inasistencia, mientras que Mateo Vargas perdió Estadística (sin definitiva) por inasistencia.\n\nValentina Castro: Asignaturas perdidas: Microeconomía (1.8), Estadística (2.3).\nMateo Vargas: Asignatura perdida: Estadística.\n\nEstas perdidas se respaldan en el Artículo 11 del reglamento académico." },
        { "b", "Mateo Vargas perdió Estadística (sin definitiva) por inasistencia." },
        { "c", "Valentina Castro (ID EST-005) tiene asignaturas perdidas por inasistencia: Microeconomía con 1.8 y Estadística con 2.3." },
        { "d", "Camilo Rojas perdió Contabilidad." },
        { "e", "Valentina Castro perdió Bases de Datos." },
        { "i", "Valentina Castro y Mateo Vargas perdieron Estadística." }
    };

    public static TheoryData<string> TextosCorrectosDeCoordinador02 => new()
    {
        "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).",
        "Valentina Castro perdió Microeconomía (1.8) y Estadística (2.3).",
        "Mateo Vargas no ha perdido Estadística: le falta calificar el corte 3."
    };

    public static TheoryData<string> TextoCorregidoDeCoordinador02 => new()
    {
        "Valentina Castro (ID EST-005) tiene asignaturas perdidas: Microeconomía con 1.8 y Estadística con 2.3. Mateo Vargas (ID EST-006) tiene una asignatura sin definitiva: Estadística. Camilo Rojas (ID EST-004) no tiene ninguna asignatura perdida."
    };

    [Theory]
    [MemberData(nameof(PerdidasNoRespaldadas))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task ESC_175_Fuera_del_modo_bloque_la_oracion_con_persona_se_elimina_sin_verificar_su_perdida(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: true);
    }

    [Theory]
    [MemberData(nameof(TextosCorrectosDeCoordinador02))]
    [MemberData(nameof(TextoCorregidoDeCoordinador02))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task ESC_175f1f2_Los_textos_con_persona_de_COORD_02_se_eliminan_aunque_los_datos_los_respalden(string texto)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task ESC_175f3_La_perdida_real_de_Andres_con_COORD_01_se_elimina_por_llevar_su_nombre()
    {
        const string texto = "Andrés Martínez perdió Cálculo Integral (2.6) y Bases de Datos por inasistencia.";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", PreguntaPerdidas);

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task ESC_175g_Sin_datos_leidos_una_perdida_de_una_persona_autorizada_da_texto_no_verificable()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Valentina Castro perdió Microeconomía."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaHabilitacion);

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: false);
        Assert.Equal(1, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task ESC_175h_La_inyeccion_del_propio_estudiante_no_produce_una_perdida_ni_una_persona()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "Andrés Martínez perdió Inglés III."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-002", "¿Qué dice el reglamento sobre la habilitación?");

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task ESC_175c_La_atribucion_falsa_a_Valentina_se_elimina_con_su_nombre()
    {
        const string texto = "Valentina Castro (ID EST-005) tiene asignaturas perdidas por inasistencia: Microeconomía con 1.8 y Estadística con 2.3. Mateo Vargas (ID EST-006) tiene una asignatura sin definitiva: Estadística. Camilo Rojas (ID EST-004) no tiene ninguna asignatura perdida.";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaPerdidas);

        Afirmaciones.OracionConPersonaPodada(respuesta, fabrica, usoDatos: true);
    }
}
