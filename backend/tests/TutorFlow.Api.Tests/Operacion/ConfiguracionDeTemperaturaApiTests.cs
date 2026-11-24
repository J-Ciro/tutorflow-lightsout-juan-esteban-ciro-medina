using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ConfiguracionDeTemperaturaApiTests
{
    private const string Temperatura = "Llm:Temperatura";
    private const string PuntajeRelevante = "Conocimiento:PuntajeRelativoRelevante";
    private const string PuntajeMinimo = "Conocimiento:PuntajeRelativoMinimo";
    private const string ReglaTemperatura = "decimal de 0 a 0.3";
    private const string ReglaPuntaje = "entero de 1 a 100";

    public static TheoryData<string, string, string> ValoresInvalidos => new()
    {
        { Temperatura, "0.31", ReglaTemperatura },
        { Temperatura, "-0.1", ReglaTemperatura },
        { Temperatura, "0,2", ReglaTemperatura },
        { Temperatura, "1e-1", ReglaTemperatura },
        { Temperatura, " 0.1", ReglaTemperatura },
        { Temperatura, "0.1 ", ReglaTemperatura },
        { Temperatura, "abc", ReglaTemperatura },
        { PuntajeRelevante, "0", ReglaPuntaje },
        { PuntajeRelevante, "101", ReglaPuntaje },
        { PuntajeMinimo, " 55", ReglaPuntaje }
    };

    public static TheoryData<string, string> ValoresValidos => new()
    {
        { Temperatura, "0" },
        { Temperatura, "0.3" },
        { Temperatura, "0.30" },
        { PuntajeRelevante, "40" },
        { Temperatura, "" }
    };

    private static ResultadoArranque Iniciar(Dictionary<string, string?> configuracion)
    {
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());
        return ResultadoArranque.De(fabrica);
    }

    [Theory]
    [MemberData(nameof(ValoresInvalidos))]
    [Trait("CA", "CA-15")]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-59")]
    [Trait("D", "D-63")]
    public void ESC_199c1_Un_valor_invalido_impide_arrancar_con_la_linea_exacta(string clave, string valor, string regla)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        Assert.Contains(Invalida(clave, valor, regla), resultado.LineasDelError);
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ValoresValidos))]
    [Trait("CA", "CA-15")]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-59")]
    public void ESC_199c2_Los_valores_en_el_rango_arrancan_sin_advertencia(string clave, string valor)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        Assert.True(resultado.Arranco, string.Join("\n", resultado.LineasDelError));
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal) || l.StartsWith(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-59")]
    public void ESC_199c1_Todos_los_valores_invalidos_se_informan_a_la_vez()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [Temperatura] = "0.31", [PuntajeRelevante] = "0" });

        Assert.False(resultado.Arranco);
        Assert.Contains(Invalida(Temperatura, "0.31", ReglaTemperatura), resultado.LineasDelError);
        Assert.Contains(Invalida(PuntajeRelevante, "0", ReglaPuntaje), resultado.LineasDelError);
    }
}
