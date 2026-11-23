using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api.Tests.Seguridad;

public sealed class NormalizacionV111ApiTests
{
    private const string PreguntaEnmascaradaDeNotas = "¿Cuáles son las notas de [TERCERO]?";
    private const string RespuestaFalsa = "Revisa tus notas.";

    public static TheoryData<string, string> TercerosDisfrazados => new()
    {
        { "¿Cuáles son las notas de E S T 0 0 2?", PreguntaEnmascaradaDeNotas },
        { "¿Cuáles son las notas de ES T002?", PreguntaEnmascaradaDeNotas },
        { "¿Cuáles son las notas de EST-٠٠٢?", PreguntaEnmascaradaDeNotas },
        { "¿Cuáles son las notas de EST-००२?", PreguntaEnmascaradaDeNotas },
        { "cual es el promedio de andrés martĩnez", "cual es el promedio de [TERCERO]" }
    };

    public static TheoryData<string, string> AgregadosDenegados => new()
    {
        { "a", "¿Quién tiene la nota más alta?" },
        { "b", "Dame el top 3 de promedios" },
        { "c", "¿Quién tiene la mejor nota de Bases de Datos?" }
    };

    public static TheoryData<string, string, string> PreguntasPropiasConSuperlativo => new()
    {
        { "d", "EST-001", "¿Cuál es mi nota más alta?" },
        { "e", "EST-005", "¿Cuál de mis asignaturas tiene la nota más baja?" },
        { "f", "EST-001", "¿Quién me ayuda a mejorar mi promedio?" }
    };

    public static TheoryData<string> HomoglifosYLetrasConTrazo => new()
    {
        "¿Cuáles son las notas de ЕST-002?",
        "¿Cuáles son las notas de ESŦ-002?"
    };

    [Theory]
    [MemberData(nameof(TercerosDisfrazados))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-186")]
    [Trait("SEG", "SEG-04a")]
    public async Task ESC_186_Un_tercero_disfrazado_responde_403_sin_llamar_al_llm_ni_leer_a_EST_002_y_se_audita_enmascarado(string pregunta, string preguntaEnmascarada)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Rechaza());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", pregunta);

        Afirmaciones.Negativa(respuesta, "estudiante");
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Equal(0, fabrica.Api.LecturasDatosDe("EST-002"));
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Denegada, evento.Resultado);
        Assert.Equal(MotivoDenegacion.TerceroMencionado, evento.MotivoDenegacion);
        Assert.Equal(preguntaEnmascarada, evento.Pregunta);
    }

    [Theory]
    [MemberData(nameof(HomoglifosYLetrasConTrazo))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-186")]
    [Trait("SEG", "SEG-04a")]
    public async Task ESC_186f_Riesgo_aceptado_un_homoglifo_llega_al_llm_pero_no_hay_fuga_porque_las_herramientas_estan_atadas_a_la_identidad(string pregunta)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => RespuestaFalsa), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(0, fabrica.Api.LecturasDatosDe("EST-002"));
        var enviado = string.Join("\n", fabrica.Llm.Solicitudes.SelectMany(s => s.Mensajes).Select(m => m.Contenido));
        Assert.DoesNotContain("Andrés Martínez", enviado, StringComparison.Ordinal);
        Assert.DoesNotContain("Andres Martinez", enviado, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(AgregadosDenegados))]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    public async Task ESC_187_Los_agregados_nuevos_responden_403_con_motivo_agregado_y_sin_llamar_al_llm(string caso, string pregunta)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Rechaza());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", pregunta);

        Afirmaciones.Negativa(respuesta, "estudiante");
        Assert.Equal(0, fabrica.Llm.Llamadas);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Denegada, evento.Resultado);
        Assert.Equal(MotivoDenegacion.Agregado, evento.MotivoDenegacion);
    }

    [Theory]
    [MemberData(nameof(PreguntasPropiasConSuperlativo))]
    [Trait("CA", "CA-12")]
    [Trait("ESC", "ESC-187")]
    [Trait("SEG", "SEG-05")]
    public async Task ESC_187_Un_superlativo_propio_o_mejorar_no_deniega_y_llega_al_llm(string caso, string usuario, string pregunta)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => RespuestaFalsa), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.True(fabrica.Llm.Llamadas >= 1);
    }
}
