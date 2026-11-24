using System.Net;
using System.Text;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class PrecioDesdeCatalogoApiTests
{
    private const string Modelo = "gemini-3.5-flash-lite";
    private const string IdEnCatalogo = "google/gemini-3.5-flash-lite";
    private const string LlaveGemini = "AIzaLLAVEABC123";
    private const string LlaveOpenAi = "sk-LLAVE-ABC123-openai";
    private const string ClaveUrl = "Llm:CatalogoPrecios:Url";
    private const string UrlDePrueba = "https://catalogo.precios.test/api/v1/models";
    private const string Pregunta = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const int Entrada = 1000;
    private const int Salida = 200;
    private const decimal CostoDeLaTabla = 0.0008m;
    private const decimal CostoDelCatalogo = 0.002m;
    private const decimal Tolerancia = 0.000001m;
    private const int BytesMaximos = 10_485_760;
    private const string PrefijoAdvertencia = "Configuración ignorada: no se encontró el precio del modelo ";
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(20);

    public static TheoryData<string, string?> PrecioDeEntradaInvalido => new()
    {
        { "negativo", "-0.0000003" },
        { "mayor-al-limite", "0.0011" },
        { "notacion-cientifica", "3e-7" },
        { "coma-decimal", "0,0000003" },
        { "nulo", null },
        { "texto", "abc" }
    };

    public static TheoryData<string, string, string, string, string, string, decimal, bool> BusquedaEnElCatalogo => new()
    {
        { "1-openrouter-id-exacto", "openrouter", "sk-or-LLAVE-ABC123", "openai/gpt-4o-mini", "openai/gpt-4o-mini", "0.0000002|0.0000008", 0.00036m, false },
        { "2-sufijo-unico", "openai-compatible", LlaveOpenAi, "deepseek-chat", "deepseek/deepseek-chat", "0.00000125|0.00001", 0.00325m, false },
        { "3-prefijo-del-proveedor", "anthropic", "sk-ant-LLAVE-ABC123", "claude-haiku-5-5", "anthropic/claude-haiku-5-5", "0.0000002|0.000001", 0.0004m, false },
        { "4-sufijo-ambiguo", "openai-compatible", LlaveOpenAi, "deepseek-chat", "deepseek/deepseek-chat;otro/deepseek-chat", "0.00000125|0.00001", 0m, true },
        { "5-mayusculas-distintas", "openai-compatible", LlaveOpenAi, "DeepSeek-Chat", "deepseek/deepseek-chat", "0.00000125|0.00001", 0m, true },
        { "6-sin-entrada-usa-la-tabla", "groq", "gsk_LLAVEABC123", "llama-3.1-8b-instant", "otro/modelo", "0.1|0.1", 0.000066m, false }
    };

    private static Dictionary<string, string?> Tabla() => new()
    {
        [ClaveUrl] = UrlDePrueba,
        ["Llm:Proveedores:gemini:ModeloPorDefecto"] = Modelo,
        ["Llm:Proveedores:gemini:Modelos:0:Nombre"] = Modelo,
        ["Llm:Proveedores:gemini:Modelos:0:PrecioEntradaPorMillon"] = "0.30",
        ["Llm:Proveedores:gemini:Modelos:0:PrecioSalidaPorMillon"] = "2.50"
    };

    private static Dictionary<string, string?> EntornoGemini() =>
        new() { ["LLM_API_KEY"] = LlaveGemini, ["LLM_MODELO"] = Modelo };

    private static string Json(string id, string? prompt, string completion) =>
        $$$"""{"data":[{"id":"otro/modelo","pricing":{"prompt":"0.1","completion":"0.1"}},{"id":"{{{id}}}","pricing":{"prompt":{{{Literal(prompt)}}},"completion":{{{Literal(completion)}}}}}]}""";

    private static string Literal(string? valor) => valor is null ? "null" : $"\"{valor}\"";

    private static ManejadorConCatalogo ConCatalogo(string json) =>
        new(_ => Task.FromResult(ManejadorConCatalogo.Catalogo(json)), Entrada, Salida);

    private static ManejadorConCatalogo ConRespuesta(Func<HttpResponseMessage> respuesta) =>
        new(_ => Task.FromResult(respuesta()), Entrada, Salida);

    private static FabricaApi Fabrica(ManejadorConCatalogo manejador, Dictionary<string, string?> configuracion, Dictionary<string, string?> entorno) =>
        new(configuracion: configuracion, variablesEntorno: entorno, reemplazarLlm: false, manejadorHttp: manejador);

    private static Task<bool> EsperarAsync(Func<bool> condicion) =>
        EsperarAsync(() => Task.FromResult(condicion()));

    private static async Task<bool> EsperarAsync(Func<Task<bool>> condicion)
    {
        using var limite = new CancellationTokenSource(Espera);
        while (!await condicion())
        {
            try
            {
                await Task.Delay(Intervalo, limite.Token);
            }
            catch (OperationCanceledException)
            {
                return await condicion();
            }
        }

        return true;
    }

    private static async Task<decimal> PreguntarHastaAsync(FabricaApi fabrica, decimal esperado)
    {
        var costo = decimal.MinusOne;
        await EsperarAsync(async () =>
        {
            costo = (await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta)).CostoUsd;
            return Math.Abs(costo - esperado) <= Tolerancia;
        });
        return costo;
    }

    private static async Task<IReadOnlyList<RespuestaApi>> PreguntarTresVecesAsync(FabricaApi fabrica)
    {
        var respuestas = new List<RespuestaApi>();
        for (var i = 0; i < 3; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta));
        }

        return respuestas;
    }

    private static async Task<IReadOnlyList<RespuestaApi>> PreguntarTrasLosIntentosAsync(FabricaApi fabrica, ManejadorConCatalogo manejador, int intentos)
    {
        _ = fabrica.Services;
        await EsperarAsync(() => manejador.CatalogoAtendido >= intentos);
        return await PreguntarTresVecesAsync(fabrica);
    }

    private static void AfirmarCostoDeLaTabla(IEnumerable<RespuestaApi> respuestas, FabricaApi fabrica)
    {
        Assert.All(respuestas, r =>
        {
            Assert.Equal(200, r.Estado);
            Assert.InRange(r.CostoUsd, CostoDeLaTabla - Tolerancia, CostoDeLaTabla + Tolerancia);
        });
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal));
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.False(e.PrecioDesconocido));
    }

    private static string FilaDeCatalogo(string[] ids, string precios)
    {
        var partes = precios.Split('|');
        var entradas = ids.Select(id => $$$"""{"id":"{{{id}}}","pricing":{"prompt":"{{{partes[0]}}}","completion":"{{{partes[1]}}}"}}""");
        return $$$"""{"data":[{{{string.Join(",", entradas)}}}]}""";
    }

    private static string CuerpoDeBytes(int bytes)
    {
        var json = Json(IdEnCatalogo, "0.000001", "0.000005");
        return json + new string(' ', bytes - Encoding.UTF8.GetByteCount(json));
    }

    [Theory]
    [InlineData("0.0000003", "0.0000025", 0.0008)]
    [InlineData("0.000001", "0.000005", 0.002)]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 aj")]
    public async Task ESC_181aj_El_catalogo_publico_manda_sobre_la_tabla_y_se_descarga_una_vez_sin_advertencia(string prompt, string completion, double esperado)
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, prompt, completion));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, (decimal)esperado);

        Assert.InRange(costo, (decimal)esperado - Tolerancia, (decimal)esperado + Tolerancia);
        Assert.Single(manejador.SolicitudesCatalogo);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal));
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.False(e.PrecioDesconocido));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 aj")]
    public async Task ESC_181aj_La_url_por_defecto_del_catalogo_es_la_de_openrouter_sin_llave()
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, "0.000001", "0.000005"));
        var configuracion = Tabla();
        configuracion[ClaveUrl] = null;
        using var fabrica = Fabrica(manejador, configuracion, EntornoGemini());

        await PreguntarHastaAsync(fabrica, CostoDelCatalogo);

        var solicitud = Assert.Single(manejador.SolicitudesCatalogo);
        Assert.Equal("https://openrouter.ai/api/v1/models", solicitud.Uri.ToString());
    }

    [Theory]
    [MemberData(nameof(BusquedaEnElCatalogo))]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ak")]
    public async Task ESC_181ak_La_busqueda_del_modelo_en_el_catalogo_sigue_id_exacto_prefijo_del_proveedor_o_sufijo_unico(string caso, string proveedor, string llave, string modelo, string ids, string precios, decimal esperado, bool conAdvertencia)
    {
        Assert.NotEmpty(caso);
        var manejador = ConCatalogo(FilaDeCatalogo(ids.Split(';'), precios));
        var entorno = new Dictionary<string, string?> { ["LLM_API_KEY"] = llave, ["LLM_MODELO"] = modelo, ["LLM_PROVEEDOR"] = proveedor };
        if (proveedor == "openai-compatible")
        {
            entorno["LLM_URL_BASE"] = "https://api.compatible.test/v1";
        }

        using var fabrica = Fabrica(manejador, new Dictionary<string, string?> { [ClaveUrl] = UrlDePrueba }, entorno);

        var costo = esperado > 0 ? await PreguntarHastaAsync(fabrica, esperado) : decimal.Zero;
        var respuestas = await PreguntarTresVecesAsync(fabrica);

        Assert.InRange(costo, esperado - Tolerancia, esperado + Tolerancia);
        Assert.All(respuestas, r => Assert.InRange(r.CostoUsd, esperado - Tolerancia, esperado + Tolerancia));
        Assert.Equal(conAdvertencia, fabrica.Logs.Mensajes.Any(m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal)));
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.Equal(conAdvertencia, e.PrecioDesconocido));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ak")]
    public async Task ESC_181ak_Un_id_que_solo_contiene_el_modelo_sin_terminar_en_barra_modelo_no_cuenta()
    {
        var manejador = ConCatalogo(Json("google/gemini-3.5-flash-lite-preview", "0.000001", "0.000005"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var respuestas = await PreguntarTrasLosIntentosAsync(fabrica, manejador, 1);

        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ak")]
    public async Task ESC_181ak_Un_id_repetido_en_el_catalogo_se_ignora_y_se_usa_la_tabla()
    {
        var repetido = FilaDeCatalogo([IdEnCatalogo, IdEnCatalogo], "0.000001|0.000005");
        var manejador = ConCatalogo(repetido);
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var respuestas = await PreguntarTrasLosIntentosAsync(fabrica, manejador, 1);

        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Theory]
    [InlineData("tarda-mas-de-5s")]
    [InlineData("500")]
    [InlineData("json-invalido")]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 al")]
    public async Task ESC_181al_Un_catalogo_que_falla_se_reintenta_una_vez_y_se_usa_la_tabla_sin_advertencia(string falla)
    {
        var manejador = falla switch
        {
            "tarda-mas-de-5s" => new ManejadorConCatalogo(_ => throw new TaskCanceledException("timeout", new TimeoutException()), Entrada, Salida),
            "500" => ConRespuesta(() => new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            _ => ConCatalogo("{esto no es json")
        };
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var respuestas = await PreguntarTrasLosIntentosAsync(fabrica, manejador, 2);

        Assert.Equal(2, manejador.SolicitudesCatalogo.Count);
        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 al")]
    public async Task ESC_181al_Con_la_url_del_catalogo_vacia_no_hay_ningun_get_y_se_usa_la_tabla()
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, "0.000001", "0.000005"));
        var configuracion = Tabla();
        configuracion[ClaveUrl] = "";
        using var fabrica = Fabrica(manejador, configuracion, EntornoGemini());

        var respuestas = await PreguntarTresVecesAsync(fabrica);

        Assert.Empty(manejador.SolicitudesCatalogo);
        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-39")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 am")]
    public async Task ESC_181am_Un_modelo_ausente_del_catalogo_y_de_la_tabla_advierte_una_sola_vez_y_el_costo_es_0()
    {
        var manejador = ConCatalogo(FilaDeCatalogo(["deepseek/otro-modelo"], "0.000001|0.000005"));
        var entorno = new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveOpenAi, ["LLM_MODELO"] = "deepseek-chat", ["LLM_PROVEEDOR"] = "openai-compatible", ["LLM_URL_BASE"] = "https://api.compatible.test/v1" };
        using var fabrica = Fabrica(manejador, new Dictionary<string, string?> { [ClaveUrl] = UrlDePrueba }, entorno);
        _ = fabrica.Services;
        var advertencia = $"{PrefijoAdvertencia}deepseek-chat en el catálogo público ni en appsettings; costoUsd será 0.";

        await EsperarAsync(() => fabrica.Logs.Mensajes.Any(m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal)));
        var respuestas = await PreguntarTresVecesAsync(fabrica);

        Assert.All(respuestas, r =>
        {
            Assert.Equal(200, r.Estado);
            Assert.True(r.Tokens > 0);
            Assert.Equal(0m, r.CostoUsd);
        });
        Assert.Equal(1, fabrica.Logs.Mensajes.Count(m => m == advertencia));
        Assert.Equal(1, fabrica.Logs.Mensajes.Count(m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal)));
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.True(e.PrecioDesconocido));
    }

    [Theory]
    [MemberData(nameof(PrecioDeEntradaInvalido))]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 an")]
    public async Task ESC_181an_Un_precio_invalido_o_fuera_de_rango_en_el_catalogo_se_ignora_y_se_usa_la_tabla(string caso, string? prompt)
    {
        Assert.NotEmpty(caso);
        var manejador = ConCatalogo(Json(IdEnCatalogo, prompt, "0.0000025"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var respuestas = await PreguntarTrasLosIntentosAsync(fabrica, manejador, 1);

        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Theory]
    [InlineData("0.001", 1.0005)]
    [InlineData("0", 0.0005)]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 an")]
    public async Task ESC_181an_Los_limites_exactos_0_001_y_0_por_token_se_aceptan(string prompt, double esperado)
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, prompt, "0.0000025"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, (decimal)esperado);

        Assert.InRange(costo, (decimal)esperado - Tolerancia, (decimal)esperado + Tolerancia);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 an")]
    public async Task ESC_181an_Un_cuerpo_de_10485760_bytes_se_lee_completo()
    {
        var manejador = ConCatalogo(CuerpoDeBytes(BytesMaximos));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, CostoDelCatalogo);

        Assert.InRange(costo, CostoDelCatalogo - Tolerancia, CostoDelCatalogo + Tolerancia);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 an")]
    public async Task ESC_181an_Un_cuerpo_de_10485761_bytes_se_ignora_con_2_get_y_se_usa_la_tabla()
    {
        var manejador = ConCatalogo(CuerpoDeBytes(BytesMaximos + 1));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var respuestas = await PreguntarTrasLosIntentosAsync(fabrica, manejador, 2);

        Assert.Equal(2, manejador.SolicitudesCatalogo.Count);
        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Theory]
    [InlineData("qwen2.5:7b")]
    [InlineData("llama3.1:8b")]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-39")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ao")]
    public async Task ESC_181ao_Ollama_da_costo_0_sin_ningun_get_sin_advertencia_y_sin_precio_desconocido(string modelo)
    {
        var manejador = new ManejadorConCatalogo(_ => Task.FromResult(ManejadorConCatalogo.Catalogo("{}")), null, Salida);
        var entorno = new Dictionary<string, string?> { ["LLM_PROVEEDOR"] = "ollama", ["LLM_MODELO"] = modelo };
        using var fabrica = Fabrica(manejador, new Dictionary<string, string?> { [ClaveUrl] = UrlDePrueba }, entorno);

        var respuestas = await PreguntarTresVecesAsync(fabrica);

        Assert.All(respuestas, r => Assert.Equal(0m, r.CostoUsd));
        Assert.Empty(manejador.SolicitudesCatalogo);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal));
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.False(e.PrecioDesconocido));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ap")]
    public async Task ESC_181ap_Salud_y_la_primera_respuesta_no_esperan_al_catalogo_retenido_y_la_segunda_usa_su_precio()
    {
        var retenido = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manejador = new ManejadorConCatalogo(ct => retenido.Task.WaitAsync(ct), Entrada, Salida);
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;
        var catalogoPedido = await EsperarAsync(() => manejador.SolicitudesCatalogo.Count >= 1);

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null).WaitAsync(Espera);
        var primera = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta).WaitAsync(Espera);
        retenido.SetResult(ManejadorConCatalogo.Catalogo(Json(IdEnCatalogo, "0.000001", "0.000005")));
        var costo = await PreguntarHastaAsync(fabrica, CostoDelCatalogo);

        Assert.True(catalogoPedido);
        Assert.Equal(200, salud.Estado);
        Assert.Equal(200, primera.Estado);
        Assert.InRange(primera.CostoUsd, CostoDeLaTabla - Tolerancia, CostoDeLaTabla + Tolerancia);
        Assert.InRange(costo, CostoDelCatalogo - Tolerancia, CostoDelCatalogo + Tolerancia);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("abc", "-1")]
    [InlineData("1.0", "5.0")]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 aq")]
    public async Task ESC_181aq_LLM_PRECIO_ENTRADA_y_LLM_PRECIO_SALIDA_ya_no_existen_y_no_alteran_el_costo_ni_el_arranque(string entrada, string salida)
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, "0.000001", "0.000005"));
        var entorno = EntornoGemini();
        entorno["LLM_PRECIO_ENTRADA"] = entrada;
        entorno["LLM_PRECIO_SALIDA"] = salida;
        var configuracion = Tabla();
        configuracion[ClaveUrl] = "";
        using var fabrica = Fabrica(manejador, configuracion, entorno);

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var respuestas = await PreguntarTresVecesAsync(fabrica);

        Assert.Equal(200, salud.Estado);
        AfirmarCostoDeLaTabla(respuestas, fabrica);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("LLM_PRECIO", StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ar")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_181ar_Un_nombre_de_modelo_con_un_salto_de_linea_no_forja_una_linea_de_arranque_en_la_advertencia()
    {
        var manejador = ConCatalogo(FilaDeCatalogo(["deepseek/otro-modelo"], "0.000001|0.000005"));
        var entorno = new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveOpenAi, ["LLM_MODELO"] = "deepseek-chat\nArranque correcto", ["LLM_PROVEEDOR"] = "openai-compatible", ["LLM_URL_BASE"] = "https://api.compatible.test/v1" };
        using var fabrica = Fabrica(manejador, new Dictionary<string, string?> { [ClaveUrl] = UrlDePrueba }, entorno);
        _ = fabrica.Services;

        await EsperarAsync(() => fabrica.Logs.Mensajes.Any(m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal)));
        await PreguntarTresVecesAsync(fabrica);

        var advertencias = fabrica.Logs.Mensajes.Where(m => m.StartsWith(PrefijoAdvertencia, StringComparison.Ordinal)).ToList();
        Assert.Single(advertencias);
        Assert.Contains("deepseek-chat\\nArranque correcto", advertencias[0], StringComparison.Ordinal);
        Assert.DoesNotContain(fabrica.Logs.Mensajes.SelectMany(m => m.Split('\n')), l => l.StartsWith("Arranque correcto", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ar")]
    [Trait("SEG", "SEG-11")]
    public async Task ESC_181ar_Un_302_del_catalogo_no_se_sigue_se_reintenta_una_vez_y_se_usa_la_tabla()
    {
        var manejador = ConRespuesta(() =>
        {
            var redireccion = new HttpResponseMessage(HttpStatusCode.Found);
            redireccion.Headers.Location = new Uri("https://destino.precios.test/otro");
            return redireccion;
        });
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var respuestas = await PreguntarTrasLosIntentosAsync(fabrica, manejador, 2);

        Assert.Equal(2, manejador.SolicitudesCatalogo.Count);
        Assert.DoesNotContain(manejador.SolicitudesCatalogo, s => s.Uri.Host == "destino.precios.test");
        AfirmarCostoDeLaTabla(respuestas, fabrica);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ar")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_181ar_Cada_get_al_catalogo_va_sin_authorization_cookies_query_ni_cuerpo_y_sin_la_llave()
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, "0.0000003", "0.0000025"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        await PreguntarHastaAsync(fabrica, CostoDeLaTabla);

        Assert.NotEmpty(manejador.SolicitudesCatalogo);
        Assert.All(manejador.SolicitudesCatalogo, solicitud =>
        {
            Assert.DoesNotContain(solicitud.Encabezados.Keys, k => k.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || k.Equals("Cookie", StringComparison.OrdinalIgnoreCase) || k.Equals("x-api-key", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(solicitud.Encabezados.Values, v => v.Contains(LlaveGemini, StringComparison.Ordinal));
            Assert.Equal(string.Empty, solicitud.Uri.Query);
            Assert.DoesNotContain(LlaveGemini, solicitud.Uri.ToString(), StringComparison.Ordinal);
            Assert.Empty(solicitud.Cuerpo);
        });
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(LlaveGemini, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 ar")]
    [Trait("SEG", "SEG-11")]
    public void ESC_181ar_Una_url_file_del_catalogo_impide_arrancar_con_la_linea_de_configuracion_invalida()
    {
        var configuracion = Tabla();
        configuracion[ClaveUrl] = "file:///etc/passwd";
        using var fabrica = Fabrica(ConCatalogo("{}"), configuracion, EntornoGemini());

        var resultado = ResultadoArranque.De(fabrica);

        Assert.False(resultado.Arranco);
        Assert.Contains(Invalida(ClaveUrl, "file:///etc/passwd", "URL absoluta http o https, o vacía"), resultado.LineasDelError);
        Assert.DoesNotContain(LlaveGemini, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-07")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181 as")]
    public async Task ESC_181as_Con_el_precio_resuelto_una_pregunta_sobre_un_tercero_sigue_dando_403_sin_costo_ni_llamadas_al_llm()
    {
        var manejador = ConCatalogo(Json(IdEnCatalogo, "0.0000003", "0.0000025"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        await PreguntarHastaAsync(fabrica, CostoDeLaTabla);
        var llamadasAntes = manejador.PeticionesLlm.Count;

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?");

        Assert.Equal(403, respuesta.Estado);
        Assert.Equal(0, respuesta.Tokens);
        Assert.Equal(0m, respuesta.CostoUsd);
        Assert.Equal(llamadasAntes, manejador.PeticionesLlm.Count);
        var evento = fabrica.Auditoria.Eventos[^1];
        Assert.Equal(ResultadoAuditoria.Denegada, evento.Resultado);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, evento.MotivoDenegacion);
    }
}
