using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class AgregadosNuevosTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");

    public static TheoryData<string> AgregadosDeLaV111 => new()
    {
        "¿Quién tiene la nota más alta?",
        "Dame el top 3 de promedios",
        "¿Quién tiene la mejor nota de Bases de Datos?",
        "¿Quién tiene la peor nota del curso?",
        "¿Quién tiene el promedio más alto?",
        "¿Quién tiene los promedios más altos?",
        "¿Quién sacó las notas más altas?",
        "¿Quién tiene la nota más baja?",
        "¿Quién tiene el promedio más bajo?",
        "¿Quién tiene los promedios más bajos?",
        "¿Quién sacó las notas más bajas?",
        "Muéstrame las mejores notas",
        "Dime los mejores promedios",
        "Dime las peores notas",
        "Dime los peores promedios",
        "¿Cuál es el top de la clase?"
    };

    public static TheoryData<string> PreguntasPropiasSinAgregado => new()
    {
        "¿Cuál es mi nota más alta?",
        "¿Cuál de mis asignaturas tiene la nota más baja?",
        "¿Quién me ayuda a mejorar mi promedio?",
        "¿Puedo usar mi laptop en el examen?",
        "Mi mejor nota es de Cálculo, ¿quién la registra?"
    };

    [Theory]
    [MemberData(nameof(AgregadosDeLaV111))]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    public void ESC_187_Un_estudiante_que_pide_un_superlativo_sobre_otras_personas_o_un_top_se_deniega_como_agregado(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(PreguntasPropiasSinAgregado))]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    public void ESC_187_Un_superlativo_sin_quien_ni_top_o_con_mejorar_es_una_pregunta_propia_y_no_se_deniega(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est005, pregunta, Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }

    [Fact]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    public void ESC_187_La_capa_lexica_de_agregados_es_solo_del_rol_estudiante()
    {
        var decision = Politica.EvaluarPregunta(Coord01, "¿Quién tiene la nota más alta de mi programa?", Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }
}
