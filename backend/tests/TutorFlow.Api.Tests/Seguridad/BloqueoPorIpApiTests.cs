namespace TutorFlow.Api.Tests.Seguridad;

public sealed class BloqueoPorIpApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string Conexion = "10.0.0.5";
    private const string OtraConexion = "10.0.0.6";
    private const int Limite = 600;
    private const string ReintentoDelPrimerBloqueo = "30";

    public static TheoryData<string, string> CuerposDeLaTandaQueSeExcede => new()
    {
        { "cuerpo-vacio", "{}" },
        { "id-inexistente", "EST-999" },
        { "tipo-de-contenido", "text/plain" }
    };

    public static TheoryData<double, string> ReinicioDeLaEscalada => new()
    {
        { 929, "120" },
        { 930, "30" }
    };

    public static TheoryData<double, int> LimiteExactoDe60Segundos => new()
    {
        { 59.9, 429 },
        { 60, 200 }
    };

    private sealed class Sesion(FabricaApi fabrica)
    {
        private readonly DateTimeOffset _origen = fabrica.Reloj.AhoraUtc;

        public FabricaApi Fabrica { get; } = fabrica;

        public void EnSegundo(double segundos) => Fabrica.Reloj.Fijar(_origen + TimeSpan.FromSeconds(segundos));

        public async Task<RespuestaApi> Preguntar(double segundos, string ip = Conexion, IReadOnlyDictionary<string, string>? encabezados = null)
        {
            EnSegundo(segundos);
            return await Fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, ip, encabezados);
        }

        public async Task<List<RespuestaApi>> Tanda(double segundos, int preguntas = Limite + 1, string ip = Conexion)
        {
            var respuestas = new List<RespuestaApi>();
            for (var i = 0; i < preguntas; i++)
            {
                var encabezados = new Dictionary<string, string> { ["X-Forwarded-For"] = $"203.0.113.{i % 250}" };
                respuestas.Add(await Preguntar(segundos, ip, encabezados));
            }

            return respuestas;
        }
    }

    private static void AfirmarTanda(List<RespuestaApi> tanda, string retryAfter)
    {
        Assert.All(tanda.Take(Limite), r => Assert.Equal(200, r.Estado));
        AfirmarBloqueo(tanda[Limite], retryAfter);
    }

    private static void AfirmarBloqueo(RespuestaApi respuesta, string retryAfter)
    {
        Afirmaciones.ProblemDetails(respuesta, 429);
        Assert.Equal(retryAfter, respuesta.Encabezado("Retry-After"));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142a_La_pregunta_601_da_429_con_30_s_y_sin_llamadas_al_llm_ni_a_la_api_academica()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        var tanda = await sesion.Tanda(0, Limite);
        var llamadasLlm = fabrica.Llm.Llamadas;
        var llamadasApi = fabrica.Api.Llamadas.Count;

        var bloqueada = await sesion.Preguntar(0);

        Assert.All(tanda, r => Assert.Equal(200, r.Estado));
        AfirmarBloqueo(bloqueada, ReintentoDelPrimerBloqueo);
        Assert.Equal(llamadasLlm, fabrica.Llm.Llamadas);
        Assert.Equal(llamadasApi, fabrica.Api.Llamadas.Count);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142b_Durante_el_bloqueo_el_429_informa_los_segundos_restantes_sin_alargarlo_y_la_salud_responde_200()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.Tanda(0);

        var enElSegundo10 = await sesion.Preguntar(10);
        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null, Conexion);
        var enElSegundo29y5 = await sesion.Preguntar(29.5);
        var enElSegundo30 = await sesion.Preguntar(30);

        AfirmarBloqueo(enElSegundo10, "20");
        Assert.Equal(200, salud.Estado);
        AfirmarBloqueo(enElSegundo29y5, "1");
        Assert.Equal(200, enElSegundo30.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142b_Al_terminar_el_bloqueo_la_ventana_empieza_vacia_y_las_bloqueadas_no_contaron()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.Tanda(0);
        await sesion.Preguntar(10);
        await sesion.Preguntar(29.5);

        var tanda = await sesion.Tanda(30, Limite + 1);

        AfirmarTanda(tanda, "60");
    }

    [Theory]
    [MemberData(nameof(CuerposDeLaTandaQueSeExcede))]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142c_El_429_llega_antes_de_validar_la_entrada_y_no_revela_si_un_id_existe(string caso, string variante)
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.Tanda(0, Limite);
        var llamadasApi = fabrica.Api.Llamadas.Count;

        var bloqueada = caso switch
        {
            "cuerpo-vacio" => await fabrica.EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", variante, Conexion),
            "id-inexistente" => await fabrica.PreguntarAsync("estudiante", variante, Pregunta, Conexion),
            _ => await fabrica.EnviarConTipoAsync("{\"pregunta\":\"hola\",\"usuario\":{\"rol\":\"estudiante\",\"id\":\"EST-001\"}}", variante, Conexion)
        };

        AfirmarBloqueo(bloqueada, ReintentoDelPrimerBloqueo);
        Assert.Equal(llamadasApi, fabrica.Api.Llamadas.Count);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142d_Las_tandas_en_0_30_90_y_210_escalan_el_bloqueo_a_30_60_120_y_120()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);

        var tandas = new[]
        {
            (Segundo: 0, Retry: "30"),
            (Segundo: 30, Retry: "60"),
            (Segundo: 90, Retry: "120"),
            (Segundo: 210, Retry: "120")
        };

        foreach (var (segundo, retry) in tandas)
        {
            AfirmarTanda(await sesion.Tanda(segundo), retry);
        }
    }

    [Theory]
    [MemberData(nameof(ReinicioDeLaEscalada))]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142e_La_escalada_se_reinicia_a_los_600_s_desde_el_fin_del_ultimo_bloqueo(double segundo, string retryAfter)
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        foreach (var inicio in new[] { 0, 30, 90, 210 })
        {
            await sesion.Tanda(inicio);
        }

        var tanda = await sesion.Tanda(segundo);

        AfirmarTanda(tanda, retryAfter);
    }

    [Theory]
    [MemberData(nameof(LimiteExactoDe60Segundos))]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142f_Una_marca_de_hace_60_s_exactos_ya_no_cuenta_y_una_de_59_9_s_si(double segundo, int esperado)
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.Tanda(0, Limite);

        var respuesta = await sesion.Preguntar(segundo);

        Assert.Equal(esperado, respuesta.Estado);
        if (esperado == 429)
        {
            AfirmarBloqueo(respuesta, ReintentoDelPrimerBloqueo);
        }
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_142_Un_bloqueo_no_afecta_a_otra_ip()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.Tanda(0);

        var otraIp = await sesion.Preguntar(1, OtraConexion);

        Assert.Equal(200, otraIp.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-047")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_047_Las_cabeceras_de_ip_se_ignoran_y_la_ip_auditada_es_la_de_la_conexion()
    {
        using var fabrica = new FabricaApi();
        var conXff = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" };
        var conForwarded = new Dictionary<string, string> { ["Forwarded"] = "for=203.0.113.9", ["X-Real-IP"] = "203.0.113.9", ["X-Forwarded-Host"] = "evil.example" };

        var primera = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, Conexion, conXff);
        var segunda = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, Conexion, conForwarded);
        var tercera = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, "::ffff:10.0.0.5");

        Assert.All(new[] { primera, segunda, tercera }, r => Assert.Equal(200, r.Estado));
        Assert.Equal(3, fabrica.Auditoria.Eventos.Count);
        Assert.All(fabrica.Auditoria.Eventos, e => Assert.Equal(Conexion, e.IpOrigen));
        Assert.DoesNotContain(fabrica.Auditoria.Eventos, e => e.IpOrigen.Contains("203.0.113", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-047")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_047_La_conexion_ipv4_mapeada_comparte_el_limite_de_la_ipv4()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        var mitad = Limite / 2;
        await sesion.Tanda(0, mitad, "::ffff:10.0.0.5");
        await sesion.Tanda(0, mitad, Conexion);

        var siguiente = await sesion.Preguntar(0, Conexion);

        AfirmarBloqueo(siguiente, ReintentoDelPrimerBloqueo);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-068")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task ESC_068_El_limite_y_el_bloqueo_son_por_la_ip_de_la_conexion_y_no_por_x_forwarded_for()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        await sesion.Tanda(0);
        var suplantando = new Dictionary<string, string> { ["X-Forwarded-For"] = OtraConexion };

        var otraIp = await sesion.Preguntar(1, OtraConexion);
        var conCabecera = await sesion.Preguntar(1, Conexion, suplantando);

        Assert.Equal(200, otraIp.Estado);
        AfirmarBloqueo(conCabecera, "29");
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-084")]
    [Trait("SEG", "SEG-13")]
    [Trait("D", "D-72")]
    public async Task CA_13_Las_tandas_de_los_segundos_0_30_90_210_y_930_reciben_30_60_120_120_y_30_sin_afectar_a_otra_ip()
    {
        using var fabrica = new FabricaApi();
        var sesion = new Sesion(fabrica);
        var tandas = new[]
        {
            (Segundo: 0, Retry: "30"),
            (Segundo: 30, Retry: "60"),
            (Segundo: 90, Retry: "120"),
            (Segundo: 210, Retry: "120"),
            (Segundo: 930, Retry: "30")
        };

        foreach (var (segundo, retry) in tandas)
        {
            AfirmarTanda(await sesion.Tanda(segundo), retry);
            Assert.Equal(200, (await sesion.Preguntar(segundo, OtraConexion)).Estado);
            if (segundo == 0)
            {
                AfirmarBloqueo(await sesion.Preguntar(29.5), "1");
            }
        }
    }
}
