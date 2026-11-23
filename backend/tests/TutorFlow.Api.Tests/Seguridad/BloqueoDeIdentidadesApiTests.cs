using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class BloqueoDeIdentidadesApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string Conexion = "10.0.0.5";
    private const string IpDelCliente = "203.0.113.7";

    private static readonly Dictionary<string, string?> Bloqueo = new() { ["Limites:BloquearMultiplesIdentidades"] = "true" };

    private sealed class Sesion(FabricaApi fabrica)
    {
        private readonly DateTimeOffset _origen = fabrica.Reloj.AhoraUtc;

        public FabricaApi Fabrica { get; } = fabrica;

        public async Task<RespuestaApi> PreguntarEn(double minutos, string id, string conexion = Conexion, Dictionary<string, string>? encabezados = null)
        {
            Fabrica.Reloj.Fijar(_origen + TimeSpan.FromMinutes(minutos));
            return await Fabrica.PreguntarAsync("estudiante", id, Pregunta, conexion, encabezados);
        }

        public async Task RegistrarTresIdentidades()
        {
            foreach (var (id, minuto) in new[] { ("EST-001", 0), ("EST-002", 1), ("EST-003", 2) })
            {
                Assert.Equal(200, (await PreguntarEn(minuto, id)).Estado);
            }
        }
    }

    private static bool? Bloqueada(EventoMultiplesIdentidades evento) =>
        (bool?)evento.GetType().GetProperty("Bloqueada")?.GetValue(evento);

    private static void AfirmarRetryAfter(RespuestaApi respuesta, int segundos)
    {
        Afirmaciones.ProblemDetails(respuesta, 429);
        Assert.Equal(segundos.ToString(), respuesta.Encabezado("Retry-After"));
    }

    public static TheoryData<bool, string, string, string, string, int, int, string, int> FilasDeCa13 => new()
    {
        { false, "", "10.0.0.5", "203.0.113.7", "EST-004", 200, 1, "10.0.0.5", 0 },
        { true, "", "10.0.0.5", "203.0.113.7", "EST-004", 429, 1, "10.0.0.5", 420 },
        { true, "", "10.0.0.5", "203.0.113.7", "EST-001", 200, 0, "10.0.0.5", 0 },
        { true, "10.0.0.5", "10.0.0.5", "198.51.100.1, 203.0.113.7", "EST-004", 429, 1, "203.0.113.7", 420 }
    };

    [Theory]
    [MemberData(nameof(FilasDeCa13))]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    [Trait("SEG", "SEG-19")]
    public async Task CA_13_Suplantacion_por_ip_de_origen(bool bloqueo, string proxies, string conexion, string xff, string cuarta, int http, int eventos, string ip, int retryAfter)
    {
        var configuracion = new Dictionary<string, string?> { ["Limites:BloquearMultiplesIdentidades"] = bloqueo ? "true" : "false" };
        if (proxies.Length > 0)
        {
            configuracion["Proxy:IpsConfiables:0"] = proxies;
        }

        using var fabrica = new FabricaApi(configuracion: configuracion);
        var sesion = new Sesion(fabrica);
        var encabezados = new Dictionary<string, string> { ["X-Forwarded-For"] = xff };

        foreach (var (id, minuto) in new[] { ("EST-001", 0), ("EST-002", 1), ("EST-003", 2) })
        {
            await sesion.PreguntarEn(minuto, id, conexion, encabezados);
        }

        var respuesta = await sesion.PreguntarEn(3, cuarta, conexion, encabezados);

        Assert.Equal(http, respuesta.Estado);
        if (retryAfter > 0)
        {
            AfirmarRetryAfter(respuesta, retryAfter);
        }

        Assert.Equal(eventos, fabrica.Auditoria.MultiplesIdentidades.Count);
        Assert.Equal(4, fabrica.Auditoria.Eventos.Count);
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.Equal(ip, e.IpOrigen));
        Assert.All(fabrica.Auditoria.MultiplesIdentidades, e => Assert.Equal(ip, e.IpOrigen));
        Assert.All(fabrica.Auditoria.MultiplesIdentidades, e => Assert.Equal(retryAfter > 0, Bloqueada(e)));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-140")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_140_Sin_bloqueo_el_evento_lleva_bloqueada_false()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        var cuarta = await sesion.PreguntarEn(3, "EST-004");

        Assert.Equal(200, cuarta.Estado);
        var evento = Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
        Assert.Equal(false, Bloqueada(evento));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141a_La_identidad_nueva_recibe_429_con_retry_after_420_y_evento_bloqueada_true()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);

        var cuarta = await sesion.PreguntarEn(3, "EST-004");

        AfirmarRetryAfter(cuarta, 420);
        var evento = Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004"], evento.UsuarioIds);
        Assert.Equal(true, Bloqueada(evento));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141a_El_429_no_invoca_al_llm_ni_lee_notas_o_asistencia()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();
        var llamadasLlm = fabrica.Llm.Llamadas;
        var llamadasApi = fabrica.Api.Llamadas.Count;

        await sesion.PreguntarEn(3, "EST-004");

        Assert.Equal(llamadasLlm, fabrica.Llm.Llamadas);
        var nuevas = fabrica.Api.Llamadas.Skip(llamadasApi).ToList();
        Assert.DoesNotContain(nuevas, l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal) || l.StartsWith("ObtenerAsistencia", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141a_Una_identidad_ya_registrada_se_atiende_con_llm_aunque_haya_otra_bloqueada()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();
        await sesion.PreguntarEn(3, "EST-004");
        var llamadasLlm = fabrica.Llm.Llamadas;

        var registrada = await sesion.PreguntarEn(4, "EST-001");

        Assert.Equal(200, registrada.Estado);
        Assert.Equal(llamadasLlm + 1, fabrica.Llm.Llamadas);
        Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_141b_El_bloqueo_es_por_ip_de_origen()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        var otraIp = await sesion.PreguntarEn(3, "EST-004", "10.0.0.6");

        Assert.Equal(200, otraIp.Estado);
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-03")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141c_Una_identidad_inexistente_es_403_identidad_invalida_y_no_429()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        var inexistente = await sesion.PreguntarEn(3, "EST-999");

        Afirmaciones.Negativa(inexistente, "estudiante");
        Assert.Equal(MotivoDenegacion.IdentidadInvalida, fabrica.Auditoria.Eventos[^1].MotivoDenegacion);
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_141c_Una_identidad_inexistente_no_ocupa_un_lugar_de_la_ventana()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.PreguntarEn(0, "EST-001");
        await sesion.PreguntarEn(1, "EST-002");
        await sesion.PreguntarEn(2, "EST-999");

        var tercera = await sesion.PreguntarEn(3, "EST-003");

        Assert.Equal(200, tercera.Estado);
        Assert.Empty(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-097")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_097_Sin_bloqueo_se_emite_un_evento_por_identidad_nueva_y_no_por_repeticion()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        var respuestas = new List<RespuestaApi>();

        foreach (var (id, minuto) in new[] { ("EST-001", 0), ("EST-002", 1), ("EST-003", 2), ("EST-004", 3), ("EST-005", 4), ("EST-004", 5) })
        {
            respuestas.Add(await sesion.PreguntarEn(minuto, id));
        }

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        var eventos = fabrica.Auditoria.MultiplesIdentidades;
        Assert.Equal(2, eventos.Count);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004"], eventos[0].UsuarioIds);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004", "EST-005"], eventos[1].UsuarioIds);
        Assert.All(eventos, e => Assert.Equal(false, Bloqueada(e)));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_098a_El_retry_after_baja_con_el_tiempo_y_el_minuto_10_libera_la_identidad_nueva()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        AfirmarRetryAfter(await sesion.PreguntarEn(3, "EST-004"), 420);
        AfirmarRetryAfter(await sesion.PreguntarEn(9, "EST-004"), 60);
        AfirmarRetryAfter(await sesion.PreguntarEn(599.5 / 60, "EST-004"), 1);
        var enElMinuto10 = await sesion.PreguntarEn(10, "EST-004");

        Assert.Equal(200, enElMinuto10.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_098b_El_reintento_de_una_identidad_ya_reportada_no_emite_otro_evento()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        await sesion.PreguntarEn(3, "EST-004");
        await sesion.PreguntarEn(9, "EST-004");
        await sesion.PreguntarEn(599.5 / 60, "EST-004");

        Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_098c_Tras_el_minuto_10_la_siguiente_identidad_nueva_recibe_60_y_un_evento_nuevo()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();
        await sesion.PreguntarEn(3, "EST-004");
        await sesion.PreguntarEn(10, "EST-004");

        var primera = await sesion.PreguntarEn(10, "EST-001");

        AfirmarRetryAfter(primera, 60);
        Assert.Equal(2, fabrica.Auditoria.MultiplesIdentidades.Count);
        Assert.Equal(["EST-002", "EST-003", "EST-004", "EST-001"], fabrica.Auditoria.MultiplesIdentidades[1].UsuarioIds);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-099")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_099a_Una_identidad_registrada_renueva_su_hora_y_cambia_el_retry_after()
    {
        using var fabrica = new FabricaApi(configuracion: Bloqueo);
        var sesion = new Sesion(fabrica);
        await sesion.RegistrarTresIdentidades();

        var renovada = await sesion.PreguntarEn(8, "EST-001");
        var nueva = await sesion.PreguntarEn(9, "EST-004");

        Assert.Equal(200, renovada.Estado);
        AfirmarRetryAfter(nueva, 120);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-099")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_099b_Con_umbral_1_solo_la_identidad_nueva_recibe_429()
    {
        var configuracion = new Dictionary<string, string?>(Bloqueo) { ["Limites:MaximoIdentidadesPorIp"] = "1" };
        using var fabrica = new FabricaApi(configuracion: configuracion);
        var sesion = new Sesion(fabrica);

        var primera = await sesion.PreguntarEn(0, "EST-001");
        var nueva = await sesion.PreguntarEn(1, "EST-002");
        var registrada = await sesion.PreguntarEn(1, "EST-001");

        Assert.Equal(200, primera.Estado);
        AfirmarRetryAfter(nueva, 540);
        Assert.Equal(200, registrada.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    [Trait("SEG", "SEG-19")]
    public async Task El_bloqueo_se_aplica_con_el_cliente_real_tras_el_proxy()
    {
        var configuracion = new Dictionary<string, string?>(Bloqueo) { ["Proxy:IpsConfiables:0"] = Conexion };
        using var fabrica = new FabricaApi(configuracion: configuracion);
        var sesion = new Sesion(fabrica);
        var encabezados = new Dictionary<string, string> { ["X-Forwarded-For"] = IpDelCliente };
        await sesion.PreguntarEn(0, "EST-001", Conexion, encabezados);
        await sesion.PreguntarEn(1, "EST-002", Conexion, encabezados);
        await sesion.PreguntarEn(2, "EST-003", Conexion, encabezados);

        var sinCabecera = await sesion.PreguntarEn(3, "EST-004", Conexion);
        var conCabecera = await sesion.PreguntarEn(3, "EST-004", Conexion, encabezados);

        Assert.Equal(200, sinCabecera.Estado);
        AfirmarRetryAfter(conCabecera, 420);
    }
}
