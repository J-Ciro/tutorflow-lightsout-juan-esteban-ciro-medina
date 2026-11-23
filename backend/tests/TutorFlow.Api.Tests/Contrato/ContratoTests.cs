using System.Text;
using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class ContratoSaludTests
{
    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-100")]
    public async Task ESC_100_Salud_responde_200_con_el_cuerpo_exacto()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("""{"estado":"ok"}""", respuesta.Texto);
        Assert.Equal("application/json; charset=utf-8", respuesta.TipoContenido);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    public async Task ESC_110_Salud_responde_200_aunque_la_llave_tenga_prefijo_desconocido_sin_url_base()
    {
        var entorno = new Dictionary<string, string?> { ["LLM_API_KEY"] = "zzz-prefijo-desconocido" };
        using var fabrica = new FabricaApi(variablesEntorno: entorno, reemplazarLlm: false);

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("""{"estado":"ok"}""", respuesta.Texto);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-105")]
    public async Task ESC_105_Metodo_distinto_a_get_en_salud_es_405(string metodo)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(new HttpMethod(metodo), "/api/salud", null);

        Assert.Equal(405, respuesta.Estado);
    }
}

public sealed class ContratoMetodosTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-105")]
    public async Task ESC_105_Metodo_distinto_a_post_en_preguntar_es_405(string metodo)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(new HttpMethod(metodo), "/api/asistente/preguntar", null);

        Assert.Equal(405, respuesta.Estado);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Empty(fabrica.Api.Llamadas);
    }
}

public sealed class ContratoEntradaInvalidaTests
{
    private const string UsuarioValido = """{"rol":"estudiante","id":"EST-001"}""";

    public static TheoryData<string, string> CuerposInvalidos => new()
    {
        { "ESC-102 json malformado", """{"pregunta": """ },
        { "ESC-102 cuerpo vacio", "" },
        { "ESC-103 falta pregunta", $$"""{"usuario":{{UsuarioValido}}}""" },
        { "ESC-103 falta usuario", """{"pregunta":"hola"}""" },
        { "ESC-103 id nulo", """{"pregunta":"hola","usuario":{"rol":"estudiante","id":null}}""" },
        { "ESC-103 falta id", """{"pregunta":"hola","usuario":{"rol":"estudiante"}}""" },
        { "ESC-103 falta rol", """{"pregunta":"hola","usuario":{"id":"EST-001"}}""" },
        { "ESC-103 pregunta vacia", $$"""{"pregunta":"","usuario":{{UsuarioValido}}}""" },
        { "ESC-103 pregunta solo espacios", $$"""{"pregunta":"   ","usuario":{{UsuarioValido}}}""" },
        { "ESC-103 pregunta nula", $$"""{"pregunta":null,"usuario":{{UsuarioValido}}}""" },
        { "ESC-104 pregunta numero", $$"""{"pregunta":123,"usuario":{{UsuarioValido}}}""" },
        { "ESC-104 usuario texto", """{"pregunta":"hola","usuario":"EST-001"}""" },
        { "ESC-104 rol numero", """{"pregunta":"hola","usuario":{"rol":1,"id":"EST-001"}}""" },
        { "ESC-104 cuerpo arreglo", "[]" },
        { "ESC-104 cuerpo null", "null" }
    };

