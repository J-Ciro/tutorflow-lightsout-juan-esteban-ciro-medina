using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class AvisosDeConfiguracionApiTests
{
    private const string ReglaFormatoCodigo = @"formato ^[A-Z]{2,4}-[0-9]{3}\z";
    private const string ClaveCodigoBd = "Autorizacion:Asignaturas:2:Codigo";
    private const string PreguntaNotaEst103 = "¿Cuál es mi nota en EST-103?";
    private const string AdvertenciaDeExclusion = "Configuración ignorada: Autorizacion:Asignaturas:8:Codigo = \"EST-002\" (coincide con el ID de una persona del registro); se excluye del catálogo";
    private const int PeticionesRepetidas = 3;

    public static TheoryData<char, string> CaracteresDeSeparacionOFormato => new()
    {
        { '\u2028', "\\u2028" },
        { '\u2029', "\\u2029" },
        { '\u202E', "\\u202E" },
        { '\u200B', "\\u200B" }
    };

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public async Task ESC_188a_La_advertencia_de_exclusion_del_catalogo_se_emite_una_sola_vez_aunque_haya_varias_peticiones()
    {
        var configuracion = new Dictionary<string, string?>
        {
            ["Autorizacion:Asignaturas:8:Codigo"] = "EST-002",
            ["Autorizacion:Asignaturas:8:Nombre"] = "Electiva de prueba"
        };
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());

        for (var i = 0; i < PeticionesRepetidas; i++)
        {
            var respuesta = await fabrica.PreguntarEstudianteAsync("EST-006", PreguntaNotaEst103);
            Assert.Equal(200, respuesta.Estado);
        }

        Assert.Single(fabrica.Logs.Mensajes, m => m.Contains(AdvertenciaDeExclusion, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(CaracteresDeSeparacionOFormato))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-188")]
    [Trait("RF", "RNF-05")]
    public void ESC_188b_El_valor_del_mensaje_escapa_los_separadores_de_linea_y_los_caracteres_de_formato(char caracter, string escapado)
    {
        var configuracion = new Dictionary<string, string?> { [ClaveCodigoBd] = $"BD-203{caracter}Arranque correcto" };
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());

        var resultado = ResultadoArranque.De(fabrica);

        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        Assert.Contains(Invalida(ClaveCodigoBd, $"BD-203{escapado}Arranque correcto", ReglaFormatoCodigo), resultado.LineasDelError);
        Assert.DoesNotContain(caracter.ToString(), resultado.TextoCompleto, StringComparison.Ordinal);
        Assert.DoesNotContain(resultado.LineasDelError.Concat(resultado.LineasDeLaConsola), l => l.StartsWith("Arranque correcto", StringComparison.Ordinal));
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }
}
