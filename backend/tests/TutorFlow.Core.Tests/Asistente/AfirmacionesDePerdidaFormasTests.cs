namespace TutorFlow.Core.Tests.Asistente;

public sealed class AfirmacionesDePerdidaFormasTests
{
    public static TheoryData<string, string> FormasQueLosDatosNoRespaldan => new()
    {
        { "j", "Mateo Vargas (EST-006) tiene asignatura perdida por Estadística." },
        { "k", "Mateo Vargas tiene una asignatura perdida: Estadística." },
        { "l", "Mateo Vargas: asignaturas perdidas: Estadística." },
        { "m", "Mateo Vargas tiene alguna materia perdida." },
        { "n", "Mateo Vargas está con Estadística perdida." },
        { "o", "Mateo Vargas (ID EST-006) y Valentina Castro (ID EST-005) tienen asignaturas perdidas." },
        { "q", "Mateo Vargas:\n- Asignatura perdida: Estadística" },
        { "q-asterisco", "Mateo Vargas:\n* Asignatura perdida: Estadística" },
        { "q-numerada", "Mateo Vargas:\n1. Asignatura perdida: Estadística" }
    };

    public static TheoryData<string, string> FormasQueLosDatosRespaldanONoAplican => new()
    {
        { "p1-1", "Valentina Castro tiene una asignatura perdida por nota: Microeconomía (1.8)." },
        { "p1-2", "Valentina Castro está con Estadística perdida (2.3)." },
        { "p1-3", "Valentina Castro tiene asignaturas perdidas." },
        { "p1-4", "Mateo Vargas no tiene asignaturas perdidas, solo Estadística sin definitiva." },
        { "p2-1", "Mateo Vargas: asignaturas perdidas: ninguna, sin definitiva: Estadística." },
        { "p2-2", "Valentina Castro perdió Microeconomía (1.8) y no ha perdido Contabilidad." },
        { "p2-3", "Mateo Vargas podría perder Estadística si no aprueba el corte 3." },
        { "p2-4", "Valentina Castro perdió Microeconomía (1.8) y Mateo Vargas tiene Estadística sin definitiva." },
        { "r", "Valentina Castro:\n- Asignaturas perdidas: Microeconomía (1.8) y Estadística (2.3)\nMateo Vargas:\n- Sin definitiva: Estadística" }
    };

    private static readonly string[] Adm = ["EST-004", "EST-005", "EST-006"];

    [Theory]
    [MemberData(nameof(FormasQueLosDatosNoRespaldan))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_175_Una_forma_de_perdida_que_los_datos_no_respaldan_se_reemplaza_por_texto_no_verificable(string caso, string texto)
    {
        Assert.NotEmpty(caso);

        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", texto, Adm);
    }

    [Theory]
    [MemberData(nameof(FormasQueLosDatosRespaldanONoAplican))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_175p1p2_Una_forma_de_perdida_respaldada_negada_o_hipotetica_se_entrega_sin_cambios(string caso, string texto)
    {
        Assert.NotEmpty(caso);

        await ClasesVerificadoras.AfirmarPasaAsync("COORD-02", texto, Adm);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_175j_Mateo_Vargas_con_asignatura_perdida_por_Estadistica_es_la_regresion_del_fallo_real()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", "Mateo Vargas (EST-006) tiene asignatura perdida por Estadística.", Adm);
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_175m_Una_construccion_nominal_sin_asignatura_se_respalda_con_alguna_perdida_de_la_persona()
    {
        await ClasesVerificadoras.AfirmarPasaAsync("COORD-02", "Valentina Castro tiene alguna materia perdida.", Adm);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_175_La_inyeccion_del_coordinador_no_logra_una_construccion_nominal_inventada()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", "Mateo Vargas tiene asignatura perdida por Estadística.", "EST-004", "EST-005", "EST-006");
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task ESC_175_Para_un_estudiante_la_construccion_nominal_se_verifica_contra_sus_propios_datos()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("EST-001", "Laura Gómez tiene alguna asignatura perdida.", "EST-001");
    }
}
