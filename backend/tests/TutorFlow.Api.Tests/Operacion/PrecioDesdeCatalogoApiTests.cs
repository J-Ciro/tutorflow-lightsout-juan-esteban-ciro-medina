using System.Net;
using System.Text;
using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class PrecioDesdeCatalogoApiTests
{
    private const string Modelo = "gemini-3.5-flash-lite";
    private const string IdEnCatalogo = "google/gemini-3.5-flash-lite";
    private const string LlaveGemini = "AIzaLLAVEABC123";
    private const string ModeloSinPrecio = "modelo-sin-precio";
    private const string LlaveOpenAi = "sk-LLAVE-ABC123-openai";
    private const string ClaveUrl = "Llm:CatalogoPrecios:Url";
    private const string UrlDePrueba = "https://catalogo.precios.test/api/v1/models";
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const int Entrada = 1000;
    private const int Salida = 200;
    private const decimal CostoDeLaTabla = 0.0008m;
    private const decimal CostoDelCatalogo = 0.0018m;
    private const decimal CostoEnLosLimites = 0.2m;
    private const int BytesMaximos = 10 * 1024 * 1024;
    private const int BytesDeSobra = 1024;
    private const string Advertencia = "Configuración ignorada: no se encontró el precio del modelo modelo-sin-precio en el catálogo público ni en appsettings; costoUsd será 0.";
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(20);

    public static TheoryData<string, string> PreciosInvalidosEnCatalogo => new()
    {
        { "0.01", "0.01" },
        { "0.0000003", "0.0011" },
        { "-0.0000003", "0.0000025" },
        { "abc", "0.0000025" },
        { "0.0000003", "" },
        { "0,0000003", "0.0000025" }
    };

    public static TheoryData<string, string?, string?, string?> VariablesDePrecioEliminadas => new()
    {
        { "env-validas", "999", "999", null },
        { "env-invalidas", "abc", "-1", null },
        { "env-vacias", "", "", null },
        { "config-validas", null, null, "999" },
        { "config-invalidas", null, null, "-1" }
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

    private static ManejadorConCatalogo ConCatalogo(string json) =>
        new(_ => Task.FromResult(ManejadorConCatalogo.Catalogo(json)), Entrada, Salida);

    private static ManejadorConCatalogo ConCatalogoCaido() =>
        new(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)), Entrada, Salida);

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
            return costo == esperado;
        });
        return costo;
    }

    private static async Task<IReadOnlyList<decimal>> PreguntarTresVecesAsync(FabricaApi fabrica)
    {
        var costos = new List<decimal>();
        for (var i = 0; i < 3; i++)
        {
            costos.Add((await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta)).CostoUsd);
        }

        return costos;
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181aj")]
    public async Task ESC_181aj_El_precio_del_catalogo_publico_por_defecto_da_un_costo_de_0_0008_con_1000_de_entrada_y_200_de_salida()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo(IdEnCatalogo, "0.0000003", "0.0000025"));
        var configuracion = Tabla();
        configuracion[ClaveUrl] = null;
        using var fabrica = Fabrica(manejador, configuracion, EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, CostoDeLaTabla);

        Assert.Equal(CostoDeLaTabla, costo);
        var solicitud = Assert.Single(manejador.SolicitudesCatalogo);
        Assert.Equal("https://openrouter.ai/api/v1/models", solicitud.Uri.ToString());
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181aj")]
    public async Task ESC_181aj_El_catalogo_publico_manda_sobre_la_tabla_de_appsettings_cuando_los_precios_difieren()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo(IdEnCatalogo, "0.000001", "0.000004"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, CostoDelCatalogo);

        Assert.Equal(CostoDelCatalogo, costo);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181ak")]
    public async Task ESC_181ak_Un_modelo_con_el_prefijo_de_otro_proveedor_se_encuentra_por_la_unica_entrada_que_termina_en_barra_modelo()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo("otro-proveedor/gemini-3.5-flash-lite", "0.000001", "0.000004"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, CostoDelCatalogo);

        Assert.Equal(CostoDelCatalogo, costo);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181ak")]
    public async Task ESC_181ak_Dos_entradas_que_terminan_en_barra_modelo_no_se_adivinan_y_se_usa_la_tabla()
    {
        var json = """{"data":[{"id":"a/gemini-3.5-flash-lite","pricing":{"prompt":"0.000001","completion":"0.000004"}},{"id":"b/gemini-3.5-flash-lite","pricing":{"prompt":"0.000001","completion":"0.000004"}}]}""";
        var manejador = ConCatalogo(json);
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;
        await EsperarAsync(() => manejador.CatalogoAtendido >= 1);

        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181ak")]
    public async Task ESC_181ak_La_comparacion_por_sufijo_es_exacta_y_un_id_que_solo_contiene_el_modelo_no_cuenta()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo("google/gemini-3.5-flash-lite-preview", "0.000001", "0.000004"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;
        await EsperarAsync(() => manejador.CatalogoAtendido >= 1);

        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181al")]
    public async Task ESC_181al_El_catalogo_con_error_500_se_reintenta_una_vez_y_se_usa_la_tabla()
    {
        var manejador = ConCatalogoCaido();
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;

        var atendidas = await EsperarAsync(() => manejador.CatalogoAtendido >= 2);
        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.True(atendidas);
        Assert.Equal(2, manejador.SolicitudesCatalogo.Count);
        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181al")]
    public async Task ESC_181al_El_timeout_del_catalogo_se_reintenta_una_vez_y_se_usa_la_tabla()
    {
        var manejador = new ManejadorConCatalogo(_ => throw new TaskCanceledException("timeout", new TimeoutException()), Entrada, Salida);
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;

        var atendidas = await EsperarAsync(() => manejador.CatalogoAtendido >= 2);
        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.True(atendidas);
        Assert.Equal(2, manejador.SolicitudesCatalogo.Count);
        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181am")]
    public async Task ESC_181am_Un_modelo_ausente_del_catalogo_y_de_la_tabla_advierte_una_sola_vez_y_el_costo_es_0()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo("openai/otro-modelo", "0.000001", "0.000004"));
        var entorno = new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveOpenAi, ["LLM_MODELO"] = ModeloSinPrecio };
        using var fabrica = Fabrica(manejador, new Dictionary<string, string?> { [ClaveUrl] = UrlDePrueba }, entorno);
        _ = fabrica.Services;

        var advertida = await EsperarAsync(() => fabrica.Logs.Mensajes.Contains(Advertencia));
        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.True(advertida);
        Assert.All(costos, c => Assert.Equal(0m, c));
        Assert.Equal(1, fabrica.Logs.Mensajes.Count(m => m == Advertencia));
    }

    [Theory]
    [MemberData(nameof(PreciosInvalidosEnCatalogo))]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181an")]
    public async Task ESC_181an_Un_precio_fuera_de_rango_o_no_numerico_en_el_catalogo_se_ignora_y_se_usa_la_tabla(string prompt, string completion)
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo(IdEnCatalogo, prompt, completion));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;
        await EsperarAsync(() => manejador.CatalogoAtendido >= 1);

        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181an")]
    public async Task ESC_181an_Los_precios_en_los_limites_0_y_0_001_por_token_son_validos()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo(IdEnCatalogo, "0", "0.001"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());

        var costo = await PreguntarHastaAsync(fabrica, CostoEnLosLimites);

        Assert.Equal(CostoEnLosLimites, costo);
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181ao")]
    public async Task ESC_181ao_Ollama_da_costo_0_sin_descargar_el_catalogo_ni_advertir()
    {
        var manejador = new ManejadorConCatalogo(_ => Task.FromResult(ManejadorConCatalogo.Catalogo("{}")), null, Salida);
        var entorno = new Dictionary<string, string?> { ["LLM_PROVEEDOR"] = "ollama" };
        using var fabrica = Fabrica(manejador, new Dictionary<string, string?> { [ClaveUrl] = UrlDePrueba }, entorno);

        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.All(costos, c => Assert.Equal(0m, c));
        Assert.Empty(manejador.SolicitudesCatalogo);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("no se encontró el precio", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181ap")]
    public async Task ESC_181ap_La_primera_respuesta_no_espera_a_un_catalogo_que_no_responde()
    {
        var sinRespuesta = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manejador = new ManejadorConCatalogo(ct => sinRespuesta.Task.WaitAsync(ct), Entrada, Salida);
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;
        var catalogoPedido = await EsperarAsync(() => manejador.SolicitudesCatalogo.Count >= 1);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta).WaitAsync(Espera);

        Assert.True(catalogoPedido);
        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(CostoDeLaTabla, respuesta.CostoUsd);
        Assert.Equal(0, manejador.CatalogoAtendido);
        sinRespuesta.TrySetCanceled();
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181aq")]
    public async Task ESC_181aq_La_descarga_del_catalogo_no_envia_la_llave_ni_datos()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo(IdEnCatalogo, "0.0000003", "0.0000025"));
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;

        await EsperarAsync(() => manejador.CatalogoAtendido >= 1);
        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        var solicitud = Assert.Single(manejador.SolicitudesCatalogo);
        Assert.DoesNotContain("Authorization", solicitud.Encabezados.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("x-api-key", solicitud.Encabezados.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(solicitud.Encabezados.Values, v => v.Contains(LlaveGemini, StringComparison.Ordinal));
        Assert.DoesNotContain(LlaveGemini, solicitud.Uri.ToString(), StringComparison.Ordinal);
        Assert.Empty(solicitud.Cuerpo);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(LlaveGemini, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(VariablesDePrecioEliminadas))]
    [Trait("CA", "CA-15")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181ar")]
    public async Task ESC_181ar_LLM_PRECIO_ENTRADA_y_LLM_PRECIO_SALIDA_ya_no_existen_y_se_ignoran_sin_fallar_el_arranque(string caso, string? entrada, string? salida, string? enConfiguracion)
    {
        Assert.NotEmpty(caso);
        var manejador = ConCatalogoCaido();
        var entorno = EntornoGemini();
        var configuracion = Tabla();
        if (entrada is not null)
        {
            entorno["LLM_PRECIO_ENTRADA"] = entrada;
            entorno["LLM_PRECIO_SALIDA"] = salida;
        }

        if (enConfiguracion is not null)
        {
            configuracion["LLM_PRECIO_ENTRADA"] = enConfiguracion;
            configuracion["LLM_PRECIO_SALIDA"] = enConfiguracion;
        }

        using var fabrica = Fabrica(manejador, configuracion, entorno);

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.Equal(200, salud.Estado);
        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("LLM_PRECIO", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181as")]
    public async Task ESC_181as_Un_catalogo_de_mas_de_10_MB_se_ignora_y_se_usa_la_tabla()
    {
        var relleno = new string('x', BytesMaximos + BytesDeSobra);
        var json = $$$"""{"data":[{"id":"{{{IdEnCatalogo}}}","pricing":{"prompt":"0.000001","completion":"0.000004"}},{"id":"{{{relleno}}}"}]}""";
        var manejador = new ManejadorConCatalogo(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") }),
            Entrada,
            Salida);
        using var fabrica = Fabrica(manejador, Tabla(), EntornoGemini());
        _ = fabrica.Services;
        await EsperarAsync(() => manejador.CatalogoAtendido >= 1);

        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("D", "D-105")]
    [Trait("ESC", "ESC-181aj")]
    public async Task ESC_181aj_Con_la_url_del_catalogo_vacia_no_hay_descarga_y_se_usa_la_tabla()
    {
        var manejador = ConCatalogo(ManejadorConCatalogo.JsonDeCatalogo(IdEnCatalogo, "0.000001", "0.000004"));
        var configuracion = Tabla();
        configuracion[ClaveUrl] = "";
        using var fabrica = Fabrica(manejador, configuracion, EntornoGemini());

        var costos = await PreguntarTresVecesAsync(fabrica);

        Assert.All(costos, c => Assert.Equal(CostoDeLaTabla, c));
        Assert.Empty(manejador.SolicitudesCatalogo);
    }
}
