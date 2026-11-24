using Microsoft.Extensions.DependencyInjection;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class TopeDelLimitadorD104Tests
{
    private const int TopeDeIps = 50_000;
    private const int Limite = 600;
    private const string Advertencia = "Limitador por IP lleno";
    private const string PreguntaNormativa = "¿Cuál es la nota mínima para aprobar una asignatura?";

    private static string Ip(int indice) => $"172.{16 + indice / 65536 % 16}.{indice / 256 % 256}.{indice % 256}";

    private static LimitadorTasaPorIp LlenarElAlmacen(FabricaApi fabrica)
    {
        var limitador = fabrica.Services.GetRequiredService<LimitadorTasaPorIp>();
        for (var i = 0; i < Limite; i++)
        {
            limitador.Intentar(FabricaApi.IpPorDefecto);
        }

        for (var i = 0; i < TopeDeIps - 1; i++)
        {
            limitador.Intentar(Ip(i));
        }

        return limitador;
    }

    private static int Advertencias(FabricaApi fabrica) =>
        fabrica.Logs.Mensajes.Count(m => m.Contains(Advertencia, StringComparison.Ordinal));

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-104")]
    public async Task ESC_142_h_Con_el_almacen_lleno_la_ip_nueva_se_atiende_sin_429_en_el_segundo_0_y_en_el_30()
    {
        using var fabrica = new FabricaApi();
        LlenarElAlmacen(fabrica);

        var primera = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa, "198.51.100.1");
        fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(30));
        var segunda = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa, "198.51.100.2");

        Assert.NotEqual(429, primera.Estado);
        Assert.NotEqual(429, segunda.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-104")]
    public async Task ESC_142_h_El_log_advierte_Limitador_por_IP_lleno_una_sola_vez_por_minuto()
    {
        using var fabrica = new FabricaApi();
        LlenarElAlmacen(fabrica);

        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa, "198.51.100.1");
        fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(30));
        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa, "198.51.100.2");
        var dentroDelMinuto = Advertencias(fabrica);
        fabrica.Reloj.Avanzar(TimeSpan.FromSeconds(31));
        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa, "198.51.100.3");

        Assert.Equal(1, dentroDelMinuto);
        Assert.Equal(2, Advertencias(fabrica));
        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("50000 IPs", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-104")]
    public async Task ESC_142_h_La_ip_nueva_no_se_registra_y_el_evaluador_sigue_contando_hasta_su_429()
    {
        using var fabrica = new FabricaApi();
        var limitador = LlenarElAlmacen(fabrica);

        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa, "198.51.100.1");
        var evaluador = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaNormativa);

        Assert.Equal(429, evaluador.Estado);
        Assert.True(limitador.Intentar("198.51.100.1").Permitido);
    }
}
