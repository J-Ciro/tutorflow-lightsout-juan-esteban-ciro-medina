using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ReintentoApiTests
{
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";
    private const string PreguntaQ10Seis = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PerdidaDeMateo = "Mateo Vargas perdió Estadística.";
    private const string PerdidasDeValentina = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
    private const int SalidaPorLlamada = 20;
    private const decimal ToleranciaDeCosto = 0.000001m;

    public static TheoryData<string, string> BloqueosSinReintento => new()
    {
        { "SEG-10b-nombre-ajeno", "Andres Martinez sacó 2.6 en Cálculo" },
        { "SEG-10b-id-inventado", "EST 099 tiene un promedio de 4.39." }
    };

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_193b_El_reintento_se_entrega_con_los_tokens_y_el_costo_de_los_dos_turnos_y_reintento_true_en_la_auditoria()
    {
        var llm = GuionesPorTurno.Texto("Se aprueba con 4.40.", "Se aprueba con 3.0.");
        using var fabrica = new FabricaApi(llm: llm, configuracion: FabricaApi.Precios("0.10", "0.50"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal("Se aprueba con 3.0.", respuesta.Respuesta);
        Assert.Equal(2, fabrica.Llm.Llamadas);
        var entrada = fabrica.Llm.Solicitudes.Sum(ConsumoRealista.Reportados);
        var salida = fabrica.Llm.Llamadas * SalidaPorLlamada;
        Assert.Equal(entrada + salida, respuesta.Tokens);
        Assert.InRange(respuesta.CostoUsd, ConsumoRealista.CostoDe(entrada, salida, 0.10m, 0.50m) - ToleranciaDeCosto, ConsumoRealista.CostoDe(entrada, salida, 0.10m, 0.50m) + ToleranciaDeCosto);
        Assert.False(respuesta.UsoDatosEstudiante);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Respondida, evento.Resultado);
        Assert.Equal(entrada + salida, evento.Tokens);
        AfirmacionesDeReintento.AfirmarReintentoRegistrado(evento);
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_193a_El_coordinador_recibe_el_texto_del_reintento_con_un_solo_segundo_turno()
    {
        var llm = GuionesPorTurno.LeeElProgramaYResponde(PerdidaDeMateo, PerdidasDeValentina);
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", "¿Qué dice el reglamento sobre la habilitación?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(PerdidasDeValentina, respuesta.Respuesta);
        Assert.Contains("Datos del programa · ADM", respuesta.Citas);
        Assert.Equal(1, GuionesPorTurno.Reintentos(fabrica.Llm));
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Respondida, evento.Resultado);
        AfirmacionesDeReintento.AfirmarReintentoRegistrado(evento);
    }

    [Theory]
    [MemberData(nameof(BloqueosSinReintento))]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10b")]
    public async Task ESC_193f_Un_bloqueo_de_SEG_10b_hace_una_sola_llamada_y_no_lleva_reintento(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPromedio);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NegativaEstudiante, respuesta.Respuesta);
        Assert.Equal(1, fabrica.Llm.Llamadas);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, evento.Resultado);
        Assert.False(AfirmacionesDeReintento.ReintentoRegistrado(evento));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-06")]
    public async Task ESC_193f_El_403_tardio_no_se_reintenta()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.PideEstudiante("EST-004"), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", "¿Qué estudiantes de mi programa perdieron materias?");

        Assert.Equal(403, respuesta.Estado);
        Assert.Equal(1, fabrica.Llm.Llamadas);
        Assert.False(AfirmacionesDeReintento.ReintentoRegistrado(Assert.Single(fabrica.Auditoria.Eventos)));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("D", "D-62")]
    [Trait("RF", "SEG-13")]
    public async Task ESC_193h_Con_un_timeout_de_1_s_el_reintento_de_500_ms_se_cancela_al_llegar_a_1_s_y_se_responde_el_primer_bloqueo_en_menos_de_2_s_sin_503()
    {
        var reloj = new RelojControlado();
        var llm = new ClienteLlmConDemoraPorTurno(reloj, TimeSpan.FromMilliseconds(750), TimeSpan.FromMilliseconds(500), "Se aprueba con 4.40.", "Se aprueba con 3.0.");
        var configuracion = new Dictionary<string, string?> { ["Limites:TimeoutLlmSegundos"] = "1" };
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: configuracion, conocimientoReal: true, reloj: reloj);
        _ = fabrica.Services;

        var respuesta = await reloj.AvanzarHastaQueTermine(fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?"));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Equal(2, llm.Llamadas);
        Assert.Equal(TimeSpan.FromSeconds(1), reloj.Transcurrido);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, evento.Resultado);
        AfirmacionesDeReintento.AfirmarReintentoRegistrado(evento);
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-13")]
    public async Task ESC_193i_Sin_presupuesto_para_una_segunda_reserva_no_hay_reintento()
    {
        var configuracion = new Dictionary<string, string?> { ["Limites:PresupuestoDiarioUsd"] = "0.004", ["Llm:ReservaPorPeticionUsd"] = "0.003" };
        using var fabrica = new FabricaApi(llm: GuionesPorTurno.Texto("Se aprueba con 4.40.", "Se aprueba con 3.0."), configuracion: configuracion, variablesEntorno: EntornoSinPrecio(), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Equal(1, fabrica.Llm.Llamadas);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, evento.Resultado);
        Assert.False(AfirmacionesDeReintento.ReintentoRegistrado(evento));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-13")]
    public async Task ESC_193i_Con_presupuesto_para_la_segunda_reserva_el_reintento_ocurre()
    {
        var configuracion = new Dictionary<string, string?> { ["Limites:PresupuestoDiarioUsd"] = "0.01", ["Llm:ReservaPorPeticionUsd"] = "0.003" };
        using var fabrica = new FabricaApi(llm: GuionesPorTurno.Texto("Se aprueba con 4.40.", "Se aprueba con 3.0."), configuracion: configuracion, variablesEntorno: EntornoSinPrecio(), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("Se aprueba con 3.0.", respuesta.Respuesta);
        Assert.Equal(2, fabrica.Llm.Llamadas);
    }
}
