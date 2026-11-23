using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class NormalizacionV111Tests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est006 = new(Rol.Estudiante, "EST-006");

    public static TheoryData<string> TercerosDisfrazados => new()
    {
        "¿Cuáles son las notas de E S T 0 0 2?",
        "¿Cuáles son las notas de ES T002?",
        "¿Cuáles son las notas de EST 0 0 2?",
        "¿Cuáles son las notas de E S T - 0 0 2?",
        "¿Cuáles son las notas de e s t_002?",
        "¿Cuáles son las notas de E S T002?",
        "¿Cuáles son las notas de EST-٠٠٢?",
        "¿Cuáles son las notas de EST-۰۰۲?",
        "¿Cuáles son las notas de EST-००२?",
        "¿Cuáles son las notas de EST-০০২?",
        "¿Cuáles son las notas de EST-๐๐๒?",
        "cual es el promedio de andrés martĩnez",
        "cual es el promedio de andrés martǐnez",
        "cual es el promedio de andrẹs martinez",
        "cual es el promedio de andrếs martinez",
        "cual es el promedio de Vǎlentina Castro",
        "¿Cuáles son las notas de E\u202ES\u2066T-002?"
    };

    public static TheoryData<string> PreguntasPropiasOExceptuadas => new()
    {
        "mi id es E S T 0 0 1, ¿cuál es mi promedio?",
        "¿Qué pasó en el test 002 de Estadística?",
        "¿Cuál es mi nota en E S T 1 0 3?",
        "¿Cuál es mi nota en EST-١٠٣?",
        "¿Cuál es mi nota en la pestaña 2 del reglamento?"
    };

    public static TheoryData<string> HomoglifosYLetrasConTrazo => new()
    {
        "¿Cuáles son las notas de ЕST-002?",
        "¿Cuáles son las notas de EЅT-002?",
        "¿Cuáles son las notas de ESТ-002?",
        "¿Cuáles son las notas de ΕSΤ-002?",
        "¿Cuáles son las notas de ESŦ-002?",
        "¿Cuáles son las notas de ESŧ-002?",
        "¿Cuáles son las notas de đST-002?",
        "¿Cuál es el promedio de Аndrés Martínez?",
        "cual es el promedio de anđres martinez",
        "¿Cuáles son las notas de ESТ 0 0 2?"
    };

    public static TheoryData<string> PreguntasPropiasConPlegadoOCaracterLatin1 => new()
    {
        "¿Cuál es el promedio de ЕST-001?",
        "¿Cuál es el promedio de ΕSΤ-001?",
        "¿La definitiva es 0.3 × corte 1 más el resto?",
        "¿Qué significa el 1º corte de la asignatura?"
    };

    public static TheoryData<string, string> MarcasYDigitos => new()
    {
        { "ǎ", "a" },
        { "ĩ", "i" },
        { "ü", "u" },
        { "ñ", "n" },
        { "ǖ", "u" },
        { "ế", "e" },
        { "ạ", "a" },
        { "ạ́", "a" },
        { "eͅ", "e" },
        { "ó̂̃̄", "o" }
    };

    public static TheoryData<string> VariantesDeEst002 => new()
    {
        "EST-٠٠٢",
        "EST-۰۰۲",
        "EST-००२",
        "EST-০০২",
        "EST-๐๐๒",
        "EST-００２",
        "ÉST-002",
        "E\u00ADST-002",
        "E\u200DST-002",
        "E\u2060ST-002",
        "E\u202EST-002"
    };

    private static DecisionAcceso Evaluar(Usuario usuario, string pregunta) =>
        Politica.EvaluarPregunta(usuario, pregunta, Directorio);

    [Theory]
    [MemberData(nameof(TercerosDisfrazados))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-186")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_186_Un_id_o_un_nombre_disfrazado_con_marcas_digitos_de_otro_sistema_o_letras_sueltas_se_deniega_como_tercero(string pregunta)
    {
        var decision = Evaluar(Est001, pregunta);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(PreguntasPropiasOExceptuadas))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-186")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_186_Las_letras_sueltas_no_producen_falsos_403_con_el_propio_id_el_codigo_de_asignatura_o_palabras_comunes(string pregunta)
    {
        var decision = Evaluar(Est001, pregunta);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }

    [Theory]
    [MemberData(nameof(HomoglifosYLetrasConTrazo))]
    [Trait("CA", "CA-04")]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-186")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-61")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_199a_Los_homoglifos_y_las_letras_con_trazo_se_pliegan_y_deniegan_como_tercero(string pregunta)
    {
        var decision = Evaluar(Est001, pregunta);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(PreguntasPropiasConPlegadoOCaracterLatin1))]
    [Trait("CA", "CA-24")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    public void ESC_199a6a7_El_propio_ID_plegado_y_los_caracteres_Latin_1_no_producen_falsos_403(string pregunta)
    {
        var decision = Evaluar(Est001, pregunta);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-186")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_186f_La_normalizacion_convierte_la_E_cirilica_y_la_T_con_trazo_en_letras_latinas()
    {
        Assert.Equal(NormalizadorTexto.Normalizar("EST-002"), NormalizadorTexto.Normalizar("ЕST-002"));
        Assert.Equal(NormalizadorTexto.Normalizar("EST-002"), NormalizadorTexto.Normalizar("ESŦ-002"));
    }

    [Theory]
    [MemberData(nameof(MarcasYDigitos))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-196")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_196_Toda_marca_Mn_se_elimina_tras_la_descomposicion_canonica_completa(string entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorTexto.Normalizar(entrada));
    }

    [Theory]
    [MemberData(nameof(VariantesDeEst002))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-196")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_196_Los_digitos_Nd_de_cualquier_sistema_y_los_caracteres_de_formato_se_pliegan_al_mismo_id(string variante)
    {
        Assert.Equal(NormalizadorTexto.Normalizar("EST-002"), NormalizadorTexto.Normalizar(variante));
    }

    [Theory]
    [MemberData(nameof(VariantesDeEst002))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-196")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_196_La_normalizacion_de_las_variantes_es_idempotente(string variante)
    {
        var una = NormalizadorTexto.Normalizar(variante);

        Assert.Equal(una, NormalizadorTexto.Normalizar(una));
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-186")]
    [Trait("SEG", "SEG-04a")]
    public void ESC_186_El_propio_estudiante_puede_escribir_su_id_con_digitos_de_otro_sistema()
    {
        var decision = Evaluar(Est006, "¿Cuál es mi promedio? Soy EST-٠٠٦.");

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }
}
