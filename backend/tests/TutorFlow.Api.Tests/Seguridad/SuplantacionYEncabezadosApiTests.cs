using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class SuplantacionApiTests
{
    private const string Ip = "10.0.0.5";
    private const string Pregunta = "¿Cuál es mi promedio?";
    private static readonly string[] CuatroIds = ["EST-001", "EST-002", "EST-003", "EST-004"];

    private static async Task<List<RespuestaApi>> PreguntarConCadaId(FabricaApi fabrica, IEnumerable<string> ids)
    {
        var respuestas = new List<RespuestaApi>();
        foreach (var id in ids)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync(id, Pregunta, Ip));
            fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(1));
        }

        return respuestas;
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-140")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_140_Cuatro_identidades_desde_una_ip_responden_200_y_emiten_un_evento_multiples_identidades()
    {
        using var fabrica = new FabricaApi();

        var respuestas = await PreguntarConCadaId(fabrica, CuatroIds);

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        var evento = Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
        Assert.Equal(Ip, evento.IpOrigen);
        Assert.Equal(CuatroIds, evento.UsuarioIds.Order().ToArray());
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-140")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_140_Tres_identidades_no_emiten_el_evento()
    {
        using var fabrica = new FabricaApi();

        var respuestas = await PreguntarConCadaId(fabrica, CuatroIds.Take(3));

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public async Task Repetir_la_misma_identidad_no_emite_el_evento()
    {
        using var fabrica = new FabricaApi();

        var respuestas = await PreguntarConCadaId(fabrica, Enumerable.Repeat("EST-001", 6));

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public async Task Fuera_de_la_ventana_de_diez_minutos_no_se_acumulan_identidades()
    {
        using var fabrica = new FabricaApi();

        await PreguntarConCadaId(fabrica, CuatroIds.Take(3));
        fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(11));
        var cuarta = await fabrica.PreguntarEstudianteAsync("EST-004", Pregunta, Ip);

        Assert.Equal(200, cuarta.Estado);
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public async Task Identidades_repartidas_en_ips_distintas_no_emiten_el_evento()
    {
        using var fabrica = new FabricaApi();

        var respuestas = new List<RespuestaApi>();
        foreach (var (id, indice) in CuatroIds.Select((id, i) => (id, i)))
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync(id, Pregunta, $"10.0.1.{indice}"));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }
}

public sealed class EncabezadosDeSeguridadApiTests
{
    private const string Pregunta = "¿Cuál es la nota mínima para aprobar?";

    private static void AfirmarEncabezados(RespuestaApi respuesta)
    {
        Assert.Equal("nosniff", respuesta.Encabezado("X-Content-Type-Options"));
        Assert.Equal("DENY", respuesta.Encabezado("X-Frame-Options"));
        Assert.Equal("no-referrer", respuesta.Encabezado("Referrer-Policy"));
        Afirmaciones.SinEncabezadoCorsComodin(respuesta);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-111")]
    [Trait("SEG", "SEG-14")]
    public async Task ESC_111_Una_respuesta_200_incluye_los_encabezados_de_seguridad()
    {
        using var fabrica = new FabricaApi();

        AfirmarEncabezados(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-111")]
    [Trait("SEG", "SEG-14")]
    public async Task ESC_111_Una_respuesta_403_incluye_los_encabezados_de_seguridad()
    {
        using var fabrica = new FabricaApi();

        AfirmarEncabezados(await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?"));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-111")]
    [Trait("SEG", "SEG-14")]
    public async Task ESC_111_Una_respuesta_400_incluye_los_encabezados_de_seguridad()
    {
        using var fabrica = new FabricaApi();

        AfirmarEncabezados(await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", "{"));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-111")]
    [Trait("SEG", "SEG-14")]
    public async Task ESC_111_La_salud_y_el_405_incluyen_los_encabezados_de_seguridad()
    {
        using var fabrica = new FabricaApi();

        AfirmarEncabezados(await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null));
        AfirmarEncabezados(await fabrica.EnviarAsync(HttpMethod.Get, "/api/asistente/preguntar", null));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-111")]
    [Trait("SEG", "SEG-14")]
    public async Task ESC_111_Una_ruta_inexistente_404_incluye_los_encabezados_de_seguridad()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarAsync(HttpMethod.Get, "/no-existe", null);

        Assert.Equal(404, respuesta.Estado);
        AfirmarEncabezados(respuesta);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-111")]
    [Trait("SEG", "SEG-14")]
    public async Task Un_origen_ajeno_nunca_recibe_access_control_allow_origin_comodin()
    {
        using var fabrica = new FabricaApi();
        var encabezados = new Dictionary<string, string> { ["Origin"] = "https://evil.example" };

        var respuesta = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, FabricaApi.IpPorDefecto, encabezados);
        var preflight = await fabrica.EnviarAsync(HttpMethod.Options, "/api/asistente/preguntar", null, FabricaApi.IpPorDefecto, new Dictionary<string, string>
        {
            ["Origin"] = "https://evil.example",
            ["Access-Control-Request-Method"] = "POST"
        });

        Assert.Equal(200, respuesta.Estado);
        Afirmaciones.SinEncabezadoCorsComodin(respuesta);
        Afirmaciones.SinEncabezadoCorsComodin(preflight);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-14")]
    public async Task Swagger_no_se_expone_fuera_de_development()
    {
        using var fabrica = new FabricaApi();

        var swagger = await fabrica.EnviarAsync(HttpMethod.Get, "/swagger/index.html", null);
        var documento = await fabrica.EnviarAsync(HttpMethod.Get, "/swagger/v1/swagger.json", null);

        Assert.Equal(404, swagger.Estado);
        Assert.Equal(404, documento.Estado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-12")]
    public async Task Los_errores_no_exponen_stack_trace_ni_rutas()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(new HttpRequestException("fallo interno")));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(503, respuesta.Estado);
        Afirmaciones.SinDetallesInternos(respuesta);
    }
}
