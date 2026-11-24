using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public static partial class UuidV7
{
    public static bool EsValido(string? valor) => valor is not null && FormatoRegex().IsMatch(valor);

    public static DateTimeOffset Marca(string valor) =>
        DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(string.Concat(valor.AsSpan(0, 8), valor.AsSpan(9, 4)), 16));

    [GeneratedRegex("^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")]
    private static partial Regex FormatoRegex();
}

public sealed class CorrelacionApiTests
{
    private const string ClienteCorrelacion = "00000000-0000-7000-8000-000000000001";
    private const string TraceIdDelCliente = "0af7651916cd43dd8448eb211c80319c";
    private const string TraceparentDelCliente = $"00-{TraceIdDelCliente}-b7ad6b7169203331-01";
    private const string RequestIdDelCliente = "solicitud-del-cliente-7788";
    private const int BytesDeCuerpoExcesivo = 17 * 1024;
    private static readonly TimeSpan ToleranciaMarca = TimeSpan.FromSeconds(1);

    private sealed record Captura(RespuestaApi Respuesta, EventoAuditoria Evento);

    public static TheoryData<int> Codigos => new() { 200, 400, 403, 415, 500 };

    public static TheoryData<string, string> EncabezadosDelCliente => new()
    {
        { "X-Correlation-Id", ClienteCorrelacion },
        { "X-Request-Id", RequestIdDelCliente },
        { "traceparent", TraceparentDelCliente }
    };

