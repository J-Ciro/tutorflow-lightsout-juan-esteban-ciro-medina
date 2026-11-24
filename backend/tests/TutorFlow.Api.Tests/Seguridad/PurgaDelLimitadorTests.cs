using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class PurgaDelLimitadorTests
{
    private const int Limite = 600;
    private const int IpsDistintas = 20_000;
    private const int SegundosDeVentana = 60;
    private const int SegundosDeBloqueoInicial = 30;
    private const long BytesMaximosPorLlamada = 32 * 1024;

    private static string Ip(int indice) => $"10.{indice / 65536 % 256}.{indice / 256 % 256}.{indice % 256}";

    private static void RegistrarUnaPeticionPorIp(LimitadorTasaPorIp limitador, int cantidad)
    {
        for (var i = 0; i < cantidad; i++)
        {
            limitador.Intentar(Ip(i));
        }
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public void S_09_La_purga_con_20000_ip_inactivas_no_copia_el_almacen()
    {
        var reloj = new RelojControlado();
        var limitador = new LimitadorTasaPorIp(Limite, reloj.Tiempo);
        RegistrarUnaPeticionPorIp(limitador, IpsDistintas);
        reloj.Avanzar(TimeSpan.FromSeconds(SegundosDeVentana + 1));
        var antes = GC.GetAllocatedBytesForCurrentThread();

        limitador.Intentar("192.0.2.1");

        var asignados = GC.GetAllocatedBytesForCurrentThread() - antes;
        Assert.True(asignados < BytesMaximosPorLlamada, $"la purga asignó {asignados} bytes");
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public void S_09_La_purga_conserva_a_la_ip_bloqueada_aunque_haya_20000_inactivas()
    {
        var reloj = new RelojControlado();
        var limitador = new LimitadorTasaPorIp(Limite, reloj.Tiempo);
        for (var i = 0; i <= Limite; i++)
        {
            limitador.Intentar("203.0.113.9");
        }

        RegistrarUnaPeticionPorIp(limitador, IpsDistintas);
        reloj.Avanzar(TimeSpan.FromSeconds(11));

        var resultado = limitador.Intentar("203.0.113.9");

        Assert.False(resultado.Permitido);
        Assert.Equal(SegundosDeBloqueoInicial - 11, resultado.SegundosParaReintentar);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public void S_09_La_purga_conserva_las_marcas_vigentes_de_una_ip_con_599_peticiones()
    {
        var reloj = new RelojControlado();
        var limitador = new LimitadorTasaPorIp(Limite, reloj.Tiempo);
        for (var i = 0; i < Limite - 1; i++)
        {
            limitador.Intentar("203.0.113.10");
        }

        RegistrarUnaPeticionPorIp(limitador, IpsDistintas);
        reloj.Avanzar(TimeSpan.FromSeconds(11));

        var sexcientos = limitador.Intentar("203.0.113.10");
        var siguiente = limitador.Intentar("203.0.113.10");

        Assert.True(sexcientos.Permitido);
        Assert.False(siguiente.Permitido);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("D", "D-80")]
    public void S_09_Una_ip_cuyo_bloqueo_vencio_hace_menos_de_600_s_conserva_su_escalada()
    {
        var reloj = new RelojControlado();
        var limitador = new LimitadorTasaPorIp(Limite, reloj.Tiempo);
        for (var i = 0; i <= Limite; i++)
        {
            limitador.Intentar("203.0.113.11");
        }

        reloj.Avanzar(TimeSpan.FromSeconds(SegundosDeBloqueoInicial));
        RegistrarUnaPeticionPorIp(limitador, IpsDistintas);
        reloj.Avanzar(TimeSpan.FromSeconds(SegundosDeVentana + 1));
        limitador.Intentar("192.0.2.1");

        ResultadoTasa ultimo = new(true, 0);
        for (var i = 0; i <= Limite; i++)
        {
            ultimo = limitador.Intentar("203.0.113.11");
        }

        Assert.False(ultimo.Permitido);
        Assert.Equal(2 * SegundosDeBloqueoInicial, ultimo.SegundosParaReintentar);
    }
}
