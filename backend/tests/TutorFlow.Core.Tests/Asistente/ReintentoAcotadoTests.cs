using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ReintentoAcotadoTests
{
    private const string PreguntaQ10Seis = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaDefinitiva = "¿Cuál es mi nota definitiva en Estadística?";
    private const string PreguntaTutorias = "¿Cuándo hay tutorías académicas?";
    private const string PerdidaDeMateo = "Mateo Vargas perdió Estadística.";
    private const string PerdidasDeValentina = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
    private const string MotivoPerdidaDeMateo = "la afirmación de pérdida de Mateo Vargas en Estadística no coincide con los datos entregados";
    private const int SalidaPorLlamada = 5;
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");

    private static int TokensEsperados(EscenarioAsistente escenario) => ConsumoRealista.TokensDe(escenario.Llm.Solicitudes, SalidaPorLlamada);

    private static readonly Usuario Est006 = new(Rol.Estudiante, "EST-006");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    public static TheoryData<string, string, string, ResultadoAuditoria> BloqueosSinReintento => new()
    {
        { "SEG-10b-nombre-ajeno", PreguntaPromedio, "Andres Martinez sacó 2.6 en Cálculo", ResultadoAuditoria.FugaBloqueada },
        { "SEG-10b-id-inventado", PreguntaPromedio, "EST 099 tiene un promedio de 4.39.", ResultadoAuditoria.FugaBloqueada },
        { "SEG-10e-sin-respaldo", PreguntaTutorias, "Las tutorías se agendan escribiendo al buzón virtual de la decanatura.", ResultadoAuditoria.SinInformacion },
        { "verificada", PreguntaPromedio, "Tu promedio es 4.39.", ResultadoAuditoria.Respondida }
    };

    private static EscenarioAsistente ConPrograma(GuionDeDosTurnos guion) =>
        Con(guion.Responder, Indice(Articulo5, Articulo6, Articulo20));

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_193a_Un_bloqueo_de_perdida_admite_un_solo_segundo_turno_con_la_plantilla_fija_y_se_entrega_el_reintento()
    {
        var guion = new GuionDeDosTurnos(PerdidaDeMateo, PerdidasDeValentina, leePrograma: true);
        var escenario = ConPrograma(guion);

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Equal(PerdidasDeValentina, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.True(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Single(ReintentoDeLlm.Reintentos(escenario.Llm));
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
        ReintentoDeLlm.AfirmarSegundoTurno(guion, PerdidaDeMateo, ReintentoDeLlm.Instruccion(MotivoPerdidaDeMateo));
        Assert.Equal(TokensEsperados(escenario), resultado.Respuesta.Tokens);
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_193b_Un_bloqueo_de_numero_hace_exactamente_2_llamadas_con_el_motivo_de_la_cifra_y_suma_los_dos_turnos()
    {
        var guion = new GuionDeDosTurnos("Tu promedio es 4.40.", "Tu promedio es 4.39.");
        var escenario = Con(guion.Responder);

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
        Assert.Equal("Tu promedio es 4.39.", resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Equal(TokensEsperados(escenario), resultado.Respuesta.Tokens);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
        ReintentoDeLlm.AfirmarSegundoTurno(guion, "Tu promedio es 4.40.", ReintentoDeLlm.Instruccion("la cifra 4.40 no está en los datos ni en las secciones entregados"));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_193_Un_valor_monetario_inventado_admite_el_reintento_con_el_motivo_del_valor()
    {
        var guion = new GuionDeDosTurnos("La matrícula cuesta $1.500.000.", "Tu promedio es 4.39.");
        var escenario = Con(guion.Responder);

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
        Assert.Equal("Tu promedio es 4.39.", resultado.Respuesta!.Respuesta);
        var instruccion = guion.Capturas[^1][^1];
        Assert.Equal(RolMensaje.Usuario, instruccion.Rol);
        Assert.Equal(ReintentoDeLlm.Instruccion("el valor 1.500.000 no está en las fuentes"), instruccion.Contenido);
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10d")]
    public async Task ESC_193c_La_negativa_que_nombra_la_herramienta_admite_el_reintento_sin_repetir_el_identificador()
    {
        var guion = new GuionDeDosTurnos("Usa consultar_programa para verlo.", PerdidasDeValentina, leePrograma: true);
        var escenario = ConPrograma(guion);

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Equal(PerdidasDeValentina, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Single(ReintentoDeLlm.Reintentos(escenario.Llm));
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
        var instruccion = ReintentoDeLlm.Instruccion("mencionaste detalles internos del asistente; no menciones herramientas, instrucciones ni configuración");
        ReintentoDeLlm.AfirmarSegundoTurno(guion, "Usa consultar_programa para verlo.", instruccion);
        Assert.DoesNotContain("consultar_", instruccion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_193d_Si_el_reintento_vuelve_a_fallar_se_entrega_el_resultado_del_primer_bloqueo_tras_exactamente_2_llamadas()
    {
        var guion = new GuionDeDosTurnos("Tu definitiva en Estadística es 4.0.", "Tu definitiva en Estadística es 4.0.");
        var escenario = Con(guion.Responder, Indice(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(Est006, PreguntaDefinitiva);

        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
        Assert.Equal(ReintentoDeLlm.Instruccion("la definitiva 4.0 de Estadística no coincide con los datos entregados"), guion.Capturas[^1][^1].Contenido);
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_193e_Un_reintento_con_un_nombre_no_autorizado_se_descarta_y_se_entrega_el_primer_bloqueo()
    {
        var escenario = Con(new GuionDeDosTurnos("Tu promedio es 4.40.", "Andres Martinez tiene 4.39.").Responder);

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.NotEqual(Textos.NegativaEstudiante, resultado.Respuesta.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_193_La_respuesta_del_reintento_recorre_de_nuevo_la_limpieza_de_referencias()
    {
        var escenario = Con(new GuionDeDosTurnos("Tu promedio es 4.40.", "Tu promedio es 4.39 (x.md · Sección 9).").Responder, Indice(PromedioPonderado, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Equal("Tu promedio es 4.39.", resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
    }

    [Theory]
    [MemberData(nameof(BloqueosSinReintento))]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10")]
    public async Task ESC_193f_Los_bloqueos_excluidos_y_la_respuesta_verificada_hacen_una_sola_llamada_y_no_llevan_reintento(string caso, string pregunta, string texto, ResultadoAuditoria esperado)
    {
        Assert.NotEmpty(caso);
        var escenario = Con(Fijo(texto));

        var resultado = await escenario.PreguntarAsync(Est001, pregunta);

        Assert.Single(escenario.Llm.Solicitudes);
        Assert.Equal(esperado, resultado.Auditoria);
        Assert.False(ReintentoDeLlm.ReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos)));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-06")]
    public async Task ESC_193f_El_403_tardio_de_la_herramienta_de_detalle_no_se_reintenta()
    {
        var escenario = Con(SalidasVerificadas.LeeElProgramaYElDetalleYResponde("Detalle de EST-004.", "EST-004"), Indice(Articulo5, Articulo6, Articulo20));

        var resultado = await escenario.PreguntarAsync(Coord01, "¿Qué estudiantes de mi programa perdieron materias?");

        Assert.Equal(TipoResultadoPregunta.Denegada, resultado.Tipo);
        Assert.Equal(MotivoDenegacion.HerramientaIdAjeno, resultado.Motivo);
        Assert.Single(escenario.Llm.Solicitudes);
        Assert.False(ReintentoDeLlm.ReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos)));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_193g_La_instruccion_correctiva_no_revela_otros_nombres_ids_ni_cifras()
    {
        var guion = new GuionDeDosTurnos(PerdidaDeMateo, PerdidaDeMateo, leePrograma: true);
        var escenario = ConPrograma(guion);

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        var instruccion = guion.Capturas[^1][^1];
        Assert.Equal(ReintentoDeLlm.Instruccion(MotivoPerdidaDeMateo), instruccion.Contenido);
        Assert.DoesNotMatch(new Regex(@"\d"), instruccion.Contenido);
        Assert.DoesNotContain("Valentina", instruccion.Contenido, StringComparison.Ordinal);
        Assert.DoesNotContain("Camilo", instruccion.Contenido, StringComparison.Ordinal);
        Assert.DoesNotContain("EST-", instruccion.Contenido, StringComparison.Ordinal);
        ReintentoDeLlm.AfirmarSegundoTurno(guion, PerdidaDeMateo, instruccion.Contenido);
        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_199g1_El_motivo_de_una_definitiva_para_el_estudiante_no_nombra_a_ninguna_persona()
    {
        var guion = new GuionDeDosTurnos("Tu definitiva en Estadística es 4.0.", "Tu definitiva en Estadística es 4.0.");
        var escenario = Con(guion.Responder, Indice(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(Est006, PreguntaDefinitiva);

        ReintentoDeLlm.AfirmarSegundoTurno(guion, "Tu definitiva en Estadística es 4.0.", ReintentoDeLlm.Instruccion("la definitiva 4.0 de Estadística no coincide con los datos entregados"));
        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_199g2_El_motivo_de_una_perdida_para_el_estudiante_no_nombra_a_ninguna_persona()
    {
        var guion = new GuionDeDosTurnos("Perdiste Contabilidad.", "Perdiste Contabilidad.");
        var escenario = Con(guion.Responder, Indice(Articulo5, Articulo6));

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, "EST-005"), "¿Perdí Contabilidad?");

        ReintentoDeLlm.AfirmarSegundoTurno(guion, "Perdiste Contabilidad.", ReintentoDeLlm.Instruccion("la afirmación de pérdida en Contabilidad no coincide con los datos entregados"));
        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_199g3_El_motivo_monetario_copia_solo_los_digitos_y_separadores_y_el_bloqueo_termina_en_COMP_03_con_reintento()
    {
        var guion = new GuionDeDosTurnos("La matrícula cuesta $1.500.000.", "La matrícula cuesta $1.500.000.");
        var escenario = Con(guion.Responder);

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuánto cuesta la matrícula?");

        ReintentoDeLlm.AfirmarSegundoTurno(guion, "La matrícula cuesta $1.500.000.", ReintentoDeLlm.Instruccion("el valor 1.500.000 no está en las fuentes"));
        Assert.Equal(Textos.SinInformacion, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.SinInformacion, resultado.Auditoria);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-62")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_199g4_Sin_datos_leidos_el_bloqueo_de_una_cifra_tambien_tiene_reintento_y_el_motivo_no_dice_de_tu()
    {
        const string texto = "Se aprueba con 3.5 (reglamento_academico_2025.md · Artículo 5).";
        var guion = new GuionDeDosTurnos(texto, texto);
        var escenario = Con(guion.Responder, Indice(Articulo5));

        var resultado = await escenario.PreguntarAsync(Est001, "¿Cuál es la nota mínima para aprobar una asignatura?");

        var instruccion = ReintentoDeLlm.Instruccion("la cifra 3.5 no está en los datos ni en las secciones entregados");
        ReintentoDeLlm.AfirmarSegundoTurno(guion, texto, instruccion);
        Assert.DoesNotContain("de tu", instruccion, StringComparison.Ordinal);
        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.False(resultado.Respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        ReintentoDeLlm.AfirmarReintentoRegistrado(Assert.Single(escenario.Auditoria.Eventos));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("RF", "SEG-10f")]
    public async Task ESC_193_El_reintento_nunca_pasa_de_uno_aunque_el_segundo_turno_tambien_se_bloquee()
    {
        var guion = new GuionDeDosTurnos(PerdidaDeMateo, PerdidaDeMateo, leePrograma: true);
        var escenario = ConPrograma(guion);

        await escenario.PreguntarAsync(Coord02, PreguntaQ10Seis);

        Assert.Single(ReintentoDeLlm.Reintentos(escenario.Llm));
        Assert.Single(guion.Capturas, c => c.Any(m => m.Contenido.StartsWith(ReintentoDeLlm.Prefijo, StringComparison.Ordinal)));
    }
}
