using TutorFlow.Core.Asistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class LimitadorIdentidadesTests
{
    private const string Ip = "10.0.0.5";

    private sealed class RelojManual : IReloj
    {
        private readonly DateTimeOffset _origen = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);
        private TimeSpan _transcurrido;

        public DateTimeOffset AhoraUtc => _origen + _transcurrido;

        public void EnSegundo(double segundos) => _transcurrido = TimeSpan.FromSeconds(segundos);
    }

    private sealed class Escenario
    {
        private readonly RelojManual _reloj = new();
        private readonly LimitadorIdentidades _limitador;

        public Escenario(bool bloqueo, int maximo = 3)
        {
            var limites = new LimitesAsistente { BloquearMultiplesIdentidades = bloqueo, MaximoIdentidadesPorIp = maximo };
            _limitador = new LimitadorIdentidades(_reloj, limites);
        }

        public ResultadoIdentidades Registrar(string id, double minuto, string ip = Ip)
        {
            _reloj.EnSegundo(minuto * 60);
            return _limitador.Registrar(ip, id);
        }

        public void RegistrarTres()
        {
            Registrar("EST-001", 0);
            Registrar("EST-002", 1);
            Registrar("EST-003", 2);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void La_tercera_identidad_entra_y_no_supera_el_umbral(bool bloqueo)
    {
        var escenario = new Escenario(bloqueo);
        escenario.Registrar("EST-001", 0);
        escenario.Registrar("EST-002", 1);

        var tercera = escenario.Registrar("EST-003", 2);

        Assert.False(tercera.Superado);
        Assert.False(tercera.SuperacionNueva);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void La_cuarta_identidad_supera_el_umbral_con_evento_y_la_lista_en_orden_de_llegada(bool bloqueo)
    {
        var escenario = new Escenario(bloqueo);
        escenario.RegistrarTres();

        var cuarta = escenario.Registrar("EST-004", 3);

        Assert.True(cuarta.Superado);
        Assert.True(cuarta.SuperacionNueva);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004"], cuarta.Ids);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public void Con_bloqueo_una_identidad_ya_registrada_se_atiende_aunque_haya_una_nueva_bloqueada()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();
        escenario.Registrar("EST-004", 3);

        var registrada = escenario.Registrar("EST-001", 4);

        Assert.False(registrada.Superado);
        Assert.False(registrada.SuperacionNueva);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public void Con_bloqueo_el_reintento_no_emite_otro_evento_y_sigue_superando_el_umbral()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();
        escenario.Registrar("EST-004", 3);

        var reintento = escenario.Registrar("EST-004", 9);

        Assert.True(reintento.Superado);
        Assert.False(reintento.SuperacionNueva);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004"], reintento.Ids);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public void Con_bloqueo_el_intento_bloqueado_no_se_registra_y_la_entrada_vence_en_el_minuto_10_exacto()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();
        escenario.Registrar("EST-004", 3);
        escenario.Registrar("EST-004", 9);

        var enElMinuto10 = escenario.Registrar("EST-004", 10);

        Assert.False(enElMinuto10.Superado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public void Una_entrada_sigue_vigente_un_instante_antes_de_los_10_minutos()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();

        var antes = escenario.Registrar("EST-004", 599.5 / 60);

        Assert.True(antes.Superado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-098")]
    [Trait("SEG", "SEG-16")]
    public void Con_bloqueo_tras_vencer_una_entrada_la_identidad_vencida_cuenta_como_nueva_y_emite_un_evento_nuevo()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();
        escenario.Registrar("EST-004", 3);
        escenario.Registrar("EST-004", 10);

        var vencida = escenario.Registrar("EST-001", 10);

        Assert.True(vencida.Superado);
        Assert.True(vencida.SuperacionNueva);
        Assert.Equal(["EST-002", "EST-003", "EST-004", "EST-001"], vencida.Ids);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-099")]
    [Trait("SEG", "SEG-16")]
    public void Una_identidad_registrada_renueva_su_hora_y_la_mas_antigua_pasa_a_ser_otra()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();

        var renovada = escenario.Registrar("EST-001", 8);
        var nuevaEnElMinuto11 = escenario.Registrar("EST-004", 11);

        Assert.False(renovada.Superado);
        Assert.False(nuevaEnElMinuto11.Superado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-099")]
    [Trait("SEG", "SEG-16")]
    public void Con_umbral_1_solo_la_identidad_nueva_supera_el_umbral()
    {
        var escenario = new Escenario(bloqueo: true, maximo: 1);
        escenario.Registrar("EST-001", 0);

        var nueva = escenario.Registrar("EST-002", 1);
        var registrada = escenario.Registrar("EST-001", 1);

        Assert.True(nueva.Superado);
        Assert.False(registrada.Superado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-097")]
    [Trait("SEG", "SEG-16")]
    public void Sin_bloqueo_cada_identidad_nueva_sobre_el_umbral_emite_un_evento_y_la_repeticion_no()
    {
        var escenario = new Escenario(bloqueo: false);
        escenario.RegistrarTres();

        var cuarta = escenario.Registrar("EST-004", 3);
        var quinta = escenario.Registrar("EST-005", 4);
        var repetida = escenario.Registrar("EST-004", 5);

        Assert.True(cuarta.SuperacionNueva);
        Assert.True(quinta.SuperacionNueva);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004", "EST-005"], quinta.Ids);
        Assert.False(repetida.SuperacionNueva);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void Las_ips_distintas_llevan_ventanas_independientes()
    {
        var escenario = new Escenario(bloqueo: true);
        escenario.RegistrarTres();

        var otraIp = escenario.Registrar("EST-004", 3, "10.0.0.6");

        Assert.False(otraIp.Superado);
    }
}