    [Theory]
    [MemberData(nameof(CuerposInvalidos))]
    [Trait("CA", "CA-07")]
    public async Task Cuerpo_invalido_responde_400_problem_details_sin_llamar_al_llm_ni_a_la_api(string caso, string cuerpo)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo);

        Afirmaciones.ProblemDetails(respuesta, 400);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Empty(fabrica.Api.Llamadas);
        Assert.True(respuesta.Estado == 400, caso);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("Estudiante")]
    [InlineData("ESTUDIANTE")]
    [InlineData("coordinador ")]
    [InlineData(" estudiante")]
    [InlineData("")]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-065")]
    public async Task ESC_065_Rol_fuera_de_la_lista_cerrada_es_400_sin_consultar_api_ni_llm(string rol)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarAsync(rol, "EST-001", "¿Cuál es mi promedio?");

        Afirmaciones.ProblemDetails(respuesta, 400);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Empty(fabrica.Api.Llamadas);
    }

    [Theory]
    [InlineData("EST-001/../EST-002")]
    [InlineData("EST-001%2F..%2FEST-002")]
    [InlineData("EST-001?x=1")]
    [InlineData("EST-001/notas")]
    [InlineData("est-001")]
    [InlineData("EST-1")]
    [InlineData("EST-0001")]
    [InlineData("COORD-1")]
    [InlineData(" EST-001")]
    [InlineData("")]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-082")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_082_Id_con_formato_invalido_es_400_sin_llamadas_a_la_api(string id)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarEstudianteAsync(id, "¿Cuál es mi promedio?");

        Afirmaciones.ProblemDetails(respuesta, 400);
        Assert.Empty(fabrica.Api.Llamadas);
        Assert.Equal(0, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-084")]
    public async Task ESC_084_Pregunta_de_1001_caracteres_es_400()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", new string('a', 1001));

        Afirmaciones.ProblemDetails(respuesta, 400);
        Assert.Equal(0, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-084")]
    public async Task ESC_084_Pregunta_de_exactamente_1000_caracteres_no_es_400()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", new string('a', 1000));

        Assert.Equal(200, respuesta.Estado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-084")]
    public async Task ESC_084_Cuerpo_de_17_KB_es_413_problem_details()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", CuerpoDeBytes(17 * 1024));

        Afirmaciones.ProblemDetails(respuesta, 413);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Empty(fabrica.Api.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-084")]
    public async Task ESC_084_Cuerpo_de_exactamente_16_KB_no_es_413()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", CuerpoDeBytes(16 * 1024));

        Assert.NotEqual(413, respuesta.Estado);
        Assert.Equal(200, respuesta.Estado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-107")]
    public async Task ESC_107_Campos_extra_en_usuario_se_ignoran_y_no_otorgan_permisos()
    {
        using var fabrica = new FabricaApi();
        var cuerpo = """{"pregunta":"¿Cuáles son las notas de EST-004?","usuario":{"rol":"estudiante","id":"EST-001","programas":["ADM"],"esAdmin":true}}""";

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo);

        Afirmaciones.Negativa(respuesta, "estudiante");
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-107")]
    public async Task ESC_107_Campos_extra_en_la_entrada_no_impiden_una_pregunta_valida()
    {
        using var fabrica = new FabricaApi();
        var cuerpo = """{"pregunta":"¿Cuál es la nota mínima para aprobar?","usuario":{"rol":"estudiante","id":"EST-001","esAdmin":true},"extra":1}""";

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo);

        Assert.Equal(200, respuesta.Estado);
    }

    private static string CuerpoDeBytes(int bytes)
    {
        const string Base = """{"pregunta":"hola","usuario":{"rol":"estudiante","id":"EST-001"},"relleno":"{0}"}""";
        var sinRelleno = Encoding.UTF8.GetByteCount(Base.Replace("{0}", string.Empty, StringComparison.Ordinal));
        return Base.Replace("{0}", new string('x', bytes - sinRelleno), StringComparison.Ordinal);
    }
}

public sealed class ContratoSalidaTests
{
    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-101")]
    public async Task ESC_101_Respuesta_200_cumple_la_forma_exacta_y_el_tipo_de_contenido()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal("application/json; charset=utf-8", respuesta.TipoContenido);
        Assert.Equal("Respuesta de prueba.", respuesta.Respuesta);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-101")]
    public async Task ESC_101_Respuesta_403_cumple_la_forma_exacta_y_el_tipo_de_contenido()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?");

        Afirmaciones.Negativa(respuesta, "estudiante");
        Assert.Equal("application/json; charset=utf-8", respuesta.TipoContenido);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-101")]
    public async Task ESC_101_Citas_es_lista_vacia_y_nunca_null_cuando_no_hay_citas()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto(Textos.SinInformacion));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Qué dice el reglamento sobre el uso de parqueaderos?");

        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SalidaDelContrato(respuesta);
        Assert.Equal(JsonValueKind.Array, respuesta.Json.GetProperty("citas").ValueKind);
        Assert.Empty(respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-101")]
    public async Task ESC_101_Propiedades_en_camel_case_exactamente_como_el_contrato()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        var nombres = respuesta.Json.EnumerateObject().Select(p => p.Name).Order().ToArray();
        Assert.Equal(["citas", "costoUsd", "respuesta", "tokens", "usoDatosEstudiante"], nombres);
    }
}
