using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class CitasD73Tests
{
    private const string DesgloseDeBasesDeDatos = "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.\nTotal: 2 faltas";
    private const string PreguntaFaltas ="¿Cuántas faltas tengo en Bases de Datos?";
    private const string PreguntaContabilidad = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaEstudiantesDelPrograma = "¿Cuántos estudiantes tiene mi programa?";
    private const string PreguntaDosMilVeintitres = "¿Qué decía el reglamento de 2023 sobre la habilitación?";
    private const string PreguntaReglamentoAnterior = "¿Qué requisitos tiene el examen de habilitación de una asignatura?";
    private const string TextoDelReglamentoDerogado = "El reglamento de 2023 fue derogado.";
    private const string FuenteDelArticulo3 = "fuente=\"reglamento_academico_2025.md · Artículo 3\"";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");
    private static readonly Cita Art3 = new(Reglamento, "Artículo 3");
    private static readonly Cita Art5 = new(Reglamento, "Artículo 5");
    private static readonly Cita Art11 = new(Reglamento, "Artículo 11");
    private static readonly Cita Sec3 = new(Evaluacion, "Sección 3");
    private static readonly Cita DatosNotas = new(Textos.DocumentoDatosEstudiante, "Notas");
    private static readonly Cita DatosAsistencia = new(Textos.DocumentoDatosEstudiante, "Asistencia");
    private static readonly Cita DatosProgramaAdm = new(Textos.DocumentoDatosPrograma, "ADM");
    private static readonly Cita DatosProgramaIngSis = new(Textos.DocumentoDatosPrograma, "ING-SIS");

    private static readonly SeccionDocumento Articulo3 = BaseConocimientoFalsa.Vigente().Secciones
        .Single(s => s.Documento == Reglamento && s.Seccion == "Artículo 3");

    private static IndiceFijo IndiceDeReglas(params SeccionDocumento[] entregadas) =>
        Indice(
            [Articulo3, NotaDefinitiva, PromedioPonderado, Articulo5, Articulo6, Articulo7, Articulo11, Articulo12, Articulo20, Articulo22, BecaPorRendimiento],
            entregadas);

    private static Cita[] Documentos(ResultadoPregunta resultado) =>
        [.. resultado.Respuesta!.Citas.Where(c => c.Documento is not (Textos.DocumentoDatosEstudiante or Textos.DocumentoDatosPrograma))];

    private static Func<SolicitudLlm, int, RespuestaLlm> LeeElProgramaYResponde(string texto) =>
        (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto(texto);

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177j_Las_citas_copiadas_de_las_reglas_aplicadas_sin_campo_usado_se_descartan_aunque_las_secciones_se_entreguen()
    {
        var texto = $"Bases de Datos: 2 faltas de 48 clases ({Evaluacion} · Sección 3; {Evaluacion} · Sección 4).";
        var escenario = Con(Fijo(texto), IndiceDeReglas(NotaDefinitiva, PromedioPonderado, Articulo11));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaFaltas);

        Assert.Equal(DesgloseDeBasesDeDatos, resultado.Respuesta!.Respuesta.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal([Art11], Documentos(resultado));
        Assert.Contains(DatosAsistencia, resultado.Respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177j_Las_citas_copiadas_se_descartan_tambien_cuando_las_secciones_no_se_entregaron()
    {
        var texto = $"Bases de Datos: 2 faltas de 48 clases ({Evaluacion} · Sección 3; {Evaluacion} · Sección 4).";
        var escenario = Con(Fijo(texto), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaFaltas);

        Assert.DoesNotContain(Sec3, resultado.Respuesta!.Citas);
        Assert.DoesNotContain(new Cita(Evaluacion, "Sección 4"), resultado.Respuesta.Citas);
        Assert.Equal([Art11], Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177k_Sin_campo_usado_el_coordinador_recibe_documento_vacio_y_solo_los_datos_del_programa()
    {
        var escenario = Con(Fijo("Tu programa tiene 3 estudiantes."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Coord01, PreguntaEstudiantesDelPrograma);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal("Tu programa tiene 3 estudiantes.", resultado.Respuesta!.Respuesta);
        Assert.Empty(Documentos(resultado));
        Assert.Equal([DatosProgramaIngSis], resultado.Respuesta.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-73")]
    public async Task ESC_198f2_Con_solo_la_cifra_de_la_definitiva_no_hay_campo_usado_ni_reglas_aplicadas_ni_respaldo_con_datos_leidos()
    {
        var escenario = Con(Fijo("3.9."), IndiceDeReglas(NotaDefinitiva, Articulo5));

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaContabilidad);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal("3.9.", resultado.Respuesta!.Respuesta);
        Assert.Empty(Documentos(resultado));
        Assert.Equal([DatosNotas, DatosAsistencia], resultado.Respuesta.Citas.ToArray());
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Theory]
    [InlineData("Contabilidad: 3.9.")]
    [InlineData("Tu definitiva en Contabilidad es 3.9.")]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-005")]
    [Trait("D", "D-73")]
    public async Task ESC_177c_El_par_asignatura_y_cifra_igual_a_la_definitiva_cita_solo_la_Seccion_3(string texto)
    {
        var escenario = Con(Fijo(texto), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaContabilidad);

        Assert.Equal([Sec3], Documentos(resultado));
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177c_Una_cifra_distinta_de_la_definitiva_no_forma_el_par_por_valor()
    {
        var escenario = Con(Fijo("Contabilidad: 4.1."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Est004, PreguntaContabilidad);

        Assert.DoesNotContain(Sec3, resultado.Respuesta!.Citas);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("ESC", "ESC-006")]
    [Trait("D", "D-73")]
    public async Task ESC_177d_Los_pares_1_8_y_2_3_son_definitivas_de_asignaturas_perdidas_y_citan_solo_el_Articulo_5()
    {
        var escenario = Con(LeeElProgramaYResponde("Valentina Castro: Microeconomía (1.8) y Estadística (2.3)."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Equal([Art5], Documentos(resultado));
        Assert.Equal([DatosProgramaAdm], resultado.Respuesta!.Citas.Where(c => c.Documento == Textos.DocumentoDatosPrograma).ToArray());
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Seccion is "Artículo 6" or "Artículo 20" or "Sección 3");
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177d_Sin_valores_ni_terminos_de_perdida_el_coordinador_no_recibe_el_Articulo_5()
    {
        var escenario = Con(LeeElProgramaYResponde("Valentina Castro tiene dos asignaturas con nota baja: Microeconomía y Estadística."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.DoesNotContain(Art5, resultado.Respuesta!.Citas);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task La_pregunta_o_los_datos_leidos_no_usan_el_campo_sin_que_la_respuesta_final_lo_haga()
    {
        var escenario = Con(LeeElProgramaYResponde("Revisa el detalle de tus estudiantes."), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Empty(Documentos(resultado));
        Assert.Equal([DatosProgramaAdm], resultado.Respuesta!.Citas.ToArray());
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-014")]
    [Trait("D", "D-73")]
    public async Task ESC_014_Con_la_palabra_2023_el_Articulo_3_se_entrega_aunque_no_se_haya_recuperado()
    {
        var escenario = Con(Fijo(TextoDelReglamentoDerogado), IndiceDeReglas());

        await escenario.PreguntarAsync(Est001, PreguntaDosMilVeintitres);

        Assert.Contains(FuenteDelArticulo3, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-014")]
    [Trait("D", "D-73")]
    public async Task ESC_014_El_Articulo_3_se_cita_por_tema_derogado_y_nunca_el_reglamento_2023()
    {
        var escenario = Con(Fijo(TextoDelReglamentoDerogado), IndiceDeReglas());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaDosMilVeintitres);

        Assert.Equal(TextoDelReglamentoDerogado, resultado.Respuesta!.Respuesta);
        Assert.Equal([Art3], Documentos(resultado));
        Assert.DoesNotContain(resultado.Respuesta.Citas, c => c.Documento.Contains("2023", StringComparison.Ordinal));
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-014")]
    [Trait("D", "D-73")]
    public async Task ESC_014_El_prompt_enviado_no_contiene_2023()
    {
        var escenario = Con(Fijo(TextoDelReglamentoDerogado), IndiceDeReglas());

        await escenario.PreguntarAsync(Est001, PreguntaDosMilVeintitres);

        Assert.DoesNotContain("2023", Assert.Single(escenario.Llm.Solicitudes).PromptSistema, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-014")]
    [Trait("D", "D-73")]
    public async Task Sin_la_palabra_2023_el_Articulo_3_no_se_entrega()
    {
        var escenario = Con(Fijo("El reglamento anterior fue derogado."), IndiceDeReglas(Articulo6));

        await escenario.PreguntarAsync(Est001, PreguntaReglamentoAnterior);

        Assert.DoesNotContain(FuenteDelArticulo3, escenario.Llm.TextoDeLlamada(0), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-014")]
    [Trait("D", "D-73")]
    public async Task Un_tema_de_derogacion_sin_el_Articulo_3_entregado_no_agrega_su_cita()
    {
        var escenario = Con(Fijo("El reglamento anterior fue derogado."), IndiceDeReglas(Articulo6));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaReglamentoAnterior);

        Assert.DoesNotContain(Art3, resultado.Respuesta!.Citas);
    }
}
