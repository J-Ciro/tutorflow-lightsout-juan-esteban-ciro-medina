using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ConfiguracionEnLimitesApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const int PeticionesSimultaneas = 3;
    private const int Aceptadas = 2;
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(30);

    public static TheoryData<string, string?> ValoresValidosSinAdvertencia => new()
    {
        { "Conocimiento:RelevanciaMinima", "1" },
        { "Conocimiento:RelevanciaMinima", "10" },
        { "Conocimiento:ApoyoMinimoRespaldo", "1" },
        { "Conocimiento:ApoyoMinimoRespaldo", "10" },
        { "Limites:MaximoCaracteresPregunta", "1" },
        { "Limites:MaximoCaracteresPregunta", "1000" },
        { "Limites:MaximoBytesCuerpo", "1024" },
        { "Limites:MaximoBytesCuerpo", "16384" },
        { "Limites:PreguntasPorMinutoPorIp", "1" },
        { "Limites:PreguntasPorMinutoPorIp", "10000" },
        { "Limites:MaximoSeccionesRecuperadas", "1" },
        { "Limites:MaximoSeccionesRecuperadas", "6" },
        { "Limites:MaximoTokensSalida", "1" },
        { "Limites:MaximoTokensSalida", "2000" },
        { "Limites:MaximoInvocacionesHerramientas", "1" },
        { "Limites:MaximoInvocacionesHerramientas", "4" },
        { "Limites:TimeoutLlmSegundos", "1" },
        { "Limites:TimeoutLlmSegundos", "60" },
        { "Limites:PresupuestoApiAcademicaSegundos", "1" },
        { "Limites:PresupuestoApiAcademicaSegundos", "30" },
        { "Limites:PresupuestoDiarioUsd", "0.000001" },
        { "Limites:PresupuestoDiarioUsd", "1000" },
        { "Limites:MaximoIdentidadesPorIp", "1" },
        { "Limites:MaximoIdentidadesPorIp", "100" },
        { "Limites:VentanaIdentidadesMinutos", "1" },
        { "Limites:VentanaIdentidadesMinutos", "1440" },
        { "Limites:BloquearMultiplesIdentidades", "true" },
        { "Limites:BloquearMultiplesIdentidades", "false" },
        { "LLM_PRECIO_ENTRADA", "0" },
        { "LLM_PRECIO_ENTRADA", "1000" },
        { "LLM_PRECIO_SALIDA", "0" },
        { "LLM_PRECIO_SALIDA", "1000" },
        { "Llm:Proveedores:anthropic:Modelos:0:PrecioEntradaPorMillon", "0" },
        { "Llm:Proveedores:anthropic:Modelos:0:PrecioSalidaPorMillon", "1000" },
        { "Limites:TimeoutLlmSegundos", "" },
        { "Conocimiento:RelevanciaMinima", "  " },
        { "Limites:BloquearMultiplesIdentidades", "" },
        { "LLM_PRECIO_ENTRADA", "" },
        { "Llm:ReservaSinPrecioUsd", "" },
        { "Llm:ReservaSinPrecioUsd", "   " },
        { "Llm:ReservaPorPeticionUsd", "" },
        { "Llm:ReservaPorPeticionUsd", null }
    };

    public static TheoryData<string, string, string, string, string, string> ReservasInvalidas => new()
    {
        {
            "o",
            "Llm:ReservaSinPrecioUsd",
            "-0.5",
            "0.02",
            "sin-precio",
            Ignorada("Llm:ReservaSinPrecioUsd", "-0.5", "decimal mayor que 0", "0.01")
        },
        {
            "p",
            "Llm:ReservaSinPrecioUsd",
            "abc",
            "0.02",
            "sin-precio",
            Ignorada("Llm:ReservaSinPrecioUsd", "abc", "decimal mayor que 0", "0.01")
        },
        {
            "o-cero",
            "Llm:ReservaSinPrecioUsd",
            "0",
            "0.02",
            "sin-precio",
            Ignorada("Llm:ReservaSinPrecioUsd", "0", "decimal mayor que 0", "0.01")
        },
        {
            "q",
            "Llm:ReservaPorPeticionUsd",
            "0",
            "0.0036",
            "haiku",
            Ignorada("Llm:ReservaPorPeticionUsd", "0", "decimal mayor que 0", "el estimado de 8000 tokens de entrada y 2000 de salida")
        },
        {
            "q-negativa",
            "Llm:ReservaPorPeticionUsd",
            "-1",
            "0.0036",
            "haiku",
            Ignorada("Llm:ReservaPorPeticionUsd", "-1", "decimal mayor que 0", "el estimado de 8000 tokens de entrada y 2000 de salida")
        },
        {
            "q-no-numerica",
            "Llm:ReservaPorPeticionUsd",
            "abc",
            "0.0036",
            "haiku",
            Ignorada("Llm:ReservaPorPeticionUsd", "abc", "decimal mayor que 0", "el estimado de 8000 tokens de entrada y 2000 de salida")
        }
    };

    private static Dictionary<string, string?> EntornoDe(string modelo) =>
        modelo == "haiku" ? EntornoConLlave() : EntornoSinPrecio();

    private static async Task<IReadOnlyList<RespuestaApi>> PreguntarSimultaneasAsync(FabricaApi fabrica, ClienteLlmRetenido llm)
    {
        var peticiones = Enumerable.Range(0, PeticionesSimultaneas)
            .Select(_ => Task.Run(() => fabrica.PreguntarEstudianteAsync("EST-001", Pregunta)))
            .ToList();
        var pendientes = peticiones.ToList();
        try
        {
            var rechazada = await Task.WhenAny(pendientes).WaitAsync(EsperaMaxima);
            pendientes.Remove(rechazada);
        }
        finally
        {
            llm.Liberar();
        }

        return await Task.WhenAll(peticiones);
    }

    [Theory]
    [MemberData(nameof(ValoresValidosSinAdvertencia))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public async Task ESC_181_Un_valor_en_el_limite_del_rango_o_vacio_arranca_sin_advertencia(string clave, string? valor)
    {
        using var fabrica = new FabricaApi(configuracion: new Dictionary<string, string?> { [clave] = valor }, variablesEntorno: EntornoConLlave());

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Equal(200, salud.Estado);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(LlaveFalsa, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ReservasInvalidas))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_181_Una_reserva_invalida_usa_su_defecto_con_advertencia_y_no_desactiva_el_presupuesto(string caso, string clave, string valor, string presupuesto, string modelo, string advertencia)
    {
        Assert.NotEmpty(caso);
        var llm = new ClienteLlmRetenido(new UsoTokens(100, 20));
        var configuracion = new Dictionary<string, string?> { ["Limites:PresupuestoDiarioUsd"] = presupuesto, [clave] = valor };
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: configuracion, variablesEntorno: EntornoDe(modelo));
        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var respuestas = await PreguntarSimultaneasAsync(fabrica, llm);

        Assert.Equal(200, salud.Estado);
        Assert.Contains(advertencia, fabrica.Logs.Mensajes);
        Assert.Equal(Aceptadas, respuestas.Count(r => r.Estado == 200));
        Assert.Equal(PeticionesSimultaneas - Aceptadas, respuestas.Count(r => r.Estado == 503));
        Assert.Equal(Aceptadas, llm.Llamadas);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(LlaveFalsa, StringComparison.Ordinal) || m.Contains(LlaveFalsaSinPrecio, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_181w_La_reserva_vacia_usa_001_sin_advertencia_y_limita_a_dos_de_tres_peticiones()
    {
        var llm = new ClienteLlmRetenido(new UsoTokens(100, 20));
        var configuracion = new Dictionary<string, string?> { ["Limites:PresupuestoDiarioUsd"] = "0.02", ["Llm:ReservaSinPrecioUsd"] = "" };
        using var fabrica = new FabricaApi(clienteLlm: llm, configuracion: configuracion, variablesEntorno: EntornoSinPrecio());
        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        var respuestas = await PreguntarSimultaneasAsync(fabrica, llm);

        Assert.Equal(Aceptadas, respuestas.Count(r => r.Estado == 200));
        Assert.Equal(PeticionesSimultaneas - Aceptadas, respuestas.Count(r => r.Estado == 503));
        Assert.Equal(Aceptadas, llm.Llamadas);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }
}
