using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class TextoFinalYBloquesApiTests
{
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";
    private const string PreguntaPerdieron = "¿Qué estudiantes de mi programa perdieron materias?";
    private const string PreguntaQ10Seis = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaDefinitivaEstadistica = "¿Cuál es mi nota definitiva en Estadística?";
    private const string PreguntaAprobarEstadistica = "¿Qué necesito para aprobar Estadística?";
    private const string PreguntaHabilitar = "¿Puedo habilitar Microeconomía?";
    private const string PreguntaDefinitivasEst004 = "¿Cuáles son mis notas definitivas?";
    private const string CitaProgramaAdm = "Datos del programa · ADM";
    private const string CitaNotas = "Datos del estudiante · Notas";

    public static TheoryData<string, string> PerdidasQueDebenFallar => new()
    {
        { "a", "| Estudiante | Asignaturas perdidas |\n| --- | --- |\n| Mateo Vargas | Estadística |" },
        { "b", "Asignaturas perdidas por estudiante:\n- Mateo Vargas: Estadística" },
        { "c", "Mateo Vargas:\n\n- Asignatura perdida: Estadística" },
        { "d", "Mateo Vargas:\n> - Asignatura perdida: Estadística" },
        { "e", "**Mateo Vargas**\nAsignatura perdida: Estadística" },
        { "f", "Mateo Vargas, sin duda, perdió Estadística." },
        { "g", "Valentina Castro y Mateo Vargas:\n- Asignatura perdida: Estadística" }
    };

    public static TheoryData<string, string> PerdidasQueSeEntregan => new()
    {
        { "h", "Estudiantes con asignaturas perdidas:\n- Valentina Castro: Microeconomía (1.8) y Estadística (2.3)" },
        { "i", "| Estudiante | Asignaturas perdidas |\n| --- | --- |\n| Valentina Castro | Microeconomía (1.8), Estadística (2.3) |" },
        { "j", "Mateo Vargas no ha perdido Estadística." },
        { "k", "Mateo Vargas:\n- Sin definitiva: Estadística" },
        { "l", "Estudiantes con asignaturas perdidas:\n- Valentina Castro: Microeconomía (1.8)\n\nMateo Vargas tiene Estadística sin definitiva." }
    };

    public static TheoryData<string, string> DefinitivasQueDebenFallar => new()
    {
        { "a", "Sin duda, tu definitiva en Estadística es 4.0." },
        { "b", "Aun así, tu definitiva en Estadística es 4.0." },
        { "c", "No dudes: tu definitiva en Estadística es 4.0." },
        { "d", "Tu definitiva en Estadística, superior a 3.0, es 4.0." },
        { "e", "Tu definitiva en Estadística (mínimo 3.0 para aprobar) es 4.0." },
        { "f", "Necesita revisión: definitiva de Estadística 4.0." }
    };

    public static TheoryData<string, string, string, string> DefinitivasQueSeEntregan => new()
    {
        { "g", "EST-006", PreguntaDefinitivaEstadistica, "Estadística no tiene nota definitiva: llevas 2.4 sobre el 60% evaluado." },
        { "h", "EST-005", PreguntaAprobarEstadistica, "Estadística se aprueba con definitiva ≥ 3.0 y la tuya es 2.3." },
        { "i", "EST-005", PreguntaHabilitar, "Microeconomía no es habilitable porque tu definitiva es menor a 2.0." },
        { "j", "EST-005", PreguntaAprobarEstadistica, "Para aprobar Estadística necesitas una definitiva de 3.0." },
        { "k", "EST-004", PreguntaDefinitivasEst004, "Tus definitivas:\n- Contabilidad: 3.9\n- Microeconomía: 3.3" }
    };

    private static void AfirmarNoVerificable(RespuestaApi respuesta, FabricaApi fabrica, ResultadoAuditoria auditoria)
    {
        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(auditoria, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    private static void AfirmarNegativaDeSalida(RespuestaApi respuesta, FabricaApi fabrica, string negativa)
    {
        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(negativa, respuesta.Respuesta);
        Assert.Equal([Afirmaciones.CitaNegativa], respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
        Assert.False(AfirmacionesDeReintento.ReintentoRegistrado(fabrica.Auditoria.Eventos[0]));
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_183a_La_cifra_partida_que_deja_la_limpieza_no_se_entrega_como_1_2()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Se aprueba con 1 x.md .2"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        AfirmarNoVerificable(respuesta, fabrica, ResultadoAuditoria.NumeroNoVerificado);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_183b_El_id_partido_por_una_referencia_borrada_recibe_la_negativa_del_estudiante_sin_reintento()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Tu promedio es 4.39, igual que EST a.md 002."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPromedio);

        AfirmarNegativaDeSalida(respuesta, fabrica, Textos.NegativaEstudiante);
        Assert.Equal(1, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_183c_El_estudiante_ajeno_con_el_id_partido_recibe_la_negativa_del_coordinador()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "Valentina b.md Castro (EST a.md 005) tiene Microeconomía perdida."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", PreguntaPerdieron);

        AfirmarNegativaDeSalida(respuesta, fabrica, Textos.NegativaCoordinador);
    }

    [Fact]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_183d_La_definitiva_que_queda_cerca_tras_la_limpieza_se_verifica_contra_la_asignatura_sin_definitiva()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Tu definitiva x1.md x2.md x3.md x4.md es 4.0 en Estadística."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-006", PreguntaDefinitivaEstadistica);

        AfirmarNoVerificable(respuesta, fabrica, ResultadosNuevosDeAuditoria.EstadoNoVerificado);
    }

    [Theory]
    [MemberData(nameof(PerdidasQueDebenFallar))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_184_Las_evasiones_reales_de_formato_con_COORD_02_fallan_cerradas(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaQ10Seis);

        AfirmarNoVerificable(respuesta, fabrica, ResultadosNuevosDeAuditoria.EstadoNoVerificado);
    }

    [Theory]
    [MemberData(nameof(PerdidasQueSeEntregan))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-184")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_184_Los_bloques_fieles_o_sin_forma_de_perdida_se_entregan_sin_cambios(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaQ10Seis);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(CitaProgramaAdm, respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(DefinitivasQueDebenFallar))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_Las_evasiones_reales_de_la_definitiva_de_EST_006_fallan_cerradas(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-006", PreguntaDefinitivaEstadistica);

        AfirmarNoVerificable(respuesta, fabrica, ResultadosNuevosDeAuditoria.EstadoNoVerificado);
    }

    [Theory]
    [MemberData(nameof(DefinitivasQueSeEntregan))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_Las_respuestas_fieles_con_negacion_umbral_o_pares_correctos_se_entregan_sin_cambios(string caso, string usuario, string pregunta, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(CitaNotas, respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185l_Los_pares_cruzados_de_la_lista_de_definitivas_de_EST_004_fallan_cerrados()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Tus definitivas:\n- Contabilidad: 3.3\n- Microeconomía: 3.9"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaDefinitivasEst004);

        AfirmarNoVerificable(respuesta, fabrica, ResultadosNuevosDeAuditoria.EstadoNoVerificado);
    }
}
