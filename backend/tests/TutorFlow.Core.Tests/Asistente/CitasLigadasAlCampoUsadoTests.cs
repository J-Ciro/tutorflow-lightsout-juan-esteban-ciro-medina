using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CitasLigadasAlCampoUsadoTests
{
    private const string PreguntaNormativa = "¿Qué dice el reglamento sobre la habilitación?";
    private const string PreguntaQ10Dos = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaQ10Seis = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string LineaDeDefinitivas = "Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0.";
    private const string Sec3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Sec4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Art5 = "reglamento_academico_2025.md · Artículo 5";
    private const string Art6 = "reglamento_academico_2025.md · Artículo 6";
    private const string Art7 = "reglamento_academico_2025.md · Artículo 7";
    private const string Art11 = "reglamento_academico_2025.md · Artículo 11";
    private const string Art12 = "reglamento_academico_2025.md · Artículo 12";
    private const string Art20 = "reglamento_academico_2025.md · Artículo 20";
    private const string Beca31 = "bienestar_y_becas.md · Sección 3.1";
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    public static TheoryData<string, string, string, string[]> CampoUsadoEnLaRespuesta => new()
    {
        { "EST-001", PreguntaQ10Dos, $"Tu promedio es 4.39. {LineaDeDefinitivas}", [Sec4, Sec3] },
        { "EST-001", PreguntaQ10Dos, $"Tu promedio ponderado del periodo es 4.39. {LineaDeDefinitivas}", [Sec4, Sec3] },
        { "EST-001", PreguntaQ10Dos, $"4.39. {LineaDeDefinitivas}", [Sec4, Sec3] },
        { "EST-001", PreguntaQ10Dos, "Tu resultado del periodo es 4.39.", [Sec4, Sec3] },
        { "EST-001", "¿Cuántas faltas tengo en Bases de Datos?", "Bases de Datos: 2 faltas de 48 clases.", [Art11] },
        { "EST-005", "¿Estoy en prueba académica?", "Sí, estás en prueba académica.", [Art20, Sec4] },
        { "EST-005", "¿Estoy en prueba académica?", "Sí, estás en prueba académica: tu promedio es 2.65.", [Art20, Sec4] },
        { "EST-005", "¿Qué materias puedo habilitar?", "Puedes habilitar Estadística con 2.3.", [Art6, Sec3, Art5] },
        { "EST-003", "¿Cuántas faltas tengo en Inglés III?", "Solicita la revisión a tu coordinador.", [Art11, Art12] },
        { "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "Tu definitiva en Contabilidad es 3.9.", [Sec3] },
        { "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "Contabilidad: 3.9. La definitiva se calcula con los pesos de evaluación_y_calificaciones.md.", [Sec3] },
        { "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "Contabilidad: 3.9.", [Sec3] }
    };

    public static TheoryData<string, string> SinCampoUsado => new()
    {
        { "EST-004", "3.9." },
        { "EST-001", "Revisa el detalle de tus asignaturas." }
    };

    private static IndiceFijo IndiceDeReglas() =>
        Indice([NotaDefinitiva, PromedioPonderado, Articulo5, Articulo6, Articulo7, Articulo11, Articulo12, Articulo20, Articulo22, BecaPorRendimiento], []);

    private static IndiceFijo IndiceNormativo() =>
        Indice([NotaDefinitiva, PromedioPonderado, Articulo5, Articulo6, Articulo7, Articulo11, Articulo12, Articulo20, Articulo22, BecaPorRendimiento], [Articulo6]);

    private static string[] Documentos(ResultadoPregunta resultado) =>
        [.. resultado.Respuesta!.Citas
            .Where(c => c.Documento is not (Textos.DocumentoDatosEstudiante or Textos.DocumentoDatosPrograma))
            .Select(c => $"{c.Documento} · {c.Seccion}")];

    private static Usuario Estudiante(string id) => new(Rol.Estudiante, id);

    [Theory]
    [MemberData(nameof(CampoUsadoEnLaRespuesta))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task ESC_194_Si_la_respuesta_usa_el_campo_de_una_regla_solo_se_citan_esas_reglas(string usuario, string pregunta, string texto, string[] esperadas)
    {
        var escenario = Con(Fijo(texto), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Estudiante(usuario), pregunta);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(esperadas, Documentos(resultado));
    }

    [Theory]
    [MemberData(nameof(SinCampoUsado))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-73")]
    public async Task ESC_194_Sin_campo_usado_no_se_cita_ninguna_regla_aplicada(string usuario, string texto)
    {
        var escenario = Con(SalidasVerificadas.LeeNotasYAsistenciaYResponde(texto), IndiceNormativo());

        var resultado = await escenario.PreguntarAsync(Estudiante(usuario), PreguntaNormativa);

        Assert.Empty(Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task ESC_194g_Nombrar_la_beca_sin_datos_de_beca_no_cita_secciones_de_fuera_del_bloque()
    {
        var texto = "Para la beca se revisa con Bienestar.";
        var escenario = Con(Fijo(texto), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Contains("Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.Equal([Art5], Documentos(resultado));
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Seccion.StartsWith("Sección 3.", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task ESC_194_Con_aprobadas_en_la_respuesta_del_coordinador_solo_se_cita_el_Articulo_5()
    {
        var escenario = Con((_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto("Camilo Rojas tiene todas sus asignaturas aprobadas."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Equal([Art5], Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task ESC_194_Falta_en_singular_no_es_un_termino_y_sin_raices_compartidas_no_hay_citas_de_reglas()
    {
        var escenario = Con(SalidasVerificadas.LeeNotasYAsistenciaYResponde("Estadística: le falta calificar el corte 3."), IndiceNormativo());

        var resultado = await escenario.PreguntarAsync(Estudiante("EST-006"), PreguntaNormativa);

        Assert.Empty(Documentos(resultado));
        Assert.Equal([new Cita(Textos.DocumentoDatosEstudiante, "Notas"), new Cita(Textos.DocumentoDatosEstudiante, "Asistencia")], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task ESC_194_Un_termino_cuya_seccion_no_esta_en_el_bloque_no_agrega_nada()
    {
        var escenario = Con(Fijo("No estás en prueba académica: tu promedio es 4.39."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Estudiante("EST-001"), "¿Estoy en prueba académica?");

        Assert.Equal([Sec4], Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-49")]
    public async Task ESC_194e_La_cita_escrita_al_Articulo_20_no_entregada_se_descarta_y_el_respaldo_es_la_Seccion_3_1()
    {
        var texto = $"Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente ({Art20}).";
        var escenario = Con(Fijo(texto), Indice([Articulo20, BecaPorRendimiento, BecaSocioeconomica], [BecaPorRendimiento]));

        var resultado = await escenario.PreguntarAsync(Estudiante("EST-004"), "¿Qué requisitos tiene la beca por rendimiento académico?");

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal([Beca31], Documentos(resultado));
        Assert.DoesNotContain("Artículo 20", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }
}
