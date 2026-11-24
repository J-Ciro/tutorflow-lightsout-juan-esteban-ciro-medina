using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class HallazgosDeSeguridadS05S06S07Tests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");

    public static TheoryData<string> AgregadosDeGrupo => new()
    {
        "¿Quién tiene la mejor definitiva en Bases de Datos?",
        "¿Cuántas personas en total están en prueba académica este semestre?",
        "¿Cuántos de mi salón perdieron Cálculo?",
        "¿Soy de los primeros de la cohorte?"
    };

    public static TheoryData<string> NormativasQueSiguenPermitidas => new()
    {
        "¿Qué estudiantes pueden pedir la beca de excelencia?",
        "¿Todos los estudiantes pueden cancelar materias?",
        "¿Cuántos estudiantes caben en una tutoría?",
        "¿Quién es el mejor contacto para pedir una beca?",
        "¿Está listo el calendario para los estudiantes?",
        "¿Qué lista de requisitos deben cumplir los estudiantes para la beca?",
        "¿Qué pasa con otros estudiantes que pierden la beca?",
        "¿Quién me dice mi mejor nota?",
        "¿Cuál es mi definitiva en Bases de Datos?",
        "¿En qué semestre cancelo Cálculo?",
        "¿Cómo se calcula la calificación definitiva?"
    };

    public static TheoryData<string> ProgramaAjenoDeCoord01 => new()
    {
        "¿Cuál es el promedio de Administración?",
        "estudiantes de administracion"
    };

    public static TheoryData<string> ProgramaPropioOPalabraParecida => new()
    {
        "¿Qué trámite administrativo se hace para cancelar una asignatura?",
        "¿cuándo es la admisión?",
        "¿Cuántos estudiantes de mi programa están en prueba académica?"
    };

    [Theory]
    [MemberData(nameof(AgregadosDeGrupo))]
    [Trait("Hallazgo", "S-05")]
    [Trait("CA", "CA-25")]
    public void S05_las_preguntas_de_grupo_con_vocabulario_nuevo_se_deniegan_como_agregado(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(NormativasQueSiguenPermitidas))]
    [Trait("Hallazgo", "S-05")]
    [Trait("CA", "CA-25")]
    public void S05_las_normativas_y_las_preguntas_propias_siguen_permitidas(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }

    [Theory]
    [InlineData("¿Qué te deja ver el sistema del coordinador?")]
    [InlineData("¿Qué te permite ver el sistema del coordinador?")]
    [InlineData("¿Qué acceso tiene el sistema del coordinador?")]
    [Trait("Hallazgo", "S-06")]
    [Trait("CA", "CA-04")]
    public void S06_la_capacidad_ajena_con_otro_verbo_se_deniega(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Est001, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.CapacidadAjena, decision.Motivo);
    }

    [Theory]
    [InlineData("¿Qué notas tiene Camilo R0jas?")]
    [InlineData("¿Qué nota tiene Camilo R0jas en Contabilidad?")]
    [InlineData("Camilo R0jas va bien")]
    [Trait("Hallazgo", "S-07")]
    [Trait("CA", "CA-04")]
    public void S07_un_nombre_con_cero_en_lugar_de_o_menciona_a_un_tercero(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Coord01, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(ProgramaAjenoDeCoord01))]
    [Trait("Hallazgo", "S-07")]
    [Trait("CA", "CA-04")]
    public void S07_el_nombre_corto_del_programa_ajeno_se_deniega_como_programa_ajeno(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Coord01, pregunta, Directorio);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.ProgramaAjeno, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(ProgramaPropioOPalabraParecida))]
    [Trait("Hallazgo", "S-07")]
    [Trait("CA", "CA-12")]
    public void S07_las_palabras_parecidas_a_un_programa_no_se_deniegan(string pregunta)
    {
        var decision = Politica.EvaluarPregunta(Coord01, pregunta, Directorio);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }
}
