using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ProxyConfiableConfiguracionApiTests
{
    private const string Regla = "IP individual IPv4 o IPv6, sin red, rango, puerto ni zona";
    private const string Clave0 = "Proxy:IpsConfiables:0";

    public static TheoryData<string, string> EntradasInvalidas => new()
    {
        { "a", "10.0.0.0/8" },
        { "a", "172.30.80.0/29" },
        { "a", "2001:db8::/32" },
        { "b", "10.0.0.5-10.0.0.9" },
        { "c", "abc" },
        { "c", "10.0.0.256" },
        { "c", "10.1" },
        { "c", "010.0.0.5" },
        { "c", " 10.0.0.5" },
        { "d", "10.0.0.5:8080" },
        { "d", "fe80::1%eth0" },
        { "e", "0.0.0.0" },
        { "e", "::" }
    };

    public static TheoryData<string> EntradasValidas => new()
    {
        "10.0.0.5",
        "2001:db8::5",
        "172.30.80.2"
    };

    private static ResultadoArranque Iniciar(Dictionary<string, string?> configuracion)
    {
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());
        return ResultadoArranque.De(fabrica);
    }

    private static void AfirmarSinAdvertencias(ResultadoArranque resultado)
    {
        Assert.True(resultado.Arranco, "el arranque no debió fallar");
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal));
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.StartsWith(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(EntradasInvalidas))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-096")]
    [Trait("RF", "RNF-05")]
    public void ESC_096_Una_ip_con_red_rango_puerto_zona_o_formato_invalido_impide_arrancar(string caso, string valor)
    {
        Assert.NotEmpty(caso);

        var resultado = Iniciar(new Dictionary<string, string?> { [Clave0] = valor });

        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        Assert.Contains(Invalida(Clave0, valor, Regla), resultado.LineasDelError);
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-096")]
    [Trait("RF", "RNF-05")]
    public void ESC_096f_Solo_se_informa_la_entrada_invalida_con_su_indice()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            [Clave0] = "10.0.0.5",
            ["Proxy:IpsConfiables:1"] = "abc"
        });

        Assert.False(resultado.Arranco);
        var invalidas = resultado.LineasDelError.Where(l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal)).Distinct().ToList();
        Assert.Equal([Invalida("Proxy:IpsConfiables:1", "abc", Regla)], invalidas);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-096")]
    [Trait("RF", "RNF-05")]
    public void ESC_096g_Un_valor_escalar_en_lugar_de_una_lista_impide_arrancar()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { ["Proxy:IpsConfiables"] = "172.30.80.2" });

        Assert.False(resultado.Arranco);
        Assert.Contains(
            resultado.LineasDelError,
            l => l.StartsWith($"{ResultadoArranque.PrefijoInvalida}: Proxy:IpsConfiables = \"172.30.80.2\" (", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-096")]
    [Trait("RF", "RNF-05")]
    public void ESC_096h_Un_duplicado_arranca_sin_advertencia_y_el_log_registra_la_ip_de_confianza()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            [Clave0] = "10.0.0.5",
            ["Proxy:IpsConfiables:1"] = "10.0.0.5"
        });

        AfirmarSinAdvertencias(resultado);
        Assert.Contains(resultado.LineasDeLaConsola, l => l.Contains("10.0.0.5", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(EntradasValidas))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-096")]
    [Trait("RF", "RNF-05")]
    public void ESC_096i_Una_ip_individual_ipv4_o_ipv6_arranca_sin_advertencia(string valor)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [Clave0] = valor });

        AfirmarSinAdvertencias(resultado);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-096")]
    [Trait("RF", "RNF-05")]
    public async Task ESC_096j_Una_entrada_vacia_arranca_sin_advertencia_y_se_comporta_como_la_lista_vacia()
    {
        using var fabrica = new FabricaApi(configuracion: new Dictionary<string, string?> { [Clave0] = "" });
        AfirmarSinAdvertencias(ResultadoArranque.De(fabrica));
        var encabezados = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" };

        var respuesta = await fabrica.PreguntarAsync("estudiante", "EST-001", "¿Cuál es la nota mínima para aprobar?", "10.0.0.5", encabezados);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("10.0.0.5", fabrica.Auditoria.Eventos[^1].IpOrigen);
    }
}
