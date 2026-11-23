using System.Diagnostics;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class TimeoutTotalApiTests
{
    private const string TimeoutTotalSegundos = "2";
    private static readonly TimeSpan DemoraPorLlamada = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan MargenSobreElTimeout = TimeSpan.FromSeconds(1);
    private const int LlamadasConHerramienta = 4;
    private const int MaximoDeLlamadas = 4;

    private static Dictionary<string, string?> ConTimeout() =>
        new() { ["Limites:TimeoutLlmSegundos"] = TimeoutTotalSegundos };

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-158")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_158_El_timeout_cubre_todas_las_llamadas_y_no_se_reinicia_por_llamada()
    {
        var llm = new ClienteLlmLento(DemoraPorLlamada, LlamadasConHerramienta);
        using var fabricaLenta = new FabricaApi(clienteLlm: llm, configuracion: ConTimeout());
        _ = fabricaLenta.Services;
        var reloj = Stopwatch.StartNew();

        var respuesta = await fabricaLenta.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        reloj.Stop();
        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(int.Parse(TimeoutTotalSegundos)) + MargenSobreElTimeout, $"tardó {reloj.Elapsed}");
        Assert.True(llm.Llamadas <= MaximoDeLlamadas, $"llamadas: {llm.Llamadas}");
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabricaLenta.Auditoria.Eventos).Resultado);
    }
}

public sealed class PresupuestoConcurrenteApiTests
{
    private const string PresupuestoDiario = "0.010";
    private const string ReservaPorPeticion = "0.004";
    private const string PrecioEntradaPorMillon = "30";
    private const int TokensDeEntrada = 100;
    private const int PeticionesSimultaneas = 5;
    private const int Aceptadas = 2;
    private const string Pregunta = "¿Cuál es mi promedio?";
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);

    private static Dictionary<string, string?> Configuracion() => new()
    {
        ["Limites:PresupuestoDiarioUsd"] = PresupuestoDiario,
        ["Llm:ReservaPorPeticionUsd"] = ReservaPorPeticion,
        ["LLM_PRECIO_ENTRADA"] = PrecioEntradaPorMillon,
        ["LLM_PRECIO_SALIDA"] = "0"
    };

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-159")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_159_Con_cinco_peticiones_simultaneas_exactamente_dos_responden_200_y_tres_503_sin_llamar_al_llm()
    {
        var llm = new ClienteLlmRetenido(new UsoTokens(TokensDeEntrada, 0));
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: Configuracion());
        _ = fabrica.Services;
        var peticiones =Enumerable.Range(0, PeticionesSimultaneas)
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
        var llm = new ClienteLlmRetenido(new UsoTokens(TokensDeEntrada, 0));
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: Configuracion());
        llm.Liberar();

        var estados = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            estados.Add((await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta)).Estado);
        }

        Assert.Equal([200, 200, 200, 503], estados);
    }
}
