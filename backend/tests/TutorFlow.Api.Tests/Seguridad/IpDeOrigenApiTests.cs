namespace TutorFlow.Api.Tests.Seguridad;

public sealed class IpDeOrigenApiTests
{
    private const string Pregunta = "¿Cuál es la nota mínima para aprobar?";
    private const string Proxy = "10.0.0.5";
    private const string IpDelCliente = "203.0.113.7";
    private const int LimitePorMinuto = 60;

    private static Dictionary<string, string?> ConProxy(params string[] ips) =>
        ips.Select((ip, i) => ($"Proxy:IpsConfiables:{i}", (string?)ip)).ToDictionary();

    private static Dictionary<string, string> Xff(string valor) => new() { ["X-Forwarded-For"] = valor };

    private static string IpAuditada(FabricaApi fabrica) => fabrica.Auditoria.Eventos[^1].IpOrigen;

    private static async Task<RespuestaApi> Preguntar(FabricaApi fabrica, string conexion, Dictionary<string, string>? encabezados = null) =>
        await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, conexion, encabezados);

    private static async Task<RespuestaApi> PreguntarHastaElLimite(FabricaApi fabrica, string conexion, Dictionary<string, string> encabezados)
    {
        RespuestaApi ultima = null!;
        for (var i = 0; i <= LimitePorMinuto; i++)
        {
            ultima = await Preguntar(fabrica, conexion, encabezados);
        }

        return ultima;
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-047")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_047_Sin_proxies_configurados_x_forwarded_for_se_ignora()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await Preguntar(fabrica, Proxy, Xff(IpDelCliente));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Proxy, IpAuditada(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-047")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_047_Sin_proxies_la_peticion_cuenta_para_el_limite_de_la_conexion()
    {
        using var fabrica = new FabricaApi();
        RespuestaApi ultima = null!;

        for (var i = 1; i <= LimitePorMinuto + 1; i++)
        {
            ultima = await Preguntar(fabrica, Proxy, Xff($"203.0.113.{i}"));
        }

        Assert.Equal(429, ultima.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048a_Desde_un_proxy_de_confianza_la_ip_de_origen_es_la_de_x_forwarded_for()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var respuesta = await Preguntar(fabrica, Proxy, Xff(IpDelCliente));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(IpDelCliente, IpAuditada(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048a_La_peticion_cuenta_para_el_limite_del_cliente_y_no_para_el_del_proxy()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var ultima = await PreguntarHastaElLimite(fabrica, Proxy, Xff(IpDelCliente));
        var sinCabecera = await Preguntar(fabrica, Proxy);

        Assert.Equal(429, ultima.Estado);
        Assert.Equal(200, sinCabecera.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048b_Con_una_cadena_solo_cuenta_el_ultimo_valor()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var respuesta = await Preguntar(fabrica, Proxy, Xff($"198.51.100.1, {IpDelCliente}"));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(IpDelCliente, IpAuditada(fabrica));
    }

    [Theory]
    [InlineData("::ffff:10.0.0.5")]
    [InlineData("10.0.0.5")]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048c_La_conexion_en_formato_ipv4_mapeado_se_compara_como_ipv4(string conexion)
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var respuesta = await Preguntar(fabrica, conexion, Xff(IpDelCliente));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(IpDelCliente, IpAuditada(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048d_Sin_x_forwarded_for_se_conserva_la_ip_de_la_conexion()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var respuesta = await Preguntar(fabrica, Proxy);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Proxy, IpAuditada(fabrica));
    }

    [Theory]
    [InlineData("EST-002")]
    [InlineData("203.0.113.7, abc")]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048e_Un_ultimo_valor_que_no_es_ip_conserva_la_ip_de_la_conexion(string valor)
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var respuesta = await Preguntar(fabrica, Proxy, Xff(valor));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Proxy, IpAuditada(fabrica));
        Assert.DoesNotContain(fabrica.Auditoria.Eventos, e => e.IpOrigen.Contains("EST-002", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048e_Forwarded_x_real_ip_y_x_forwarded_host_se_ignoran_siempre()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));
        var encabezados = new Dictionary<string, string>
        {
            ["Forwarded"] = "for=203.0.113.9",
            ["X-Real-IP"] = "203.0.113.9",
            ["X-Forwarded-Host"] = "evil.example",
            ["X-Forwarded-Proto"] = "https"
        };

        var respuesta = await Preguntar(fabrica, Proxy, encabezados);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Proxy, IpAuditada(fabrica));
        Assert.DoesNotContain(fabrica.Auditoria.Eventos, e => e.IpOrigen.Contains("203.0.113.9", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-048")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_048e_Forwarded_y_x_real_ip_se_ignoran_tambien_sin_proxies_configurados()
    {
        using var fabrica = new FabricaApi();
        var encabezados = new Dictionary<string, string>
        {
            ["Forwarded"] = "for=203.0.113.9",
            ["X-Real-IP"] = "203.0.113.9",
            ["X-Forwarded-For"] = "203.0.113.9"
        };

        await Preguntar(fabrica, Proxy, encabezados);

        Assert.Equal(Proxy, IpAuditada(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-049")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_049_Una_conexion_fuera_de_la_lista_conserva_su_ip_y_su_propio_limite()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var ultima = await PreguntarHastaElLimite(fabrica, "10.0.0.6", Xff(IpDelCliente));
        var desdeElProxy = await Preguntar(fabrica, Proxy, Xff(IpDelCliente));

        Assert.Equal(429, ultima.Estado);
        Assert.Equal(200, desdeElProxy.Estado);
        Assert.All(fabrica.Auditoria.Eventos.Take(LimitePorMinuto + 1), e => Assert.Equal("10.0.0.6", e.IpOrigen));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("::1", false)]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-067")]
    [Trait("SEG", "SEG-19")]
    public async Task ESC_067_El_loopback_nunca_se_confia_implicitamente(string conexion, bool conProxyConfigurado)
    {
        var configuracion = conProxyConfigurado ? ConProxy(Proxy) : null;
        using var fabrica = new FabricaApi(configuracion: configuracion);

        var respuesta = await Preguntar(fabrica, conexion, Xff(IpDelCliente));

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(conexion, IpAuditada(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-068")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_068_Tras_el_proxy_cada_cliente_tiene_su_propio_limite()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));

        var ultima = await PreguntarHastaElLimite(fabrica, Proxy, Xff(IpDelCliente));
        var otroCliente = await Preguntar(fabrica, Proxy, Xff("203.0.113.8"));

        Assert.Equal(429, ultima.Estado);
        Assert.NotNull(ultima.Encabezado("Retry-After"));
        Assert.Equal(200, otroCliente.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-069")]
    [Trait("SEG", "SEG-16")]
    public async Task ESC_069_Las_identidades_se_cuentan_por_la_ip_del_cliente_tras_el_proxy()
    {
        using var fabrica = new FabricaApi(configuracion: ConProxy(Proxy));
        string[] ids = ["EST-001", "EST-002", "EST-003", "EST-004"];
        var respuestas = new List<RespuestaApi>();

        foreach (var id in ids)
        {
            respuestas.Add(await fabrica.PreguntarAsync("estudiante", id, "¿Cuál es mi promedio?", Proxy, Xff(IpDelCliente)));
            fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(1));
        }

        fabrica.Reloj.Avanzar(TimeSpan.FromMinutes(-1));
        respuestas.Add(await fabrica.PreguntarAsync("estudiante", "EST-005", "¿Cuál es mi promedio?", Proxy, Xff("203.0.113.8")));

        Assert.All(respuestas, r => Assert.Equal(200, r.Estado));
        var evento = Assert.Single(fabrica.Auditoria.MultiplesIdentidades);
        Assert.Equal(IpDelCliente, evento.IpOrigen);
        Assert.Equal(ids, evento.UsuarioIds);
        Assert.DoesNotContain(fabrica.Auditoria.MultiplesIdentidades, e => e.IpOrigen is Proxy or "203.0.113.8");
    }
}
