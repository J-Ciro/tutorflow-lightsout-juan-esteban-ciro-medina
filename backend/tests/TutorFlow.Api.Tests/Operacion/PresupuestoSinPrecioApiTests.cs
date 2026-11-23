using TutorFlow.Pruebas.Soporte;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class PresupuestoSinPrecioApiTests
{
    private const string PresupuestoDiario = "0.03";
    private const string ClaveReserva = "Llm:ReservaSinPrecioUsd";
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const int SalidaPorPeticion = 200;
    private const int PeticionesPorEscenario = 4;
    private const int Aceptadas = 3;
    private static readonly UsoTokens Uso = new(0, SalidaPorPeticion);

    public static TheoryData<string?> ReservasQueSeUsanPorDefecto => new()
    {
        "0.01",
        null,
        "0",
        "-0.5",
        "abc"
    };

    private static Dictionary<string, string?> Configuracion(string? reserva)
    {
        var configuracion = new Dictionary<string, string?> { ["Limites:PresupuestoDiarioUsd"] = PresupuestoDiario };
        if (reserva is not null)
        {
            configuracion[ClaveReserva] = reserva;
        }

        return configuracion;
    }

    private static Dictionary<string, string?> ModeloSinPrecio() =>
        new() { ["LLM_API_KEY"] = "sk-abc", ["LLM_MODELO"] = "modelo-sin-precio" };

    private static ClienteLlmDoble LlmDeLaPrueba() => ClienteLlmDoble.Texto("Tu promedio es 4.39.", Uso);

    private static async Task<IReadOnlyList<RespuestaApi>> PreguntarSeguidoAsync(FabricaApi fabrica)
    {
        var respuestas = new List<RespuestaApi>();
        for (var i = 0; i < PeticionesPorEscenario; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta));
        }

        return respuestas;
    }

    [Theory]
    [MemberData(nameof(ReservasQueSeUsanPorDefecto))]
    [Trait("CA", "CA-11")]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-180")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_180_Un_modelo_sin_precio_reserva_001_por_peticion_y_la_cuarta_da_503_sin_llamar_al_llm(string? reserva)
    {
        var llm = LlmDeLaPrueba();
        using var fabrica = new FabricaApi(llm: llm, configuracion: Configuracion(reserva), variablesEntorno: ModeloSinPrecio());

        var respuestas = await PreguntarSeguidoAsync(fabrica);

        Assert.Equal([200, 200, 200, 503], respuestas.Select(r => r.Estado).ToArray());
        for (var i = 0; i < Aceptadas; i++)
        {
            Assert.Equal(ConsumoRealista.Reportados(llm.Solicitudes[i]) + SalidaPorPeticion, respuestas[i].Tokens);
            Assert.Equal(0m, respuestas[i].CostoUsd);
        }

        Assert.Equal(Aceptadas, llm.Llamadas);
        Assert.All(fabrica.Auditoria.Eventos.Take(Aceptadas), e => Assert.True(e.PrecioDesconocido));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    [InlineData("abc")]
    [Trait("CA", "CA-11")]
    [Trait("ESC", "ESC-180")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_180_Una_reserva_sin_precio_invalida_se_ignora_con_una_advertencia_al_arrancar(string reserva)
    {
        using var fabrica = new FabricaApi(llm: LlmDeLaPrueba(), configuracion: Configuracion(reserva), variablesEntorno: ModeloSinPrecio());

        await PreguntarSeguidoAsync(fabrica);

        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("ReservaSinPrecioUsd", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-180")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_180_La_reserva_sin_precio_configurada_se_respeta_en_el_presupuesto_diario()
    {
        var llm = LlmDeLaPrueba();
        using var fabrica = new FabricaApi(llm: llm, configuracion: Configuracion("0.015"), variablesEntorno: ModeloSinPrecio());

        var respuestas = await PreguntarSeguidoAsync(fabrica);

        Assert.Equal([200, 200, 503, 503], respuestas.Select(r => r.Estado).ToArray());
        Assert.Equal(2, llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("ESC", "ESC-180")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_180_Ollama_con_precio_explicito_cero_no_se_limita_ni_marca_precio_desconocido()
    {
        var llm = LlmDeLaPrueba();
        using var fabrica = new FabricaApi(llm: llm, configuracion: Configuracion(null), variablesEntorno: new Dictionary<string, string?> { ["LLM_PROVEEDOR"] = "ollama" });

        var respuestas = await PreguntarSeguidoAsync(fabrica);

        Assert.All(respuestas, r =>
        {
            Assert.Equal(200, r.Estado);
            Assert.Equal(0m, r.CostoUsd);
        });
        Assert.Equal(PeticionesPorEscenario, llm.Llamadas);
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.False(e.PrecioDesconocido));
    }
}
