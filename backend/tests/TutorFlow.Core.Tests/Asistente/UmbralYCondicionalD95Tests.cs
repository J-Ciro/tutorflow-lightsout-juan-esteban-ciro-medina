using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SalidasVerificadas;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class UmbralYCondicionalD95Tests
{
    private const string PreguntaPrueba = "¿Estoy en prueba académica?";
    private const string PreguntaBeca = "¿Puedo obtener la beca?";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    public static TheoryData<string> CondicionalesYNormativas => new()
    {
        "Si estás en prueba académica, aplica el Artículo 20.",
        "Cuando un estudiante está en prueba académica, aplica el Artículo 20.",
        "El estudiante que está en prueba académica sigue el Artículo 20."
    };

    public static TheoryData<string> AfirmacionesFalsasDePrueba => new()
    {
        "Te confirmo que estás en prueba académica.",
        "Sí, estás en prueba académica."
    };

    public static TheoryData<string> UmbralesFieles => new()
    {
        "El promedio mínimo es 3.0 y tu promedio es 4.39.",
        "Se requiere un promedio de 4.3; tu promedio es 4.39."
    };

    public static TheoryData<string> UmbralesFalsos => new()
    {
        "Tu promedio supera el mínimo, es 4.8.",
        "Como máximo tu promedio es 4.8."
    };

    private static IndiceFijo IndiceDePrueba() => Indice(Articulo20, PromedioPonderado, NotaDefinitiva);

    private static IndiceFijo IndiceDeBeca() => Indice(BecaPorRendimiento, PromedioPonderado, NotaDefinitiva);

    [Theory]
    [MemberData(nameof(CondicionalesYNormativas))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    [Trait("D", "D-95")]
    public async Task ESC_175_u1_u2_La_prueba_academica_condicional_o_normativa_no_se_verifica_contra_la_persona(string texto)
    {
        await ClasesVerificadoras.AfirmarPasaAsync("EST-001", texto, "EST-001");
    }

    [Theory]
    [MemberData(nameof(AfirmacionesFalsasDePrueba))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    [Trait("D", "D-95")]
    public async Task ESC_175_u3_Que_solo_o_Si_con_tilde_no_anulan_y_la_prueba_academica_falsa_da_estado_no_verificable(string texto)
    {
        await ClasesVerificadoras.AfirmarFallaAsync("EST-001", texto, "EST-001");
    }

    [Theory]
    [MemberData(nameof(UmbralesFieles))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    [Trait("D", "D-95")]
    public async Task ESC_179_aa1_El_promedio_minimo_y_el_se_requiere_son_umbral_y_el_promedio_propio_cuadra(string texto)
    {
        await ClasesVerificadoras.AfirmarPasaAsync("EST-001", texto, "EST-001");
    }

    [Theory]
    [MemberData(nameof(UmbralesFalsos))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("SEG", "SEG-10g")]
    [Trait("D", "D-95")]
    public async Task ESC_179_aa2_El_es_general_no_es_umbral_y_el_promedio_de_otra_asignatura_da_estado_no_verificable(string texto)
    {
        await ClasesVerificadoras.AfirmarFallaAsync("EST-001", texto, "EST-001");
    }
}
