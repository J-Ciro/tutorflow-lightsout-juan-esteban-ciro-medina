using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class AgregadosD93Tests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    public static TheoryData<string> SuperlativosPluralesDelGrupo => new()
    {
        "¿Cuáles son las mejores notas de mi programa?"
    };

    public static TheoryData<string> SuperlativosPropios => new()
    {
        "¿Cuáles son mis mejores notas?",
        "Muéstrame mis peores notas",
        "¿Cuáles de mis asignaturas tienen las mejores notas?",
        "¿Cuáles son las mejores notas de mi semestre?"
    };

    public static TheoryData<string> SuperlativosDeCompaneros => new()
    {
        "Muéstrame las mejores notas de mis compañeros"
    };

    [Theory]
    [MemberData(nameof(SuperlativosPluralesDelGrupo))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    [Trait("D", "D-93")]
    public void ESC_187n_Un_superlativo_plural_con_mi_programa_se_deniega_como_agregado(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(SuperlativosPropios))]
    [Trait("CA", "CA-12")]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    [Trait("D", "D-93")]
    public void ESC_187o_Un_superlativo_plural_con_posesivo_propio_y_sin_grupo_se_permite(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
        Assert.Null(decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(SuperlativosDeCompaneros))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    [Trait("D", "D-93")]
    public void ESC_187p_Mis_no_exime_cuando_el_superlativo_plural_alude_a_los_companeros(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }
}
