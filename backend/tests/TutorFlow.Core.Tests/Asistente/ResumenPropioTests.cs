using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ResumenPropioTests
{
    private const string MarcaDelResumen = "Cálculo Integral";
    private const string LineaBasesDeDatos = "Bases de Datos";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");

    public static TheoryData<string> PreguntasConActivador => new()
    {
        "¿Cuál es mi promedio?",
        "¿Cuántas faltas tengo en Bases de Datos?",
        "Quiero ver mis notas",
        "¿Perdí alguna materia?",
        "¿Voy a conservar la beca?",
        "¿Cumplo los requisitos de la beca?",
        "¿Qué nota saqué en el primer corte?",
        "¿Cuántas inasistencias llevo?",
        "¿Estoy en prueba académica?",
        "¿Soy elegible para habilitar alguna asignatura?",
        "¿Cuál es 𝐦𝐢 𝐩𝐫𝐨𝐦𝐞𝐝𝐢𝐨?",
        "¿CUÁL ES MI PROMEDIO?"
    };

    public static TheoryData<string> PreguntasSinActivador => new()
    {
        "¿Hasta qué semana puedo cancelar una asignatura?",
        "¿Qué requisitos tiene la beca por rendimiento académico?",
        "¿Cuál es la nota mínima para aprobar una asignatura?",
        "¿Qué es la prueba académica?",
        "Tengo una pregunta sobre el reglamento",
        "¿Qué hay el miércoles con las notas de corte?",
        "Mis anotaciones sobre el reglamento",
        "¿Dónde queda Bienestar?"
    };

    public static TheoryData<string> PreguntasDeCoordinadorConMarcador => new()
    {
        "¿Cuál es mi promedio?",
        "¿Cuántas faltas tengo?",
        "Quiero ver mis notas"
    };

    [Theory]
    [MemberData(nameof(PreguntasConActivador))]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    public async Task Marcador_de_primera_persona_y_termino_de_datos_entregan_el_resumen_en_la_primera_llamada(string pregunta)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        await escenario.PreguntarAsync(Est001, pregunta);

        Assert.Contains(MarcaDelResumen, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(PreguntasSinActivador))]
    [Trait("ESC", "ESC-155")]
    [Trait("COMP", "COMP-07")]
    public async Task Sin_activador_no_se_entrega_el_resumen_ni_se_leen_datos(string pregunta)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        Assert.DoesNotContain(MarcaDelResumen, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.False(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("ESC", "ESC-155")]
    [Trait("COMP", "COMP-04")]
    public async Task Pregunta_normativa_sobre_la_beca_no_lee_datos_y_no_marca_uso_de_datos()
    {
        var seccion = new SeccionDocumento("bienestar_y_becas.md", "Sección 3", "Beca por rendimiento", "Exige buen promedio y créditos suficientes.");
        var escenario = Con(Fijo("Exige buen promedio y créditos suficientes (bienestar_y_becas.md · Sección 3)."), new IndiceFijo([seccion], [seccion]));

        var resultado = await escenario.PreguntarAsync(Est004, "¿Qué requisitos tiene la beca por rendimiento académico?");

        Assert.False(resultado.Respuesta!.UsoDatosEstudiante);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.Equal([new Cita("bienestar_y_becas.md", "Sección 3")], resultado.Respuesta.Citas.ToArray());
    }

    [Theory]
    [MemberData(nameof(PreguntasDeCoordinadorConMarcador))]
    [Trait("ESC", "ESC-155")]
    [Trait("COMP", "COMP-07")]
    public async Task El_coordinador_nunca_recibe_resumen(string pregunta)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        var resultado = await escenario.PreguntarAsync(Coord01, pregunta);

        Assert.DoesNotContain(MarcaDelResumen, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
        Assert.Empty(escenario.Api.LecturasNotas);
        Assert.Empty(escenario.Api.LecturasAsistencia);
        Assert.False(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    [Trait("SEG", "SEG-09")]
    public async Task El_resumen_va_delimitado_como_no_confiable_y_solo_con_datos_del_propio_estudiante()
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        var primera = escenario.Llm.TextoDeLlamada(0);
        Assert.Contains("DATOS_NO_CONFIABLES", primera, StringComparison.Ordinal);
        Assert.DoesNotContain("EST-002", primera, StringComparison.Ordinal);
        Assert.DoesNotContain("Andrés Martínez", primera, StringComparison.Ordinal);
        Assert.DoesNotContain("Camilo Rojas", primera, StringComparison.Ordinal);
        Assert.Equal(["EST-001"], escenario.Api.LecturasNotas.Distinct().ToArray());
        Assert.Equal(["EST-001"], escenario.Api.LecturasAsistencia.Distinct().ToArray());
    }

    [Theory]
    [InlineData("Cálculo Integral", "4.5")]
    [InlineData("Programación Orientada a Objetos", "4.8")]
    [InlineData("Bases de Datos", "4.1")]
    [InlineData("Inglés III", "4.0")]
    [Trait("ESC", "ESC-002")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    public async Task El_resumen_de_EST_001_trae_la_definitiva_de_cada_asignatura(string asignatura, string definitiva)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        var linea = LineasCon(escenario.Llm.TextoDeLlamada(0), asignatura);
        Assert.Matches(Palabra(definitiva), linea);
    }

    [Fact]
    [Trait("ESC", "ESC-002")]
    [Trait("COMP", "COMP-07")]
    public async Task El_resumen_de_EST_001_trae_el_promedio_del_periodo()
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Matches(Palabra("4.39"), escenario.Llm.TextoDeLlamada(0));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("48")]
    [InlineData("7")]
    [InlineData("5")]
    [Trait("ESC", "ESC-154")]
    [Trait("COMP", "COMP-07")]
    [Trait("RN", "RN-13")]
    public async Task El_resumen_trae_faltas_clases_maximo_y_restantes_de_Bases_de_Datos(string cifra)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));

        await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Bases de Datos?");

        var linea = LineasCon(escenario.Llm.TextoDeLlamada(0), LineaBasesDeDatos);
        Assert.Matches(Palabra(cifra), linea);
    }

    [Fact]
    [Trait("ESC", "ESC-154")]
    [Trait("CA", "CA-14")]
    [Trait("COMP", "COMP-07")]
    [Trait("COMP", "COMP-04")]
    public async Task Modelo_que_nunca_llama_herramientas_responde_con_el_resumen_en_una_sola_llamada()
    {
        var escenario = Con((solicitud, _) => Texto(CopiarLineas(solicitud, LineaBasesDeDatos)));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuántas faltas tengo en Bases de Datos?");

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Matches(Palabra("2"), resultado.Respuesta!.Respuesta);
        Assert.Matches(Palabra("48"), resultado.Respuesta.Respuesta);
        Assert.Contains(new Cita("Datos del estudiante", "Asistencia"), resultado.Respuesta.Citas);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Fact]
    [Trait("ESC", "ESC-002")]
    [Trait("COMP", "COMP-07")]
    [Trait("SEG", "SEG-10c")]
    public async Task Los_numeros_del_resumen_son_verificables_sin_invocar_herramientas()
    {
        var escenario = Con(Fijo("Tu promedio es 4.39: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1 e Inglés III 4.0."));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("COMP", "COMP-04")]
    public async Task Herramienta_de_datos_ejecutada_marca_uso_de_datos_aunque_no_haya_activador()
    {
        var escenario = Con((_, indice) => indice == 0 ? Herramienta(HerramientasAsistente.Notas) : Texto("Listo, ya revisé el reporte."));

        var resultado = await escenario.PreguntarAsync(Est001, "Reporte del periodo");

        Assert.True(resultado.Respuesta!.UsoDatosEstudiante);
    }

    private static Regex Palabra(string valor) => new($@"(?<![\d.]){Regex.Escape(valor)}(?![\d]|\.\d)");

    private static string LineasCon(string texto, string marca) =>
        string.Join("\n", texto.Split('\n').Where(linea => linea.Contains(marca, StringComparison.Ordinal)));

    private static string CopiarLineas(SolicitudLlm solicitud, string marca) =>
        LineasCon(string.Join("\n", solicitud.Mensajes.Select(m => m.Contenido)), marca);
}
