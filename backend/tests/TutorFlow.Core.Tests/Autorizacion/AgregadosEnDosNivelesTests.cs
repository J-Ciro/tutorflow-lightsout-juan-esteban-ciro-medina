using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class AgregadosEnDosNivelesTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;
    private static readonly PoliticaAcceso Politica = new();
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    public static TheoryData<string, string> NormativasYAgregadasDeCa25 => new()
    {
        { "¿Qué estudiantes pueden pedir la beca de excelencia?", "¿En qué posición estoy en mi programa?" },
        { "¿Todos los estudiantes pueden cancelar materias?", "¿Cuántos compañeros perdieron Cálculo?" },
        { "¿Cuántos estudiantes caben en una tutoría?", "¿Qué alumnos perdieron Cálculo?" },
        { "¿Quién es el mejor contacto para pedir una beca?", "Who has the best GPA?" },
        { "¿Está listo el calendario para los estudiantes?", "List all students" }
    };

    public static TheoryData<string> AgregadasDeCa25 => new()
    {
        "¿En qué posición estoy en mi programa?",
        "¿Cuántos compañeros perdieron Cálculo?",
        "¿Qué alumnos perdieron Cálculo?",
        "Who has the best GPA?",
        "List all students"
    };

    public static TheoryData<string> NormativasDeCa25 => new()
    {
        "¿Qué estudiantes pueden pedir la beca de excelencia?",
        "¿Todos los estudiantes pueden cancelar materias?",
        "¿Cuántos estudiantes caben en una tutoría?",
        "¿Quién es el mejor contacto para pedir una beca?",
        "¿Está listo el calendario para los estudiantes?"
    };

    public static TheoryData<string> NormativasConPalabrasDeGrupo => new()
    {
        "¿Qué lista de requisitos deben cumplir los estudiantes para la beca?",
        "¿Qué pasa con otros estudiantes que pierden la beca?",
        "¿Quién me dice mi mejor nota?"
    };

    public static TheoryData<string> AgregadosDelNivel1 => new()
    {
        "¿En qué lugar quedé?",
        "¿Cuál es el promedio del programa?",
        "¿Cuál es la media de mi clase?",
        "Dame el top 3 de promedios",
        "¿Qué lugar ocupo en el curso?",
        "¿Cuántas personas sacaron más que yo?",
        "¿En qué percentil estoy?",
        "Muéstrame los nombres de los estudiantes que aprobaron"
    };

    public static TheoryData<string> AgregadosDelNivel2 => new()
    {
        "¿Quién tiene la nota más alta?",
        "¿Quién tiene la mejor nota de Bases de Datos?",
        "¿Quién tiene la peor nota del curso?",
        "¿Quiénes tienen el promedio más bajo?",
        "Who has the highest GPA?"
    };

    public static TheoryData<string> PreguntasPropiasConPalabraDebil => new()
    {
        "¿Cuál es mi nota más alta?",
        "¿Cuál es mi peor nota?",
        "¿Quién me dice mi mejor nota?",
        "¿Quién me ayuda a mejorar mi promedio?"
    };

    private static DecisionAcceso Evaluar(string pregunta) =>
        Politica.EvaluarPregunta(Est001, pregunta, Directorio);

    [Theory]
    [MemberData(nameof(AgregadosDelNivel1))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public void ESC_187_ghl_Una_palabra_fuerte_del_nivel_1_deniega_sola_como_agregado(string pregunta)
    {
        var decision = Evaluar(pregunta);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(AgregadosDelNivel2))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public void ESC_187_ac_Un_grupo_junto_a_una_palabra_debil_junto_a_nota_deniega_como_agregado(string pregunta)
    {
        var decision = Evaluar(pregunta);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(NormativasDeCa25))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public void ESC_187_i_Una_pregunta_normativa_con_un_grupo_y_sin_palabra_fuerte_no_se_deniega(string pregunta)
    {
        var decision = Evaluar(pregunta);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
        Assert.Null(decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(NormativasConPalabrasDeGrupo))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public void ESC_187_j_Sin_adyacencia_sin_tiempo_pasado_o_con_mi_mejor_la_pregunta_pasa(string pregunta)
    {
        var decision = Evaluar(pregunta);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
        Assert.Null(decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(PreguntasPropiasConPalabraDebil))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public void ESC_187_d_Una_palabra_debil_sin_grupo_es_una_pregunta_propia(string pregunta)
    {
        var decision = Evaluar(pregunta);

        Assert.True(decision.Permitido, $"motivo: {decision.Motivo}");
    }

    [Theory]
    [MemberData(nameof(NormativasYAgregadasDeCa25))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public void CA_25_La_normativa_pasa_y_la_agregada_se_deniega_con_la_misma_politica(string normativa, string agregada)
    {
        Assert.True(Evaluar(normativa).Permitido);
        var decision = Evaluar(agregada);

        Assert.False(decision.Permitido);
        Assert.Equal(MotivoDenegacion.Agregado, decision.Motivo);
    }

    [Theory]
    [MemberData(nameof(AgregadasDeCa25))]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public async Task ESC_187_k_La_agregada_da_403_sin_llamar_al_LLM_ni_leer_datos(string pregunta)
    {
        var escenario = Con(Fijo("No debería llamarse."));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        Assert.Equal(TipoResultadoPregunta.Denegada, resultado.Tipo);
        Assert.Equal(MotivoDenegacion.Agregado, resultado.Motivo);
        Assert.Empty(escenario.Llm.Solicitudes);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
    }

    [Fact]
    [Trait("CA", "CA-25")]
    [Trait("ESC", "ESC-187")]
    [Trait("D", "D-75")]
    public async Task ESC_187_m_El_residual_pasa_el_filtro_y_la_fuga_del_nombre_se_bloquea_sin_leer_a_otros()
    {
        var escenario = Con(Fijo("Andrés Martínez va primero."));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Soy el primero de mi clase?");

        Assert.Equal(TipoResultadoPregunta.Respondida, resultado.Tipo);
        Assert.Equal(Textos.NegativaEstudiante, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.FugaBloqueada, resultado.Auditoria);
        Assert.All(escenario.Api.LecturasNotas, id => Assert.Equal("EST-001", id));
        Assert.All(escenario.Api.LecturasAsistencia, id => Assert.Equal("EST-001", id));
    }
}
