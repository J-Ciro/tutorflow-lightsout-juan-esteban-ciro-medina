using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api.Tests.Llm;

public sealed class DatoOpacoDelProveedorApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string PreguntaDelCoordinador = "¿Qué estudiantes de mi programa perdieron materias?";
    private const string LlaveDeGemini = "AIzaLLAVEABC123";
    private const string FirmaUno = "CiQBjz1rX3Ab9ZfirmaUno";
    private const string FirmaDos = "CiQBjz1rX3Ab9ZfirmaDos";
    private const string PropiedadExtra = "extra_content";
    private const string TextoDelAsistente = "Voy a revisar tus notas.";

    private sealed record LlamadaDoble(string Id, string Nombre, string Argumentos, string? Firma);

    public static TheoryData<string?> Proveedores => new() { LlaveDeGemini, "sk-LLAVE-ABC123-openai", null };

    private static string ExtraEsperado(string firma) =>
        JsonSerializer.Serialize(new { google = new { thought_signature = firma } });

    private static HttpResponseMessage Turno(PeticionLlm peticion, string? texto, LlamadaDoble[] llamadas)
    {
        var llamadasJson = llamadas.Select(Llamada).ToArray();
        var mensaje = new Dictionary<string, object?> { ["role"] = "assistant", ["content"] = texto, ["tool_calls"] = llamadasJson };
        return RespuestasProveedor.Json(JsonSerializer.Serialize(new
        {
            id = "c2",
            @object = "chat.completion",
            choices = new object[] { new { index = 0, message = mensaje, finish_reason = "tool_calls" } },
            usage = new { prompt_tokens = ConsumoRealistaHttp.Reportados(peticion), completion_tokens = 5, total_tokens = ConsumoRealistaHttp.Reportados(peticion) + 5 }
        }));
    }

    private static Dictionary<string, object?> Llamada(LlamadaDoble llamada)
    {
        var elemento = new Dictionary<string, object?>
        {
            ["id"] = llamada.Id,
            ["type"] = "function",
            ["function"] = new { name = llamada.Nombre, arguments = llamada.Argumentos }
        };
        if (llamada.Firma is not null)
        {
            elemento[PropiedadExtra] = new { google = new { thought_signature = llamada.Firma } };
        }

        return elemento;
    }

    private static ManejadorLlmFalso Proveedor(string? texto, params LlamadaDoble[] llamadas) =>
        new((p, n) => n == 0 ? Turno(p, texto, llamadas) : RespuestasProveedor.Texto(p, "Datos revisados.", 5));

    private static FabricaApi Fabrica(string? llave, ManejadorLlmFalso manejador) =>
        new(
            variablesEntorno: llave is null ? [] : new Dictionary<string, string?> { ["LLM_API_KEY"] = llave },
            reemplazarLlm: false,
            manejadorHttp: manejador);

    private static List<JsonElement> LlamadasReenviadas(PeticionLlm peticion)
    {
        using var documento = JsonDocument.Parse(peticion.Cuerpo);
        return documento.RootElement.GetProperty("messages").EnumerateArray()
            .Where(m => m.GetProperty("role").GetString() == "assistant" && m.TryGetProperty("tool_calls", out _))
            .SelectMany(m => m.GetProperty("tool_calls").EnumerateArray())
            .Select(c => c.Clone())
            .ToList();
    }

    private static string? TextoReenviado(PeticionLlm peticion)
    {
        using var documento = JsonDocument.Parse(peticion.Cuerpo);
        var asistente = documento.RootElement.GetProperty("messages").EnumerateArray()
            .First(m => m.GetProperty("role").GetString() == "assistant" && m.TryGetProperty("tool_calls", out _));
        return asistente.TryGetProperty("content", out var contenido) && contenido.ValueKind == JsonValueKind.String ? contenido.GetString() : null;
    }

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192i_El_dato_opaco_de_la_llamada_vuelve_igual_con_el_mismo_id_nombre_y_argumentos(string? llave)
    {
        var manejador = Proveedor(null, new LlamadaDoble("call_77", HerramientasAsistente.Notas, "{}", FirmaUno));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(2, manejador.Peticiones.Count);
        var reenviada = Assert.Single(LlamadasReenviadas(manejador.Peticiones[1]));
        Assert.Equal(ExtraEsperado(FirmaUno), reenviada.GetProperty(PropiedadExtra).GetRawText());
        Assert.Equal("call_77", reenviada.GetProperty("id").GetString());
        Assert.Equal(HerramientasAsistente.Notas, reenviada.GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("{}", reenviada.GetProperty("function").GetProperty("arguments").GetString());
    }

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-07")]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192i_El_extra_content_no_aparece_en_la_auditoria_ni_en_el_log(string? llave)
    {
        var manejador = Proveedor(null, new LlamadaDoble("call_77", HerramientasAsistente.Notas, "{}", FirmaUno));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.NotEmpty(fabrica.Auditoria.Eventos);
        var auditoria = string.Join("\n", fabrica.Auditoria.Eventos.Select(e => JsonSerializer.Serialize(e) + e));
        Assert.DoesNotContain(PropiedadExtra, auditoria, StringComparison.Ordinal);
        Assert.DoesNotContain(PropiedadExtra, string.Join("\n", fabrica.Logs.Mensajes), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192i_Con_dos_llamadas_y_cada_una_con_su_dato_opaco_se_reenvian_ambos()
    {
        var manejador = Proveedor(
            null,
            new LlamadaDoble("call_1", HerramientasAsistente.Notas, "{}", FirmaUno),
            new LlamadaDoble("call_2", HerramientasAsistente.Asistencia, "{}", FirmaDos));
        using var fabrica = Fabrica(LlaveDeGemini, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        var reenviadas = LlamadasReenviadas(manejador.Peticiones[1]);
        Assert.Equal([ExtraEsperado(FirmaUno), ExtraEsperado(FirmaDos)], reenviadas.Select(c => c.GetProperty(PropiedadExtra).GetRawText()));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192k_El_texto_del_asistente_que_acompana_a_la_llamada_se_conserva_en_la_segunda_peticion()
    {
        var manejador = Proveedor(TextoDelAsistente, new LlamadaDoble("call_1", HerramientasAsistente.Notas, "{}", FirmaUno));
        using var fabrica = Fabrica(LlaveDeGemini, manejador);

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(TextoDelAsistente, TextoReenviado(manejador.Peticiones[1]));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192l_Un_id_ajeno_en_la_llamada_con_dato_opaco_da_403_tardio_y_no_hay_segunda_peticion()
    {
        var manejador = Proveedor(null, new LlamadaDoble("call_1", HerramientasAsistente.Estudiante, """{"estudianteId":"EST-004"}""", FirmaUno));
        using var fabrica = Fabrica(LlaveDeGemini, manejador);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", PreguntaDelCoordinador);

        Assert.Equal(403, respuesta.Estado);
        Assert.Single(manejador.Peticiones);
        Assert.Equal(MotivoDenegacion.HerramientaIdAjeno, Assert.Single(fabrica.Auditoria.Eventos).MotivoDenegacion);
        Assert.Equal(0, fabrica.Api.LecturasDatosDe("EST-004"));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192m_Con_dos_llamadas_una_con_dato_opaco_y_otra_sin_el_se_reenvian_reconstruidas_sin_extra_content()
    {
        var manejador = Proveedor(
            null,
            new LlamadaDoble("call_1", HerramientasAsistente.Notas, "{}", FirmaUno),
            new LlamadaDoble("call_2", HerramientasAsistente.Asistencia, "{}", null));
        using var fabrica = Fabrica(LlaveDeGemini, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        var reenviadas = LlamadasReenviadas(manejador.Peticiones[1]);
        Assert.Equal(2, reenviadas.Count);
        Assert.All(reenviadas, c => Assert.False(c.TryGetProperty(PropiedadExtra, out _)));
        Assert.Equal(["call_1", "call_2"], reenviadas.Select(c => c.GetProperty("id").GetString()));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-86")]
    public async Task ESC_192_Una_herramienta_fuera_del_rol_con_dato_opaco_no_reenvia_el_dato_opaco()
    {
        var manejador = Proveedor(null, new LlamadaDoble("call_1", HerramientasAsistente.Estudiante, """{"estudianteId":"EST-001"}""", FirmaUno));
        using var fabrica = Fabrica(LlaveDeGemini, manejador);

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.All(manejador.Peticiones.Skip(1), p => Assert.All(LlamadasReenviadas(p), c => Assert.False(c.TryGetProperty(PropiedadExtra, out _))));
        Assert.DoesNotContain(manejador.Peticiones.Skip(1), p => p.Cuerpo.Contains(FirmaUno, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-11")]
    [Trait("ESC", "ESC-192")]
    public async Task ESC_192_El_dato_opaco_no_aparece_en_la_respuesta_ni_en_el_log_ni_en_la_auditoria()
    {
        var manejador = Proveedor(null, new LlamadaDoble("call_1", HerramientasAsistente.Notas, "{}", FirmaUno));
        using var fabrica = Fabrica(LlaveDeGemini, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.DoesNotContain(FirmaUno, respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(FirmaUno, StringComparison.Ordinal));
        Assert.DoesNotContain(FirmaUno, string.Join("\n", fabrica.Auditoria.Eventos.Select(e => e.ToString())), StringComparison.Ordinal);
    }
}