    private static async Task<Captura> EjecutarAsync(int codigo)
    {
        using var fabrica = codigo == 500 ? new FabricaApi(conocimiento: new ConocimientoQueExplota()) : new FabricaApi();
        _ = fabrica.Services;
        fabrica.Reloj.Fijar(DateTimeOffset.UtcNow);
        var respuesta = codigo switch
        {
            400 => await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", """{"pregunta": """),
            403 => await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?"),
            415 => await fabrica.EnviarConTipoAsync("""{"pregunta":"hola","usuario":{"rol":"estudiante","id":"EST-001"}}""", "text/plain"),
            _ => await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?")
        };
        return new Captura(respuesta, Assert.Single(fabrica.Auditoria.Eventos));
    }

    [Theory]
    [MemberData(nameof(Codigos))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-151")]
    [Trait("ESC", "ESC-112")]
    public async Task ESC_151_Toda_respuesta_lleva_x_correlation_id_uuid_v7_igual_al_de_la_auditoria(int codigo)
    {
        var captura = await EjecutarAsync(codigo);

        Assert.Equal(codigo, captura.Respuesta.Estado);
        var encabezado = captura.Respuesta.Encabezado("X-Correlation-Id");
        Assert.True(UuidV7.EsValido(encabezado), $"X-Correlation-Id no es UUID v7: {encabezado}");
        Assert.Equal(encabezado, captura.Evento.CorrelationId);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(415)]
    [InlineData(500)]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-151")]
    [Trait("ESC", "ESC-112")]
    public async Task ESC_151_El_trace_id_de_todo_problem_details_es_igual_al_x_correlation_id(int codigo)
    {
        var captura = await EjecutarAsync(codigo);

        Assert.Equal(captura.Respuesta.Encabezado("X-Correlation-Id"), captura.Respuesta.Json.GetProperty("traceId").GetString());
    }

    [Theory]
    [MemberData(nameof(Codigos))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-151")]
    public async Task ESC_151_La_marca_de_tiempo_del_uuid_difiere_menos_de_un_segundo_del_evento(int codigo)
    {
        var captura = await EjecutarAsync(codigo);

        Assert.True(UuidV7.EsValido(captura.Evento.CorrelationId), $"correlationId no es UUID v7: {captura.Evento.CorrelationId}");
        var diferencia = (UuidV7.Marca(captura.Evento.CorrelationId) - captura.Evento.Timestamp).Duration();
        Assert.True(diferencia < ToleranciaMarca, $"diferencia {diferencia}");
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-151")]
    public async Task ESC_151_Los_cinco_identificadores_son_distintos_entre_si()
    {
        var identificadores = new List<string?>();
        foreach (var codigo in new[] { 200, 400, 403, 415, 500 })
        {
            identificadores.Add((await EjecutarAsync(codigo)).Respuesta.Encabezado("X-Correlation-Id"));
        }

        Assert.All(identificadores, id => Assert.True(UuidV7.EsValido(id)));
        Assert.Equal(5, identificadores.Distinct().Count());
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-151")]
    public async Task ESC_151_Salud_y_el_405_tambien_llevan_x_correlation_id_uuid_v7()
    {
        using var fabrica = new FabricaApi();

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var metodo = await fabrica.EnviarAsync(HttpMethod.Put, "/api/asistente/preguntar", null);

        Assert.True(UuidV7.EsValido(salud.Encabezado("X-Correlation-Id")));
        Assert.True(UuidV7.EsValido(metodo.Encabezado("X-Correlation-Id")));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-151")]
    public async Task ESC_151_El_413_lleva_el_mismo_uuid_en_encabezado_trace_id_y_auditoria()
    {
        using var fabrica = new FabricaApi();
        var grande = $$"""{"pregunta":"hola","usuario":{"rol":"estudiante","id":"EST-001"},"relleno":"{{new string('x', BytesDeCuerpoExcesivo)}}"}""";

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", grande);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(413, respuesta.Estado);
        Assert.True(UuidV7.EsValido(respuesta.Encabezado("X-Correlation-Id")));
        Assert.Equal(respuesta.Encabezado("X-Correlation-Id"), evento.CorrelationId);
        Assert.Equal(evento.CorrelationId, respuesta.Json.GetProperty("traceId").GetString());
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-152")]
    public async Task ESC_152_El_x_correlation_id_del_cliente_se_ignora_y_no_se_registra()
    {
        using var fabrica = new FabricaApi();
        var encabezados = new Dictionary<string, string> { ["X-Correlation-Id"] = ClienteCorrelacion };

        var respuesta = await fabrica.PreguntarAsync("estudiante", "EST-001", "¿Cuál es mi promedio?", encabezados: encabezados);

        var propio = respuesta.Encabezado("X-Correlation-Id");
        Assert.True(UuidV7.EsValido(propio));
        Assert.NotEqual(ClienteCorrelacion, propio);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ClienteCorrelacion, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Auditoria.Eventos, e => e.CorrelationId == ClienteCorrelacion);
    }

    [Theory]
    [MemberData(nameof(EncabezadosDelCliente))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-152")]
    public async Task ESC_152_Ningun_encabezado_de_trazabilidad_del_cliente_llega_al_trace_id_ni_a_los_logs(string encabezado, string valor)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(
            HttpMethod.Post,
            "/api/asistente/preguntar",
            """{"pregunta": """,
            encabezados: new Dictionary<string, string> { [encabezado] = valor });

        var propio = respuesta.Encabezado("X-Correlation-Id");
        Assert.True(UuidV7.EsValido(propio));
        Assert.Equal(propio, respuesta.Json.GetProperty("traceId").GetString());
        Assert.DoesNotContain(TraceIdDelCliente, respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(valor, StringComparison.Ordinal) || m.Contains(TraceIdDelCliente, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Auditoria.Eventos, e => e.CorrelationId.Contains(TraceIdDelCliente, StringComparison.Ordinal) || e.CorrelationId == valor);
    }

    private sealed class ConocimientoQueExplota : IBaseConocimiento
    {
        public IReadOnlyList<TutorFlow.Core.Conocimiento.SeccionDocumento> BuscarSecciones(string consulta, int maximo) =>
            throw new InvalidOperationException("secreto-interno C:\\ruta\\interna");


        public TutorFlow.Core.Conocimiento.SeccionDocumento? ObtenerSeccion(TutorFlow.Core.Conocimiento.Cita cita) => null;
    }
}
