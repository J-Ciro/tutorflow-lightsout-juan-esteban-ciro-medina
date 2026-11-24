using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CitasPorTemaTests
{
    private const string PreguntaP4 = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string PreguntaP5 = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaNotaDefinitiva = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string PreguntaP8 = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string TextoDePesosYRedondeo = "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal.";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");

    private static readonly Cita Sec2 = new(Evaluacion, "Sección 2");
    private static readonly Cita Sec3 = new(Evaluacion, "Sección 3");
    private static readonly Cita Sec4 = new(Evaluacion, "Sección 4");
    private static readonly Cita Beca31 = new(Bienestar, "Sección 3.1");

    public static TheoryData<string, string, string, string> RespuestasQueUsanUnTermino => new()
    {
        { "peso", "La definitiva pondera 30% el corte 1.", "Sección 2", "30%" },
        { "pesos", "Los pesos de los tres cortes son 30%, 30% y 40%.", "Sección 2", "40%" },
        { "redondea", "La definitiva se redondea a una cifra decimal en cada corte.", "Sección 3", "redondea" },
        { "cifra decimal", "Cada corte pondera con una cifra decimal en la definitiva.", "Sección 3", "cifra decimal" }
    };

    private static IndiceFijo IndiceDeEvaluacion() =>
        Indice([NotaDefinitiva, Cortes, PromedioPonderado, Articulo5, BecaPorRendimiento], [NotaDefinitiva, Cortes, Articulo5]);

    private static Cita[] Documentos(ResultadoPregunta resultado) =>
        [.. resultado.Respuesta!.Citas.Where(c => c.Documento is not (Textos.DocumentoDatosEstudiante or Textos.DocumentoDatosPrograma))];

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198b_La_Seccion_2_entra_por_el_tema_pesos_junto_a_la_Seccion_3_escrita_y_el_Articulo_5_entregado_no_se_cita()
    {
        const string texto = "La nota definitiva se calcula como el promedio ponderado de los tres cortes, con los siguientes pesos: definitiva = 0.3 × corte 1 + 0.3 × corte 2 + 0.4 × corte 3. Mientras un corte no esté calificado, la asignatura no tendrá una nota definitiva (evaluacion_y_calificaciones.md · Sección 3).";
        var escenario = Con(Fijo(texto), IndiceDeEvaluacion());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaP4);

        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal([Sec3, Sec2], Documentos(resultado));
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198c_Sin_citas_escritas_las_secciones_por_tema_salen_en_el_orden_de_la_tabla_de_temas()
    {
        var escenario = Con(Fijo(TextoDePesosYRedondeo), IndiceDeEvaluacion());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaP4);

        Assert.Equal(TextoDePesosYRedondeo, resultado.Respuesta!.Respuesta);
        Assert.Equal([Sec2, Sec3], Documentos(resultado));
    }

    [Theory]
    [MemberData(nameof(RespuestasQueUsanUnTermino))]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198c_Cada_termino_de_la_lista_cerrada_agrega_su_seccion_si_esta_entregada_y_comparte_al_menos_dos_raices(string caso, string texto, string seccion, string termino)
    {
        Assert.NotEmpty(caso);
        Assert.Contains(termino, texto, StringComparison.OrdinalIgnoreCase);
        var escenario = Con(Fijo(texto), IndiceDeEvaluacion());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaP4);

        Assert.Contains(new Cita(Evaluacion, seccion), Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198d1_El_tema_pesos_con_una_sola_raiz_compartida_no_cita_la_Seccion_2()
    {
        const string texto = "Contabilidad: 3.9. La definitiva se calcula con los pesos.";
        var escenario = Con(SalidasVerificadas.LeeNotasYAsistenciaYResponde(texto), IndiceDeEvaluacion());

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaNotaDefinitiva);

        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal([Sec3], Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198d2_Con_solo_la_Seccion_3_1_entregada_el_respaldo_es_la_beca_y_la_respuesta_no_cambia()
    {
        const string texto = "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente.";
        var escenario = Con(Fijo(texto), Indice([BecaPorRendimiento, NotaDefinitiva, Cortes], [BecaPorRendimiento]));

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaP8);

        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal([Beca31], Documentos(resultado));
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198d3_Una_cita_por_tema_nunca_sale_de_una_seccion_no_entregada()
    {
        const string texto = "Tu promedio es 4.39 y se calcula con los pesos y se redondea.";
        var escenario = Con(SalidasVerificadas.LeeNotasYAsistenciaYResponde(texto), Indice([PromedioPonderado, NotaDefinitiva, Cortes], [PromedioPonderado]));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cómo se calcula el promedio ponderado?");

        Assert.Equal(texto, resultado.Respuesta!.Respuesta);
        Assert.Equal([Sec4], Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198_Las_negativas_y_los_textos_fijos_no_reciben_citas_por_tema()
    {
        var escenario = Con(Fijo("Andres Martinez sacó 2.6 en Cálculo con los pesos"), IndiceDeEvaluacion());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaP4);

        Assert.Equal([new Cita(Textos.DocumentoReglamentoVigente, Textos.SeccionPrivacidad)], resultado.Respuesta!.Citas.ToArray());
    }
}
