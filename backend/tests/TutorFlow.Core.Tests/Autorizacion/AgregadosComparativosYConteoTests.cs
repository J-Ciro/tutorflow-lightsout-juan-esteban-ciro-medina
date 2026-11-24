using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class AgregadosComparativosYConteoTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    public static TheoryData<string> ComparativosYConteosConGrupo => new()
    {
        "¿Cómo me va frente a los otros estudiantes?",
        "¿Cuántos estudiantes hay en prueba académica?",
        "¿Cuántos de mis compañeros están en prueba académica?",
        "Mi promedio versus el de los demás"
    };

    public static TheoryData<string> NormativasSinExistencia => new()
    {
        "¿Cuántos estudiantes caben en una tutoría?",
        "¿Qué pasa con otros estudiantes que pierden la beca?",
        "¿Cuántos estudiantes pueden estar en prueba académica?"
    };

    public static TheoryData<string> ContrastesNormativos => new()
    {
        "¿Qué derechos tienen los estudiantes frente a una sanción?",
        "¿Cuántos estudiantes tienen derecho a la beca?"
    };

    [Theory]
    [MemberData(nameof(ComparativosYConteosConGrupo))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    [Trait("D", "D-75")]
    public void ESC_187q_Un_comparativo_o_un_conteo_de_existencia_con_grupo_se_deniega_como_agregado(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(NormativasSinExistencia))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    [Trait("D", "D-75")]
    public void ESC_187r_Caben_pueden_y_pierden_no_son_de_existencia_y_la_pregunta_normativa_se_permite(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
        Assert.Null(decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(ContrastesNormativos))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    [Trait("D", "D-75")]
    public void ESC_187s_Un_grupo_antes_de_frente_a_o_tienen_derecho_no_es_comparativo_ni_conteo_y_se_permite(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
        Assert.Null(decision.Motivo);
    }
}
