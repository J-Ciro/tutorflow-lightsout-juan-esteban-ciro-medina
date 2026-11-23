using TutorFlow.Pruebas.Soporte;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class Auditoria503ApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const int SalidaPorLlamada = 20;
    private const int PeticionesQueConsumenElPresupuesto = 3;
    private const decimal CostoDeseadoPorPeticionUsd = 0.0029m;
    private const decimal TokensPorMillon = 1_000_000m;
    private const int PrecisionDelPrecio = 6;
    private static readonly UsoTokens UsoPorLlamada = new(0, SalidaPorLlamada);

    public static TheoryData<string> FallosDelLlmTrasLeerDatos => new()
    {
        "HttpRequestException",
        "TaskCanceledException",
        "TimeoutException"
    };

    private static Exception Fallo(string tipo) => tipo switch
    {
        "HttpRequestException" => new HttpRequestException("llm caído"),
        "TaskCanceledException" => new TaskCanceledException("llm sin respuesta"),
        _ => new TimeoutException("llm sin respuesta")
    };

    private static ClienteLlmDoble LeeNotasYLuegoFalla(Exception fallo) =>
        new((_, numero) => numero == 0
            ? new RespuestaLlm(null, [new LlamadaHerramienta("lectura-1", HerramientasAsistente.Notas, "{}")], UsoPorLlamada, false)
            : throw fallo);

    private static Dictionary<string, string?> ConPrecios() => FabricaApi.Precios("10", "10");

    private static async Task<string> PrecioParaUnCostoPorPeticionAsync()
    {
        var sonda = ClienteLlmDoble.Texto("Tu promedio es 4.39.", new UsoTokens(0, 0));
        using var fabricaSonda = new FabricaApi(llm: sonda);
        await fabricaSonda.PreguntarEstudianteAsync("EST-001", Pregunta);
        var tokensDeEntrada = ConsumoRealista.Reportados(sonda.Solicitudes[0]);
        return Math.Round(CostoDeseadoPorPeticionUsd * TokensPorMillon / tokensDeEntrada, PrecisionDelPrecio)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    [Theory]
    [MemberData(nameof(FallosDelLlmTrasLeerDatos))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-113")]
    [Trait("SEG", "11.3")]
    [Trait("HALLAZGO", "H-04")]
    public async Task Un_503_por_llm_caido_tras_leer_datos_audita_lo_consultado_y_el_consumo(string tipo)
    {
        var llm = LeeNotasYLuegoFalla(Fallo(tipo));
        using var fabrica = new FabricaApi(llm: llm, configuracion: ConPrecios());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Error, evento.Resultado);
        Assert.Equal(["EST-001"], evento.EstudiantesConsultados.ToArray());
        Assert.Contains(RecursoConsultado.Notas, evento.RecursosConsultados);
        Assert.Contains(RecursoConsultado.Asistencia, evento.RecursosConsultados);
        Assert.Contains(evento.DatosConsultados, d => d.EstudianteId == "EST-001" && d.Recurso == RecursoConsultado.Notas && d.Asignatura == "CAL-201");
        Assert.Equal(ConsumoRealista.Reportados(llm.Solicitudes[0]) + SalidaPorLlamada, evento.Tokens);
        Assert.True(evento.CostoUsd > 0m);
        Assert.Empty(evento.Citas);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-113")]
    [Trait("SEG", "11.3")]
    [Trait("HALLAZGO", "H-04")]
    public async Task Un_503_por_llm_caido_sin_haber_leido_datos_audita_cero_consultas_y_cero_consumo()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(new HttpRequestException("llm caído")), configuracion: ConPrecios());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");

        Afirmaciones.ProblemDetails(respuesta, 503);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Error, evento.Resultado);
        Assert.Empty(evento.EstudiantesConsultados);
        Assert.Empty(evento.DatosConsultados);
        Assert.Equal(0, evento.Tokens);
        Assert.Equal(0m, evento.CostoUsd);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-113")]
    [Trait("SEG", "11.3")]
    [Trait("HALLAZGO", "H-04")]
    public async Task Un_503_por_presupuesto_agotado_audita_lo_leido_antes_de_reservar_y_no_llama_al_llm()
    {
        var llm = ClienteLlmDoble.Texto("Tu promedio es 4.39.", new UsoTokens(0, 0));
        var precioEntrada = await PrecioParaUnCostoPorPeticionAsync();
        var configuracion = new Dictionary<string, string?>(FabricaApi.Precios(precioEntrada, "0"))
        {
            ["Limites:PresupuestoDiarioUsd"] = "0.010",
            ["Llm:ReservaPorPeticionUsd"] = "0.004"
        };
        using var fabrica = new FabricaApi(llm: llm, configuracion: configuracion);
        for (var i = 0; i < PeticionesQueConsumenElPresupuesto; i++)
        {
            await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);
        }

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo voy en Bases de Datos?");

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.Equal(PeticionesQueConsumenElPresupuesto, llm.Llamadas);
        var evento = fabrica.Auditoria.Eventos[^1];
        Assert.Equal(ResultadoAuditoria.Error, evento.Resultado);
        Assert.Equal(["EST-001"], evento.EstudiantesConsultados.ToArray());
        Assert.Contains(RecursoConsultado.Notas, evento.RecursosConsultados);
        Assert.Equal(0, evento.Tokens);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-086")]
    [Trait("SEG", "11.3")]
    [Trait("HALLAZGO", "H-04")]
    public async Task Un_503_por_limite_de_herramientas_audita_los_datos_leidos_y_los_tokens_consumidos()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.PideHerramientaSinParar(), configuracion: ConPrecios());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.LimiteHerramientas, evento.Resultado);
        Assert.NotEmpty(evento.EstudiantesConsultados);
        Assert.True(evento.Tokens > 0);
        Assert.True(evento.CostoUsd > 0m);
    }
}
