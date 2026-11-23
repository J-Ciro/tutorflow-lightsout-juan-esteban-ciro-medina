using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ConfiguracionDeRecuperacionYSemillaApiTests
{
    private const string PuntajeRelativo = "Conocimiento:PuntajeRelativoMinimo";
    private const string Semilla = "Llm:Semilla";
    private const string ReglaPuntaje = "entero de 1 a 100";
    private const string ReglaSemilla = "entero de 0 a 2147483647";

    public static TheoryData<string, string, string> ValoresInvalidos => new()
    {
        { PuntajeRelativo, "0", ReglaPuntaje },
        { PuntajeRelativo, "101", ReglaPuntaje },
        { PuntajeRelativo, "-1", ReglaPuntaje },
        { PuntajeRelativo, "50.0", ReglaPuntaje },
        { PuntajeRelativo, "50,5", ReglaPuntaje },
        { PuntajeRelativo, "cincuenta", ReglaPuntaje },
        { PuntajeRelativo, " 50 ", ReglaPuntaje },
        { Semilla, "-1", ReglaSemilla },
        { Semilla, "2147483648", ReglaSemilla },
        { Semilla, "1.5", ReglaSemilla },
        { Semilla, "uno", ReglaSemilla },
        { Semilla, "1e3", ReglaSemilla },
        { Semilla, " 1 ", ReglaSemilla }
    };

    public static TheoryData<string, string> ValoresEnElLimite => new()
    {
        { PuntajeRelativo, "1" },
        { PuntajeRelativo, "100" },
        { PuntajeRelativo, "50" },
        { Semilla, "0" },
        { Semilla, "1" },
        { Semilla, "2147483647" },
        { PuntajeRelativo, "" },
        { Semilla, "" }
    };

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
    public void ESC_188c_Un_valor_invalido_impide_arrancar_con_la_linea_exacta(string clave, string valor, string regla)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        Assert.Contains(Invalida(clave, valor, regla), resultado.LineasDelError);
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ValoresEnElLimite))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public void ESC_188c_Los_valores_en_el_limite_y_los_vacios_arrancan_sin_advertencia(string clave, string valor)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        Assert.True(resultado.Arranco, string.Join("\n", resultado.LineasDelError));
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal) || l.StartsWith(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public void ESC_188c_Se_informan_a_la_vez_el_puntaje_relativo_y_la_semilla_invalidos()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [PuntajeRelativo] = "0", [Semilla] = "-1" });

        Assert.False(resultado.Arranco);
        Assert.Contains(Invalida(PuntajeRelativo, "0", ReglaPuntaje), resultado.LineasDelError);
        Assert.Contains(Invalida(Semilla, "-1", ReglaSemilla), resultado.LineasDelError);
    }
}
