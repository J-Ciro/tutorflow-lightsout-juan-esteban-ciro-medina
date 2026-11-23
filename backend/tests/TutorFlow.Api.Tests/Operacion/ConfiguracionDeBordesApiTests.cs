using TutorFlow.Api.Tests.Dobles;
using static TutorFlow.Api.Tests.Dobles.ConfiguracionDePrueba;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ConfiguracionDeBordesApiTests
{
    private const string ReglaFormato = @"formato ^[A-Z]{2,4}-[0-9]{3}\z";
    private const string CodigoBd = "Autorizacion:Asignaturas:2:Codigo";
    private const string CodigoDeLista = "Autorizacion:CodigosAsignatura:0";
    private const string NombreEntrada6 = "Autorizacion:Asignaturas:6:Nombre";

    public static TheoryData<string, string, string> CodigosConFinDeTextoOAnchoCompleto => new()
    {
        { "y", "BD-203\n", "BD-203\\n" },
        { "z", "BD-２０３", "BD-２０３" },
        { "y-retorno", "BD-203\r\n", "BD-203\\r\\n" },
        { "z-arabe", "BD-٢٠٣", "BD-٢٠٣" }
    };

    public static TheoryData<string, string, string, string> ValoresConEspaciosAlBorde => new()
    {
        { "Conocimiento:RelevanciaMinima", " 5 ", " 5 ", "entero de 1 a 10" },
        { "Limites:PresupuestoDiarioUsd", "5 ", "5 ", "decimal mayor que 0 y hasta 1000" },
        { "Limites:BloquearMultiplesIdentidades", " true", " true", "true o false" },
        { "Conocimiento:RelevanciaMinima", "5\n", "5\\n", "entero de 1 a 10" },
        { "Limites:MaximoSeccionesRecuperadas", "\t3", "\\t3", "entero de 1 a 6" }
    };

    private static ResultadoArranque Iniciar(Dictionary<string, string?> configuracion)
    {
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: EntornoConLlave());
        return ResultadoArranque.De(fabrica);
    }

    private static List<string> LineasInvalidas(ResultadoArranque resultado) =>
        [.. resultado.LineasDelError.Where(l => l.StartsWith(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal)).Distinct()];

    private static void AfirmarArranqueFallido(ResultadoArranque resultado, params string[] lineas)
    {
        Assert.False(resultado.Arranco, "el arranque debió fallar antes de aceptar peticiones");
        foreach (var linea in lineas)
        {
            Assert.Contains(linea, resultado.LineasDelError);
        }

        Assert.DoesNotContain(LlaveFalsa, resultado.TextoCompleto, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CodigosConFinDeTextoOAnchoCompleto))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181yz_El_codigo_con_fin_de_linea_final_o_con_digitos_de_ancho_completo_es_invalido(string caso, string codigo, string codigoEnElMensaje)
    {
        Assert.NotEmpty(caso);

        var resultado = Iniciar(new Dictionary<string, string?> { [CodigoBd] = codigo });

        AfirmarArranqueFallido(resultado, Invalida(CodigoBd, codigoEnElMensaje, ReglaFormato));
        Assert.Single(LineasInvalidas(resultado));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181aa_Un_codigo_de_la_lista_separada_sin_formato_se_informa_solo_por_el_formato()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [CodigoDeLista] = "bd203" });

        AfirmarArranqueFallido(resultado, Invalida(CodigoDeLista, "bd203", ReglaFormato));
        Assert.Single(LineasInvalidas(resultado));
        Assert.DoesNotContain(resultado.LineasDelError, l => l.Contains("no está en Autorizacion:Asignaturas", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181ab_El_codigo_del_catalogo_sin_formato_y_el_de_la_lista_que_queda_sin_correspondencia_se_informan_ambos()
    {
        var resultado = Iniciar(new Dictionary<string, string?>
        {
            [CodigoBd] = "bd203",
            [CodigoDeLista] = "BD-203"
        });

        AfirmarArranqueFallido(
            resultado,
            Invalida(CodigoBd, "bd203", ReglaFormato),
            Invalida(CodigoDeLista, "BD-203", "no está en Autorizacion:Asignaturas"));
        Assert.Equal(2, LineasInvalidas(resultado).Count);
    }

    [Theory]
    [MemberData(nameof(ValoresConEspaciosAlBorde))]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181ac_Un_entero_decimal_o_booleano_con_espacios_o_control_al_borde_es_invalido_sin_recortarse(string clave, string valor, string valorEnElMensaje, string regla)
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [clave] = valor });

        AfirmarArranqueFallido(resultado, Invalida(clave, valorEnElMensaje, regla));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181ad_Un_nombre_ausente_se_informa_como_vacio_igual_que_uno_en_blanco()
    {
        var resultado = Iniciar(new Dictionary<string, string?> { [NombreEntrada6] = null });

        AfirmarArranqueFallido(resultado, Invalida(NombreEntrada6, string.Empty, "nombre vacío"));
        Assert.Single(LineasInvalidas(resultado));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181ae_El_valor_se_escapa_antes_de_truncarse_y_el_corte_puede_partir_la_secuencia_de_escape()
    {
        var codigo = new string('A', 63) + "\"B";

        var resultado = Iniciar(new Dictionary<string, string?> { [CodigoBd] = codigo });

        AfirmarArranqueFallido(resultado, Invalida(CodigoBd, new string('A', 63) + "\\…", ReglaFormato));
    }

    [Fact]
    [Trait("CA", "CA-15")]
    [Trait("ESC", "ESC-181")]
    [Trait("RF", "RNF-05")]
    public void ESC_181_Un_valor_con_par_sustituto_en_el_corte_se_trunca_en_63_caracteres()
    {
        var valor = new string('A', 63) + "😀B";

        var resultado = Iniciar(new Dictionary<string, string?> { [CodigoBd] = valor });

        AfirmarArranqueFallido(resultado, Invalida(CodigoBd, new string('A', 63) + "…", ReglaFormato));
    }
}
