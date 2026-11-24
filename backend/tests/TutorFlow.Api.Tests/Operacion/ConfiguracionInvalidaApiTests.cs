using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ConfiguracionInvalidaApiTests
{
    private const string ReglaEntero1a10 = "entero de 1 a 10";
    private const string ReglaPresupuesto = "decimal mayor que 0 y hasta 1000";
    private const string ReglaPrecio = "decimal de 0 a 1000";
    private const string ReglaFormatoCodigo = @"formato ^[A-Z]{2,4}-[0-9]{3}\z";
    private const string RelevanciaMinima = "Conocimiento:RelevanciaMinima";
    private const string PresupuestoDiario = "Limites:PresupuestoDiarioUsd";
    private const string CodigoBd = "Autorizacion:Asignaturas:2:Codigo";

    public static TheoryData<string, string, string, string> FilasDeLaSpec => new()
    {
        { "a", RelevanciaMinima, "0", Invalida(RelevanciaMinima, "0", ReglaEntero1a10) },
        { "b", RelevanciaMinima, "11", Invalida(RelevanciaMinima, "11", ReglaEntero1a10) },
        { "c", "Conocimiento:ApoyoMinimoRespaldo", "dos", Invalida("Conocimiento:ApoyoMinimoRespaldo", "dos", ReglaEntero1a10) },
        { "d", "Conocimiento:ApoyoMinimoRespaldo", "2.0", Invalida("Conocimiento:ApoyoMinimoRespaldo", "2.0", ReglaEntero1a10) },
        { "e", "Limites:PreguntasPorMinutoPorIp", "0", Invalida("Limites:PreguntasPorMinutoPorIp", "0", "entero de 1 a 10000") },
        { "f", "Limites:TimeoutLlmSegundos", "-5", Invalida("Limites:TimeoutLlmSegundos", "-5", "entero de 1 a 60") },
        { "g", "Limites:MaximoSeccionesRecuperadas", "7", Invalida("Limites:MaximoSeccionesRecuperadas", "7", "entero de 1 a 6") },
        { "h", PresupuestoDiario, "0", Invalida(PresupuestoDiario, "0", ReglaPresupuesto) },
        { "i", PresupuestoDiario, "0,5", Invalida(PresupuestoDiario, "0,5", ReglaPresupuesto) },
        { "k", CodigoBd, "bd203", Invalida(CodigoBd, "bd203", ReglaFormatoCodigo) },
        { "l", "Autorizacion:Asignaturas:7:Codigo", "BD-203", Invalida("Autorizacion:Asignaturas:7:Codigo", "BD-203", "código duplicado de Autorizacion:Asignaturas:2:Codigo") },
        { "m", "Autorizacion:Asignaturas:6:Nombre", "", Invalida("Autorizacion:Asignaturas:6:Nombre", "", "nombre vacío") }
    };

    public static TheoryData<string, string, string> LimitesFueraDeRango => new()
    {
        { "Conocimiento:RelevanciaMinima", "0", "entero de 1 a 10" },
        { "Conocimiento:RelevanciaMinima", "11", "entero de 1 a 10" },
        { "Conocimiento:ApoyoMinimoRespaldo", "0", "entero de 1 a 10" },
        { "Conocimiento:ApoyoMinimoRespaldo", "11", "entero de 1 a 10" },
        { "Limites:MaximoCaracteresPregunta", "0", "entero de 1 a 1000" },
        { "Limites:MaximoCaracteresPregunta", "1001", "entero de 1 a 1000" },
        { "Limites:MaximoBytesCuerpo", "1023", "entero de 1024 a 16384" },
        { "Limites:MaximoBytesCuerpo", "16385", "entero de 1024 a 16384" },
        { "Limites:PreguntasPorMinutoPorIp", "0", "entero de 1 a 10000" },
        { "Limites:PreguntasPorMinutoPorIp", "10001", "entero de 1 a 10000" },
        { "Limites:MaximoSeccionesRecuperadas", "0", "entero de 1 a 6" },
        { "Limites:MaximoSeccionesRecuperadas", "7", "entero de 1 a 6" },
        { "Limites:MaximoTokensSalida", "0", "entero de 1 a 2000" },
        { "Limites:MaximoTokensSalida", "2001", "entero de 1 a 2000" },
        { "Limites:MaximoInvocacionesHerramientas", "0", "entero de 1 a 4" },
        { "Limites:MaximoInvocacionesHerramientas", "5", "entero de 1 a 4" },
        { "Limites:TimeoutLlmSegundos", "0", "entero de 1 a 60" },
        { "Limites:TimeoutLlmSegundos", "61", "entero de 1 a 60" },
        { "Limites:PresupuestoApiAcademicaSegundos", "0", "entero de 1 a 30" },
        { "Limites:PresupuestoApiAcademicaSegundos", "31", "entero de 1 a 30" },
        { "Limites:MaximoIdentidadesPorIp", "0", "entero de 1 a 100" },
        { "Limites:MaximoIdentidadesPorIp", "101", "entero de 1 a 100" },
        { "Limites:VentanaIdentidadesMinutos", "0", "entero de 1 a 1440" },
        { "Limites:VentanaIdentidadesMinutos", "1441", "entero de 1 a 1440" }
    };

    public static TheoryData<string> FormatosEnterosInvalidos => new()
    {
        "2.0",
        "2,0",
        "dos",
        "1e-3",
        "0x2",
        "1 000"
    };

    public static TheoryData<string, string, string> DecimalesYBooleanosInvalidos => new()
    {
        { PresupuestoDiario, "0", ReglaPresupuesto },
        { PresupuestoDiario, "-1", ReglaPresupuesto },
        { PresupuestoDiario, "0,5", ReglaPresupuesto },
        { PresupuestoDiario, "1e-3", ReglaPresupuesto },
        { PresupuestoDiario, "1000.01", ReglaPresupuesto },
        { PresupuestoDiario, "abc", ReglaPresupuesto },
        { "Llm:Proveedores:anthropic:Modelos:0:PrecioEntradaPorMillon", "-1", ReglaPrecio },
        { "Llm:Proveedores:anthropic:Modelos:0:PrecioSalidaPorMillon", "1000.01", ReglaPrecio },
    };

    public static TheoryData<string, string, string> ValoresConCaracteresEspeciales => new()
    {
        { "comilla-y-barra", "a\"b\\c", "a\\\"b\\\\c" },
        { "tabulador", "x\ty", "x\\ty" },
        { "retorno", "x\ry", "x\\ry" },
        { "salto", "x\ny", "x\\ny" },
        { "control", "x\u0001y", "x\\u0001y" },
        { "65-caracteres", new string('a', 65), new string('a', 64) + "…" },
        { "70-caracteres", new string('a', 70), new string('a', 64) + "…" },
        { "64-caracteres", new string('a', 64), new string('a', 64) }
    };

    private static void AfirmarArranqueFallido(ResultadoArranque resultado, params string[] lineas)
    {
        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        foreach (var linea in lineas)
        {
            Assert.Contains(linea, resultado.LineasDelError);
        }

        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    private static ResultadoArranque Iniciar(Dictionary<string, string?> configuracion)
    {
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());
        return ResultadoArranque.De(fabrica);
    }

    [Theory]
    [MemberData(nameof(FilasDeLaSpec))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_valor_invalido_impide_arrancar_con_la_linea_exacta(string caso, string clave, string valor, string linea)
    {
        Assert.NotEmpty(caso);

        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        AfirmarArranqueFallido(resultado, linea);
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181j_LLM_PRECIO_ENTRADA_ya_no_existe_y_un_valor_invalido_no_impide_arrancar()
    {
        var entorno = EntornoConLlave();
        entorno["LLM_PRECIO_ENTRADA"] = "-1";
        using var fabrica = new FabricaApi(variablesEntorno: entorno);

        var resultado = ResultadoArranque.De(fabrica);

        Assert.True(resultado.Arranco);
        Assert.DoesNotContain(resultado.LineasDeLaConsola, l => l.Contains("LLM_PRECIO_ENTRADA", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181n_Se_informan_todos_los_valores_invalidos_y_no_solo_el_primero()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            [RelevanciaMinima] = "0",
            [PresupuestoDiario] = "0"
        });

        AfirmarArranqueFallido(
            resultado,
            Invalida(RelevanciaMinima, "0", ReglaEntero1a10),
            Invalida(PresupuestoDiario, "0", ReglaPresupuesto));
        Assert.Equal(2, resultado.LineasDelError.Where(l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal)).Distinct().Count());
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181n_Se_informan_a_la_vez_un_limite_un_precio_y_un_codigo_del_catalogo()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            ["Limites:TimeoutLlmSegundos"] = "0",
            ["Llm:Proveedores:anthropic:Modelos:0:PrecioSalidaPorMillon"] = "-1",
            [CodigoBd] = "bd203"
        });

        AfirmarArranqueFallido(
            resultado,
            Invalida("Limites:TimeoutLlmSegundos", "0", "entero de 1 a 60"),
            Invalida("Llm:Proveedores:anthropic:Modelos:0:PrecioSalidaPorMillon", "-1", ReglaPrecio),
            Invalida(CodigoBd, "bd203", ReglaFormatoCodigo));
    }

    [Theory]
    [MemberData(nameof(LimitesFueraDeRango))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Cada_limite_fuera_de_su_rango_impide_arrancar_sin_recortarse(string clave, string valor, string regla)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        AfirmarArranqueFallido(resultado, Invalida(clave, valor, regla));
    }

    [Theory]
    [MemberData(nameof(FormatosEnterosInvalidos))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181d_Un_entero_con_otro_formato_es_invalido(string valor)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { ["Limites:MaximoInvocacionesHerramientas"] = valor });

        AfirmarArranqueFallido(resultado, Invalida("Limites:MaximoInvocacionesHerramientas", valor, "entero de 1 a 4"));
    }

    [Theory]
    [MemberData(nameof(DecimalesYBooleanosInvalidos))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_decimal_o_booleano_invalido_impide_arrancar(string clave, string valor, string regla)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        AfirmarArranqueFallido(resultado, Invalida(clave, valor, regla));
    }

    [Theory]
    [MemberData(nameof(ValoresConCaracteresEspeciales))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181x_El_valor_del_mensaje_se_escapa_y_se_trunca_a_64_caracteres(string caso, string valor, string valorEnElMensaje)
    {
        Assert.NotEmpty(caso);

        var resultado = Iniciar(new Dictionary<string, string?> { [RelevanciaMinima] = valor });

        AfirmarArranqueFallido(resultado, Invalida(RelevanciaMinima, valorEnElMensaje, ReglaEntero1a10));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181x_Un_salto_de_linea_real_en_el_codigo_no_falsea_la_consola()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [CodigoBd] = "BD-203\nArranque correcto" });

        AfirmarArranqueFallido(resultado, Invalida(CodigoBd, "BD-203\\nArranque correcto", ReglaFormatoCodigo));
        Assert.DoesNotContain(resultado.LineasDelError.Concat(resultado.LineasDeLaConsola), l => l.StartsWith("Arranque correcto", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_La_llave_nunca_aparece_en_el_error_de_arranque_aunque_llegue_por_configuracion()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            [RelevanciaMinima] = "0",
            ["LLM_API_KEY"] = LlaveFalsa
        });

        Assert.False(resultado.Arranco);
        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }
}
