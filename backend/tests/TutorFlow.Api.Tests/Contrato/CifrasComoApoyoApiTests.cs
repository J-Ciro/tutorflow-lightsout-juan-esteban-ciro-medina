using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CifrasComoApoyoApiTests
{
    private const string PreguntaDefinitiva = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string RespuestaPesos = "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal.";
    private const string CitaSeccion2 = "evaluacion_y_calificaciones.md · Sección 2";
    private const string CitaSeccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string CitasEscritas = $"({CitaSeccion2}) ({CitaSeccion3})";
    private static readonly string[] RespaldoPermitido = ["reglamento_academico_2025.md · Artículo 5", "reglamento_academico_2025.md · Artículo 6"];

    public static TheoryData<string, string[], string> RespuestasConApoyoNumerico => new()
    {
        { "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal", [CitaSeccion2, CitaSeccion3], "ESC-171a" },
        { "El corte 1 pesa 30%", [CitaSeccion2], "ESC-171b" },
        { "La definitiva se redondea a una cifra decimal", [CitaSeccion3], "ESC-171c" }
    };

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-004")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_004_La_recuperacion_entrega_la_Seccion_2_y_la_Seccion_3_al_modelo()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => RespuestaPesos), conocimientoReal: true);

        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaDefinitiva);

        var entregadas = GuionesLlm.Bloques(Assert.Single(fabrica.Llm.Solicitudes)).Select(b => b.Fuente).ToList();
        Assert.Contains(CitaSeccion2, entregadas);
        Assert.Contains(CitaSeccion3, entregadas);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-004")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_004_La_respuesta_de_los_pesos_conserva_las_citas_de_la_Seccion_2_y_la_Seccion_3()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => $"{RespuestaPesos} {CitasEscritas}"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaDefinitiva);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal($"{RespuestaPesos} {CitasEscritas}", respuesta.Respuesta);
        Assert.Contains(CitaSeccion2, respuesta.Citas);
        Assert.Contains(CitaSeccion3, respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(RespuestasConApoyoNumerico))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-171")]
    [Trait("RF", "COMP-02")]
    [Trait("D", "D-37")]
    public async Task ESC_171_La_cita_escrita_con_apoyo_se_conserva_y_la_escrita_sin_apoyo_se_elimina_del_texto(string cuerpo, string[] citasEsperadas, string caso)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => $"{cuerpo} {CitasEscritas}."), conocimientoReal: true);
        var conservadas = string.Join(" ", citasEsperadas.Select(c => $"({c})"));

        var resultado = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaDefinitiva);

        Assert.True(resultado.Estado == 200, caso);
        Assert.Equal($"{cuerpo} {conservadas}.", resultado.Respuesta);
        Assert.Equal(citasEsperadas.Order(StringComparer.Ordinal), resultado.Citas.Order(StringComparer.Ordinal));
        Assert.False(resultado.UsoDatosEstudiante, caso);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-171")]
    [Trait("RF", "SEG-10e")]
    public async Task ESC_171d_Un_entero_sin_porcentaje_no_da_apoyo_y_la_respuesta_se_reemplaza_por_COMP_03()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => $"El corte 1 pesa 30 puntos. {CitasEscritas}"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaDefinitiva);

        Afirmaciones.SinInformacionTrasLlm(respuesta, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-171")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_171e_Un_porcentaje_inventado_responde_el_texto_no_verificable_y_no_cuenta_como_apoyo()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => $"El corte 1 pesa 35%. {CitasEscritas}"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaDefinitiva);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-166")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_166c_El_30_por_ciento_de_la_beca_no_cita_la_Seccion_2_de_evaluacion_que_solo_comparte_la_cifra()
    {
        const string texto = "La beca de apoyo socioeconómico da un descuento de hasta el 30%.";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuánto cuesta la matrícula con la beca socioeconómica?");

        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains("bienestar_y_becas.md · Sección 3.2", respuesta.Citas);
        Assert.DoesNotContain(CitaSeccion2, respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-164")]
    [Trait("RF", "COMP-02")]
    public async Task ESC_164a_El_respaldo_de_la_nota_minima_son_las_entregadas_de_Articulo_5_y_6_en_orden_de_ranking()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Una asignatura se aprueba con 3.0."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        var entregadas = GuionesLlm.Bloques(Assert.Single(fabrica.Llm.Solicitudes)).Select(b => b.Fuente).ToList();
        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("reglamento_academico_2025.md · Artículo 5", entregadas);
        Assert.Equal(entregadas.Where(RespaldoPermitido.Contains), respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
    }
}
