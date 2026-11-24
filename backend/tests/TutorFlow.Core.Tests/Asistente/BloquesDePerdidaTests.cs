namespace TutorFlow.Core.Tests.Asistente;

public sealed class BloquesDePerdidaTests
{
    private static readonly string[] Adm = ["EST-004", "EST-005", "EST-006"];

    public static TheoryData<string, string> EvasionesQueDebenFallar => new()
    {
        { "a-tabla-con-encabezado-de-columnas", "| Estudiante | Asignaturas perdidas |\n| --- | --- |\n| Mateo Vargas | Estadística |" },
        { "b-item-con-encabezado-de-lista", "Asignaturas perdidas por estudiante:\n- Mateo Vargas: Estadística" },
        { "c-linea-vacia-no-corta-la-seccion", "Mateo Vargas:\n\n- Asignatura perdida: Estadística" },
        { "d-cita-con-mayor-que", "Mateo Vargas:\n> - Asignatura perdida: Estadística" },
        { "e-encabezado-en-negrita", "**Mateo Vargas**\nAsignatura perdida: Estadística" },
        { "f-sin-duda-no-gobierna", "Mateo Vargas, sin duda, perdió Estadística." },
        { "g-ambiguo-dos-personas-en-el-contexto", "Valentina Castro y Mateo Vargas:\n- Asignatura perdida: Estadística" },
        { "negrita-con-dos-puntos", "**Mateo Vargas:**\n- Asignatura perdida: Estadística" },
        { "negrita-con-guiones-bajos", "__Mateo Vargas__\nAsignatura perdida: Estadística" },
        { "encabezado-markdown", "## Mateo Vargas\nAsignatura perdida: Estadística" },
        { "cita-en-todas-las-lineas", "> Mateo Vargas:\n> - Asignatura perdida: Estadística" },
        { "lista-bajo-encabezado-de-tema", "Estudiantes con asignaturas perdidas:\n- Mateo Vargas: Estadística" },
        { "no-sin-negar-la-forma", "No, Mateo Vargas perdió Estadística." },
        { "no-dudes-no-gobierna", "Mateo Vargas, no dudes, perdió Estadística." },
        { "segunda-fila-de-la-tabla", "| Estudiante | Asignaturas perdidas |\n| --- | --- |\n| Valentina Castro | Microeconomía (1.8), Estadística (2.3) |\n| Mateo Vargas | Estadística |" }
    };

    public static TheoryData<string, string> BloquesFielesOSinForma => new()
    {
        { "h-lista-fiel", "Estudiantes con asignaturas perdidas:\n- Valentina Castro: Microeconomía (1.8) y Estadística (2.3)" },
        { "i-tabla-fiel", "| Estudiante | Asignaturas perdidas |\n| --- | --- |\n| Valentina Castro | Microeconomía (1.8), Estadística (2.3) |" },
        { "j-no-ha-perdido-gobierna", "Mateo Vargas no ha perdido Estadística." },
        { "k-sin-forma-de-perdida", "Mateo Vargas:\n- Sin definitiva: Estadística" },
        { "l-texto-tras-linea-vacia", "Estudiantes con asignaturas perdidas:\n- Valentina Castro: Microeconomía (1.8)\n\nMateo Vargas tiene Estadística sin definitiva." },
        { "tabla-sin-forma-de-perdida", "| Estudiante | Asignatura sin definitiva |\n| --- | --- |\n| Mateo Vargas | Estadística |" },
        { "ninguna-posterior-gobierna", "Mateo Vargas: asignaturas perdidas: ninguna. Estadística sin definitiva." },
        { "no-tiene-gobierna", "Mateo Vargas no tiene asignaturas perdidas, solo Estadística sin definitiva." }
    };

    public static TheoryData<string, string, string> EstudiantePropio => new()
    {
        { "EST-006", "Tus asignaturas perdidas:\n- Estadística", "falla" },
        { "EST-006", "Mi asignatura perdida es Estadística.", "falla" },
        { "EST-005", "Asignaturas perdidas:\n- Microeconomía (1.8)\n- Estadística (2.3)", "entrega" }
    };


    [Theory]
    [MemberData(nameof(EvasionesQueDebenFallar))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_184_Una_afirmacion_de_perdida_en_el_formato_de_un_modelo_que_los_datos_no_respaldan_falla_cerrada(string caso, string texto)
    {
        Assert.NotEmpty(caso);

        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", texto, Adm);
    }

    [Theory]
    [MemberData(nameof(BloquesFielesOSinForma))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_184_Un_bloque_fiel_negado_o_sin_forma_de_perdida_se_entrega_sin_cambios(string caso, string texto)
    {
        Assert.NotEmpty(caso);

        await ClasesVerificadoras.AfirmarPasaAsync("COORD-02", texto, Adm);
    }

    [Theory]
    [MemberData(nameof(EstudiantePropio))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_184_Para_un_estudiante_la_persona_de_todo_bloque_es_el_propio_usuario(string usuarioId, string texto, string resultadoEsperado)
    {
        if (resultadoEsperado == "falla")
        {
            await ClasesVerificadoras.AfirmarFallaAsync(usuarioId, texto, usuarioId);
            return;
        }

        await ClasesVerificadoras.AfirmarPasaAsync(usuarioId, texto, usuarioId);
    }
}
