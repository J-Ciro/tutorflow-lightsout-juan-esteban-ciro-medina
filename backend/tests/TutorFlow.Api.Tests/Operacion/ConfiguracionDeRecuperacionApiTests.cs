using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ConfiguracionDeRecuperacionApiTests
{
    private const string PuntajeRelativo = "Conocimiento:PuntajeRelativoMinimo";
    private const string Semilla = "Llm:Semilla";
    private const string ReglaPuntaje = "entero de 1 a 100";

    public static TheoryData<string, string> ValoresInvalidos => new()
    {
        { "0", "0" },
        { "101", "101" },
        { "-1", "-1" },
        { "50.0", "50.0" },
        { "50,5", "50,5" },
        { "cincuenta", "cincuenta" },
        { " 50 ", " 50 " }
    };

    public static TheoryData<string> ValoresEnElLimite => new() { "1", "100", "50", "" };

    public static TheoryData<string> ValoresDeSemilla => new() { "42", "0", "-1", "2147483648", "1.5", "uno", " 1 ", "" };

    private static ResultadoArranque Iniciar(Dictionary<string, string?> configuracion)
    {
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());
        return ResultadoArranque.De(fabrica);
    }

    [Theory]
    [MemberData(nameof(ValoresInvalidos))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public void ESC_188c_Un_puntaje_relativo_minimo_invalido_impide_arrancar_con_la_linea_exacta(string valor, string valorEscapado)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [PuntajeRelativo] = valor });

        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        Assert.Contains(Invalida(PuntajeRelativo, valorEscapado, ReglaPuntaje), resultado.LineasDelError);
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ValoresEnElLimite))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public void ESC_188c_Los_valores_en_el_limite_y_los_vacios_arrancan_sin_advertencia(string valor)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [PuntajeRelativo] = valor });

        Assert.True(resultado.Arranco, string.Join("\n", resultado.LineasDelError));
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal) || l.StartsWith(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ValoresDeSemilla))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-85")]
    public void ESC_192h_Llm_Semilla_ya_no_existe_y_cualquier_valor_se_ignora_sin_validacion_ni_advertencia(string valor)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [Semilla] = valor });

        Assert.True(resultado.Arranco, string.Join("\n", resultado.LineasDelError));
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.Contains(Semilla, StringComparison.Ordinal));
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal) || l.StartsWith(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public void ESC_188c_Un_puntaje_invalido_se_informa_aunque_Llm_Semilla_traiga_un_valor_que_antes_era_invalido()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [PuntajeRelativo] = "0", [Semilla] = "-1" });

        Assert.False(resultado.Arranco);
        Assert.Contains(Invalida(PuntajeRelativo, "0", ReglaPuntaje), resultado.LineasDelError);
        Assert.DoesNotContain(resultado.LineasDelError, l => l.Contains(Semilla, StringComparison.Ordinal));
    }
}
