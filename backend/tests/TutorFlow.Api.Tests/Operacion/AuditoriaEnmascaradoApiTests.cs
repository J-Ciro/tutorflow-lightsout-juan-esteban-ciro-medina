using System.Text.Json;
using TutorFlow.Api;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class AuditoriaEnmascaradoApiTests
{
    private const string PreguntaConTerceros = "Notas de Andrés Martínez y EST-002";
    private const string PreguntaEnmascarada = "Notas de [TERCERO] y [TERCERO]";
    private const string PreguntaNoEvaluada = "[NO_EVALUADA]";
    private const string IpDelLimite = "10.0.0.8";
    private const int PeticionesPorMinuto = 60;
    private const int BytesDeCuerpoExcesivo = 17 * 1024;

    public static TheoryData<string> CuerposNoEvaluables => new()
    {
        """{"pregunta": """,
        """{"usuario":{"rol":"estudiante","id":"EST-001"}}""",
        """{"pregunta":123,"usuario":{"rol":"estudiante","id":"EST-001"}}"""
    };

    private static async Task<FabricaApi> FabricaConRegistroCargadoAsync(IReadOnlyDictionary<string, string?>? configuracion = null)
    {
        var fabrica = new FabricaApi(configuracion: configuracion);
        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");
        return fabrica;
    }

    private static string CuerpoConTerceros(string? relleno = null) =>
        JsonSerializer.Serialize(new { pregunta = PreguntaConTerceros, usuario = new { rol = "estudiante", id = "EST-001" }, relleno });

    private static EventoAuditoria Ultimo(FabricaApi fabrica) => fabrica.Auditoria.Eventos[^1];

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_El_400_por_rol_invalido_enmascara_a_los_terceros_en_la_pregunta_auditada()
    {
        using var fabrica = await FabricaConRegistroCargadoAsync();

        var respuesta = await fabrica.PreguntarAsync("admin", "EST-001", PreguntaConTerceros);

        Assert.Equal(400, respuesta.Estado);
        Assert.Equal(PreguntaEnmascarada, Ultimo(fabrica).Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_El_429_enmascara_a_los_terceros_en_la_pregunta_auditada()
    {
        using var fabrica = await FabricaConRegistroCargadoAsync(ConfiguracionDePrueba.TasaPorMinuto(PeticionesPorMinuto));
        for (var i = 1; i < PeticionesPorMinuto; i++)
        {
            await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?", IpDelLimite);
        }

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConTerceros, IpDelLimite);
        var superada = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConTerceros, IpDelLimite);

        Assert.Equal(403, respuesta.Estado);
        Assert.Equal(429, superada.Estado);
        Assert.Equal(PreguntaEnmascarada, Ultimo(fabrica).Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_El_403_por_identidad_invalida_enmascara_a_los_terceros_en_la_pregunta_auditada()
    {
        using var fabrica = await FabricaConRegistroCargadoAsync();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-999", PreguntaConTerceros);

        Afirmaciones.Negativa(respuesta, "estudiante");
        var evento = Ultimo(fabrica);
        Assert.Equal(MotivoDenegacion.IdentidadInvalida, evento.MotivoDenegacion);
        Assert.Equal(PreguntaEnmascarada, evento.Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_El_413_registra_la_pregunta_como_no_evaluada()
    {
        using var fabrica = await FabricaConRegistroCargadoAsync();
        var cuerpo = CuerpoConTerceros(new string('x', BytesDeCuerpoExcesivo));

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo);

        Assert.Equal(413, respuesta.Estado);
        Assert.Equal(PreguntaNoEvaluada, Ultimo(fabrica).Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_El_415_registra_la_pregunta_como_no_evaluada()
    {
        using var fabrica = await FabricaConRegistroCargadoAsync();
        var cuerpo = CuerpoConTerceros();

        var respuesta = await fabrica.EnviarConTipoAsync(cuerpo, "text/plain");

        Assert.Equal(415, respuesta.Estado);
        Assert.Equal(PreguntaNoEvaluada, Ultimo(fabrica).Pregunta);
    }

    [Theory]
    [MemberData(nameof(CuerposNoEvaluables))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_Un_cuerpo_no_interpretable_registra_la_pregunta_como_no_evaluada(string cuerpo)
    {
        using var fabrica = await FabricaConRegistroCargadoAsync();

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo);

        Assert.Equal(400, respuesta.Estado);
        Assert.Equal(PreguntaNoEvaluada, Ultimo(fabrica).Pregunta);
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-148")]
    public async Task ESC_148_Ningun_evento_de_rutas_tempranas_contiene_al_tercero_en_la_pregunta()
    {
        using var fabrica = await FabricaConRegistroCargadoAsync();

        await fabrica.PreguntarAsync("admin", "EST-001", PreguntaConTerceros);
        await fabrica.PreguntarEstudianteAsync("EST-999", PreguntaConTerceros);
        await fabrica.EnviarConTipoAsync(CuerpoConTerceros(), "text/plain");

        Assert.All(fabrica.Auditoria.Eventos.Skip(1), e =>
        {
            Assert.DoesNotContain("Andrés Martínez", e.Pregunta, StringComparison.Ordinal);
            Assert.DoesNotContain("EST-002", e.Pregunta, StringComparison.Ordinal);
        });
    }
}

[Collection("Consola")]
public sealed class AuditoriaIdSolicitadoApiTests
{
    public static TheoryData<string, string, string, string, string> Casos => new()
    {
        { "coordinador", "COORD-01", "EST-004", "¿Qué dice el reglamento sobre la habilitación?", "EST-004" },
        { "estudiante", "EST-001", "EST-002", "¿Qué dice el reglamento sobre la habilitación?", "EST-002" },
        { "coordinador", "COORD-01", "../EST-004\nx", "¿Qué dice el reglamento sobre la habilitación?", "[FORMATO_INVALIDO]" }
    };

    private static async Task<(RespuestaApi Respuesta, JsonElement Evento)> PreguntarCapturandoAsync(string rol, string id, string pedido, string pregunta)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.PideEstudiante(pedido), auditoriaReal: true);
        var original = Console.Out;
        using var escritor = new EscritorSincronizado();
        Console.SetOut(escritor);
        RespuestaApi respuesta;
        try
        {
            respuesta = await fabrica.PreguntarAsync(rol, id, pregunta);
        }
        finally
        {
            Console.SetOut(original);
        }

        var correlacion = respuesta.Encabezados.GetValues(CorrelacionDePeticion.Encabezado).Single();
        var lineas = escritor.ToString().Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Contains(correlacion, StringComparison.Ordinal)).ToList();
        return (respuesta, JsonElement.Parse(Assert.Single(lineas)));
    }

    [Theory]
    [MemberData(nameof(Casos))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-153")]
    public async Task ESC_153_El_403_tardio_registra_el_id_solicitado_o_formato_invalido_en_una_sola_linea(string rol, string id, string pedido, string pregunta, string esperado)
    {
        var (respuesta, evento) = await PreguntarCapturandoAsync(rol, id, pedido, pregunta);

        Assert.Equal(403, respuesta.Estado);
        Assert.Equal("herramienta_id_ajeno", evento.GetProperty("motivoDenegacion").GetString());
        Assert.Equal(esperado, evento.GetProperty("idSolicitado").GetString());
    }

    [Theory]
    [MemberData(nameof(Casos))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-153")]
    public async Task ESC_153_El_id_solicitado_no_aparece_como_consultado(string rol, string id, string pedido, string pregunta, string idSolicitado)
    {
        var (_, evento) = await PreguntarCapturandoAsync(rol, id, pedido, pregunta);

        Assert.True(evento.TryGetProperty("idSolicitado", out _), $"falta idSolicitado ({idSolicitado})");
        Assert.DoesNotContain(pedido, evento.GetProperty("estudiantesConsultados").GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain(pedido, evento.GetProperty("datosConsultados").GetRawText(), StringComparison.Ordinal);
    }
}
