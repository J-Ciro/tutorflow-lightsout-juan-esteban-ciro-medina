using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class LimitadorMonotonicoApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string Conexion = "10.0.0.5";
    private const int Limite = 600;
    private const string PrimerIpv6 = "2001:db8:abcd:12::1";
    private const string SegundaIpv6DelMismoPrefijo = "2001:db8:abcd:12::2";

    private static async Task<List<RespuestaApi>> PreguntarAsync(FabricaApi fabrica, int cantidad, string ip)
    {
        var respuestas = new List<RespuestaApi>();
        for (var i = 0; i < cantidad; i++)
        {
            respuestas.Add(await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, ip));
        }

        return respuestas;
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public async Task S_13_Un_salto_del_reloj_de_pared_hacia_adelante_no_acorta_el_bloqueo()
    {
        using var fabrica = new FabricaApi();
        await PreguntarAsync(fabrica, Limite + 1, Conexion);
        fabrica.Reloj.Fijar(fabrica.Reloj.AhoraUtc + TimeSpan.FromHours(1));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, Conexion);

        Afirmaciones.ProblemDetails(respuesta, 429);
        Assert.Equal("30", respuesta.Encabezado("Retry-After"));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public async Task S_13_Un_salto_del_reloj_de_pared_hacia_atras_no_alarga_el_bloqueo()
    {
        using var fabrica = new FabricaApi();
        await PreguntarAsync(fabrica, Limite + 1, Conexion);
        fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(30));
        fabrica.Reloj.Fijar(fabrica.Reloj.AhoraUtc - TimeSpan.FromHours(1));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, Conexion);

        Assert.Equal(200, respuesta.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public async Task S_13_Un_salto_del_reloj_de_pared_no_vacia_la_ventana_de_las_marcas_admitidas()
    {
        using var fabrica = new FabricaApi();
        await PreguntarAsync(fabrica, Limite, Conexion);
        fabrica.Reloj.Fijar(fabrica.Reloj.AhoraUtc + TimeSpan.FromHours(1));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, Conexion);

        Afirmaciones.ProblemDetails(respuesta, 429);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public async Task ESC_142_La_clave_de_la_tasa_es_la_direccion_ipv6_completa_y_no_su_prefijo()
    {
        using var fabrica = new FabricaApi();
        var tanda = await PreguntarAsync(fabrica, Limite + 1, PrimerIpv6);

        var vecina = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, SegundaIpv6DelMismoPrefijo);

        Assert.Equal(429, tanda[Limite].Estado);
        Assert.Equal(200, vecina.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public async Task ESC_142_El_ipv6_auditado_es_la_direccion_completa_de_la_conexion()
    {
        using var fabrica = new FabricaApi();

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta, PrimerIpv6);

        Assert.Equal(PrimerIpv6, Assert.Single(fabrica.Auditoria.Eventos).IpOrigen);
    }
}
