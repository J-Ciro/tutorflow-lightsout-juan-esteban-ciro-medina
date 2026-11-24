using TutorFlow.Pruebas.Soporte;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class TimeoutTotalApiTests
{
    private static readonly TimeSpan DemoraPorLlamada = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan TimeoutLlm = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan DemoraDeLaApi = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CotaDe158a = TimeSpan.FromSeconds(21);
    private static readonly TimeSpan CotaDe158b = TimeSpan.FromSeconds(27);
    private const int LlamadasConHerramienta = 4;
    private const int MaximoDeLlamadas = 4;
    private const string Pregunta = "¿Qué dice el reglamento sobre la habilitación?";

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-158")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_158a_El_timeout_desde_el_inicio_de_la_fase_del_llm_cubre_todas_las_llamadas_y_no_se_reinicia_por_llamada()
    {
        var reloj = new RelojControlado();
        var llm = new ClienteLlmLento(reloj, DemoraPorLlamada, LlamadasConHerramienta);
        using var fabrica = new FabricaApi(clienteLlm: llm, reloj: reloj);
        _ = fabrica.Services;

        var respuesta = await reloj.AvanzarHastaQueTermine(fabrica.PreguntarEstudianteAsync("EST-001", Pregunta));

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.True(reloj.Transcurrido < CotaDe158a, $"transcurrió {reloj.Transcurrido}");
        Assert.True(llm.Llamadas <= MaximoDeLlamadas, $"llamadas: {llm.Llamadas}");
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-158")]
    [Trait("RF", "RNF-02")]
    [Trait("D", "D-71")]
    public async Task ESC_158b_La_api_lenta_y_el_llm_que_nunca_responde_dan_503_con_el_total_de_ambas_fases_sin_acumular_mas()
    {
        var reloj = new RelojControlado();
        var api = new ApiAcademicaDoble { Interceptor = (metodo, ct) => metodo == "ObtenerEstudiante" ? reloj.Pausar(DemoraDeLaApi, ct) : Task.CompletedTask };
        using var fabrica = new FabricaApi(api: api, clienteLlm: new ClienteLlmLento(reloj, TimeSpan.FromMinutes(5), 0), reloj: reloj);
        _ = fabrica.Services;

        var respuesta = await reloj.AvanzarHastaQueTermine(fabrica.PreguntarEstudianteAsync("EST-001", Pregunta));

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.True(reloj.Transcurrido >= DemoraDeLaApi + TimeoutLlm, $"transcurrió {reloj.Transcurrido}");
        Assert.True(reloj.Transcurrido < CotaDe158b, $"transcurrió {reloj.Transcurrido}");
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}

public sealed class PresupuestoConcurrenteApiTests
{
    private const string PresupuestoDiario = "0.010";
    private const string ReservaPorPeticion = "0.004";
    private const decimal CostoPorPeticionUsd = 0.003m;
    private const decimal TokensPorMillon = 1_000_000m;
    private const int PrecisionDelPrecio = 6;
    private const int TokensDeSalida = 0;
    private const int PeticionesSimultaneas = 5;
    private const int Aceptadas = 2;
    private const string Pregunta = "¿Cuál es mi promedio?";
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);

    private static Dictionary<string, string?> Configuracion(string precioEntrada) => new()
    {
        ["Limites:PresupuestoDiarioUsd"] = PresupuestoDiario,
        ["Llm:ReservaPorPeticionUsd"] = ReservaPorPeticion,
        ["Llm:CatalogoPrecios:Url"] = "",
        ["Llm:Proveedores:anthropic:Modelos:0:PrecioEntradaPorMillon"] = precioEntrada,
        ["Llm:Proveedores:anthropic:Modelos:0:PrecioSalidaPorMillon"] = "0"
    };

    private static async Task<string> PrecioParaUnCostoPorPeticionAsync()
    {
        var sonda = ClienteLlmDoble.Texto("Respuesta de prueba.", new UsoTokens(0, TokensDeSalida));
        using var fabricaSonda = new FabricaApi(llm: sonda);
        await fabricaSonda.PreguntarEstudianteAsync("EST-001", Pregunta);
        var tokensDeEntrada = ConsumoRealista.Reportados(sonda.Solicitudes[0]);
        return Math.Round(CostoPorPeticionUsd * TokensPorMillon / tokensDeEntrada, PrecisionDelPrecio)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-159")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_159_Con_cinco_peticiones_simultaneas_exactamente_dos_responden_200_y_tres_503_sin_llamar_al_llm()
    {
        var llm = new ClienteLlmRetenido(new UsoTokens(0, TokensDeSalida));
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: Configuracion(await PrecioParaUnCostoPorPeticionAsync()), variablesEntorno: ConfiguracionDePrueba.EntornoConLlave());
        _ = fabrica.Services;
        var peticiones = Enumerable.Range(0, PeticionesSimultaneas)
            .Select(_ => Task.Run(() => fabrica.PreguntarEstudianteAsync("EST-001", Pregunta)))
            .ToList();

        var pendientes = peticiones.ToList();
        try
        {
            for (var rechazadas = 0; rechazadas < PeticionesSimultaneas - Aceptadas; rechazadas++)
            {
                var lista = await Task.WhenAny(pendientes).WaitAsync(EsperaMaxima);
                pendientes.Remove(lista);
            }
        }
        finally
        {
            llm.Liberar();
        }

        var respuestas = await Task.WhenAll(peticiones);

        Assert.Equal(Aceptadas, respuestas.Count(r => r.Estado == 200));
        Assert.Equal(PeticionesSimultaneas - Aceptadas, respuestas.Count(r => r.Estado == 503));
        Assert.Equal(Aceptadas, llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-159")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_159_El_gasto_del_dia_es_el_costo_real_y_no_la_reserva()
    {
        var llm = new ClienteLlmRetenido(new UsoTokens(0, TokensDeSalida));
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: Configuracion(await PrecioParaUnCostoPorPeticionAsync()), variablesEntorno: ConfiguracionDePrueba.EntornoConLlave());
        llm.Liberar();

        var estados = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            estados.Add((await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta)).Estado);
        }

        Assert.Equal([200, 200, 200, 503], estados);
    }
}
