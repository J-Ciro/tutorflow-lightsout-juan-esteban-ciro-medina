using TutorFlow.Api;
using Microsoft.Extensions.Configuration;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ValidadorDeConfiguracionTests
{
    private static readonly Dictionary<string, string?> CatalogoValido = new()
    {
        ["Autorizacion:Asignaturas:0:Codigo"] = "CAL-201",
        ["Autorizacion:Asignaturas:0:Nombre"] = "Cálculo Integral",
        ["Autorizacion:Asignaturas:1:Codigo"] = "EST-103",
        ["Autorizacion:Asignaturas:1:Nombre"] = "Estadística"
    };

    private static IConfiguration Configuracion(IEnumerable<KeyValuePair<string, string?>> valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181af_Un_catalogo_sin_entradas_es_invalido_con_la_linea_de_catalogo_vacio()
    {
        var lineas = ValidadorDeConfiguracion.Validar(Configuracion([]));

        Assert.Equal([Invalida("Autorizacion:Asignaturas", string.Empty, "catálogo vacío")], lineas);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_catalogo_valido_y_los_demas_valores_ausentes_no_producen_lineas()
    {
        var lineas = ValidadorDeConfiguracion.Validar(Configuracion(CatalogoValido));

        Assert.Empty(lineas);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_catalogo_con_un_codigo_sin_formato_y_un_limite_fuera_de_rango_devuelve_una_linea_por_valor()
    {
        var valores = new Dictionary<string, string?>(CatalogoValido)
        {
            ["Autorizacion:Asignaturas:1:Codigo"] = "est103",
            ["Conocimiento:RelevanciaMinima"] = "0"
        };

        var lineas = ValidadorDeConfiguracion.Validar(Configuracion(valores));

        Assert.Equal(2, lineas.Count);
        Assert.Contains(Invalida("Autorizacion:Asignaturas:1:Codigo", "est103", @"formato ^[A-Z]{2,4}-[0-9]{3}\z"), lineas);
        Assert.Contains(Invalida("Conocimiento:RelevanciaMinima", "0", "entero de 1 a 10"), lineas);
    }
}
