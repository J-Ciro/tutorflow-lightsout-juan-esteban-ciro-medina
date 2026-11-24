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

        public Escenario(int maximo = 3)
        {
            _limitador = new LimitadorIdentidades(_reloj, new LimitesAsistente { MaximoIdentidadesPorIp = maximo });
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

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void La_tercera_identidad_entra_sin_emitir_evento()
    {
        var escenario = new Escenario();
        escenario.Registrar("EST-001", 0);
        escenario.Registrar("EST-002", 1);

        var tercera = escenario.Registrar("EST-003", 2);

        Assert.False(tercera.SuperacionNueva);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-140")]
    [Trait("SEG", "SEG-16")]
    public void ESC_140_La_cuarta_identidad_emite_el_evento_con_la_lista_en_orden_de_llegada()
    {
        var escenario = new Escenario();
        escenario.RegistrarTres();

        var cuarta = escenario.Registrar("EST-004", 3);

        Assert.True(cuarta.SuperacionNueva);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004"], cuarta.Ids);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-097")]
    [Trait("SEG", "SEG-16")]
    public void ESC_097_Cada_identidad_nueva_sobre_el_umbral_emite_un_evento_y_la_repeticion_no()
    {
        var escenario = new Escenario();
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
    public void Una_entrada_sigue_vigente_un_instante_antes_de_los_10_minutos()
    {
        var escenario = new Escenario();
        escenario.RegistrarTres();

        var antes = escenario.Registrar("EST-004", 599.5 / 60);

        Assert.True(antes.SuperacionNueva);
        Assert.Equal(["EST-001", "EST-002", "EST-003", "EST-004"], antes.Ids);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void Una_entrada_vence_en_los_10_minutos_exactos()
    {
        var escenario = new Escenario();
        escenario.RegistrarTres();

        var enElMinuto10 = escenario.Registrar("EST-004", 10);

        Assert.False(enElMinuto10.SuperacionNueva);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void Una_identidad_registrada_renueva_su_hora_y_la_mas_antigua_pasa_a_ser_otra()
    {
        var escenario = new Escenario();
        escenario.RegistrarTres();

        var renovada = escenario.Registrar("EST-001", 8);
        var nuevaEnElMinuto11 = escenario.Registrar("EST-004", 11);

        Assert.False(renovada.SuperacionNueva);
        Assert.False(nuevaEnElMinuto11.SuperacionNueva);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-16")]
    public void Con_umbral_1_solo_la_identidad_nueva_emite_evento()
    {
        var escenario = new Escenario(maximo: 1);
        escenario.Registrar("EST-001", 0);

        var nueva = escenario.Registrar("EST-002", 1);
        var registrada = escenario.Registrar("EST-001", 1);

        Assert.True(nueva.SuperacionNueva);
        Assert.False(registrada.SuperacionNueva);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-141")]
    [Trait("SEG", "SEG-16")]
    public void ESC_141a_Las_ips_distintas_llevan_ventanas_independientes()
    {
        var escenario = new Escenario();
        escenario.RegistrarTres();

        var otraIp = escenario.Registrar("EST-004", 3, "10.0.0.6");

        Assert.False(otraIp.SuperacionNueva);
    }
}
