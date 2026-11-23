using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class CatalogoDeAsignaturasInvalidoApiTests
{
    private const string ReglaFormato = @"formato ^[A-Z]{2,4}-[0-9]{3}\z";

    public static TheoryData<string, string> CodigosSinFormato => new()
    {
        { "Autorizacion:Asignaturas:2:Codigo", "bd203" },
        { "Autorizacion:Asignaturas:2:Codigo", "BD203" },
        { "Autorizacion:Asignaturas:2:Codigo", "bd-203" },
        { "Autorizacion:Asignaturas:2:Codigo", "BD-20" },
        { "Autorizacion:Asignaturas:2:Codigo", "BD-2030" },
        { "Autorizacion:Asignaturas:2:Codigo", "ABCDE-203" },
        { "Autorizacion:Asignaturas:2:Codigo", "B-203" },
        { "Autorizacion:Asignaturas:2:Codigo", "BD-203 " },
        { "Autorizacion:Asignaturas:2:Codigo", " BD-203" },
        { "Autorizacion:Asignaturas:2:Codigo", "BD 203" },
        { "Autorizacion:Asignaturas:2:Codigo", "" },
        { "Autorizacion:Asignaturas:2:Codigo", "   " },
        { "Autorizacion:Asignaturas:6:Codigo", "EST-10A" },
        { "Autorizacion:Asignaturas:7:Codigo", "com-104" }
    };

    public static TheoryData<string, string> NombresVacios => new()
    {
        { "Autorizacion:Asignaturas:6:Nombre", "" },
        { "Autorizacion:Asignaturas:6:Nombre", "   " },
        { "Autorizacion:Asignaturas:0:Nombre", "\t" }
    };

    public static TheoryData<string, string, string> NombresDuplicados => new()
    {
        { "7", "Bases de Datos", "2" },
        { "7", "bases de datos", "2" },
        { "7", "estadistica", "6" },
        { "7", "ESTADÍSTICA", "6" },
        { "3", "cálculo integral", "0" }
    };

    public static TheoryData<string, string, string> CodigosDeLaListaSeparada => new()
    {
        { "XYZ-999", "no está en Autorizacion:Asignaturas", "Autorizacion:CodigosAsignatura:8" },
        { "xyz999", ReglaFormato, "Autorizacion:CodigosAsignatura:8" }
    };

    private static ResultadoArranque Iniciar(Dictionary<string, string?> configuracion)
    {
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());
        return ResultadoArranque.De(fabrica);
    }

    private static void AfirmarArranqueFallido(ResultadoArranque resultado, string linea)
    {
        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        Assert.Contains(linea, resultado.LineasDelError);
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CodigosSinFormato))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181k_Un_codigo_del_catalogo_sin_el_formato_impide_arrancar(string clave, string codigo)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = codigo });

        AfirmarArranqueFallido(resultado, Invalida(clave, codigo, ReglaFormato));
    }

    [Theory]
    [InlineData("1", "BD-203", "2", "1")]
    [InlineData("7", "BD-203", "7", "2")]
    [InlineData("3", "CAL-201", "3", "0")]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181l_Un_codigo_duplicado_del_catalogo_impide_arrancar_y_nombra_la_primera_aparicion(string indice, string codigo, string repeticion, string primera)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [$"Autorizacion:Asignaturas:{indice}:Codigo"] = codigo });

        AfirmarArranqueFallido(
            resultado,
            Invalida($"Autorizacion:Asignaturas:{repeticion}:Codigo", codigo, $"código duplicado de Autorizacion:Asignaturas:{primera}:Codigo"));
    }

    [Theory]
    [MemberData(nameof(NombresVacios))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181m_Un_nombre_vacio_o_en_blanco_impide_arrancar(string clave, string nombre)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = nombre });

        AfirmarArranqueFallido(resultado, Invalida(clave, nombre.Replace("\t", "\\t", StringComparison.Ordinal), "nombre vacío"));
    }

    [Theory]
    [MemberData(nameof(NombresDuplicados))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_181_Un_nombre_repetido_tras_normalizar_con_seg_04a_impide_arrancar(string indice, string nombre, string primera)
    {
        var clave = $"Autorizacion:Asignaturas:{indice}:Nombre";

        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = nombre });

        AfirmarArranqueFallido(resultado, Invalida(clave, nombre, $"nombre duplicado de Autorizacion:Asignaturas:{primera}:Nombre"));
    }

    [Theory]
    [MemberData(nameof(CodigosDeLaListaSeparada))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_codigo_de_la_lista_separada_debe_cumplir_el_formato_no_repetirse_y_estar_en_el_catalogo(string codigo, string regla, string clave)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = codigo });

        AfirmarArranqueFallido(resultado, Invalida(clave, codigo, regla));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_codigo_repetido_en_la_lista_separada_nombra_la_primera_aparicion()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            ["Autorizacion:CodigosAsignatura:0"] = "CAL-201",
            ["Autorizacion:CodigosAsignatura:8"] = "CAL-201"
        });

        AfirmarArranqueFallido(
            resultado,
            Invalida("Autorizacion:CodigosAsignatura:8", "CAL-201", "código duplicado de Autorizacion:CodigosAsignatura:0"));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Varios_defectos_del_catalogo_se_informan_todos()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            ["Autorizacion:Asignaturas:2:Codigo"] = "bd203",
            ["Autorizacion:Asignaturas:6:Nombre"] = "   ",
            ["Autorizacion:Asignaturas:7:Codigo"] = "CAL-201"
        });

        Assert.False(resultado.Arranco);
        Assert.Contains(Invalida("Autorizacion:Asignaturas:2:Codigo", "bd203", ReglaFormato), resultado.LineasDelError);
        Assert.Contains(Invalida("Autorizacion:Asignaturas:6:Nombre", "   ", "nombre vacío"), resultado.LineasDelError);
        Assert.Contains(Invalida("Autorizacion:Asignaturas:7:Codigo", "CAL-201", "código duplicado de Autorizacion:Asignaturas:0:Codigo"), resultado.LineasDelError);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public async Task ESC_181_El_catalogo_de_la_spec_con_sus_8_asignaturas_arranca_sin_advertencias()
    {
        using var fabrica = new FabricaApi(variablesEntorno: EntornoConLlave());

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Equal(200, salud.Estado);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }
}
