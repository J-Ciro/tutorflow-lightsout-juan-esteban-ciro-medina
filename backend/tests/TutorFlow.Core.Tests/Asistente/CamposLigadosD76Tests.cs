using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed partial class CamposLigadosD76Tests
{
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";
    private const string PreguntaPromedioPonderado = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaDefinitivaEstadistica = "¿Cuál es mi nota definitiva en Estadística?";
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";

    public static TheoryData<string, string, string, ResultadoAuditoria> EstudianteConCifraOEstadoFalso => new()
    {
        { "EST-001", PreguntaPromedio, "Tu promedio es 1,23.", ResultadoAuditoria.NumeroNoVerificado },
        { "EST-001", PreguntaPromedio, "Tu definitiva en Cálculo Integral es 4,1.", ResultadoAuditoria.EstadoNoVerificado },
        { "EST-001", PreguntaPromedioPonderado, "Tu promedio es 4.4.", ResultadoAuditoria.EstadoNoVerificado },
        { "EST-001", PreguntaPromedio, "Tu promedio es 4.39 y reprobaste Bases de Datos.", ResultadoAuditoria.EstadoNoVerificado },
        { "EST-006", "¿Aprobé Estadística?", "Ya aprobaste Estadística.", ResultadoAuditoria.EstadoNoVerificado },
        { "EST-006", PreguntaDefinitivaEstadistica, "Tu nota final en Estadística es 4.0.", ResultadoAuditoria.EstadoNoVerificado },
        { "EST-006", PreguntaDefinitivaEstadistica, "Tu calificación final de Estadística quedó en 4.0.", ResultadoAuditoria.EstadoNoVerificado },
        { "EST-001", "¿Cómo voy en Bases de Datos?", "Llevas 10 faltas en Bases de Datos.", ResultadoAuditoria.EstadoNoVerificado }
    };

    public static TheoryData<string, string, string> EstudianteConCifraOEstadoCorrecto => new()
    {
        { "EST-001", PreguntaPromedio, "Tu promedio es 4,39." },
        { "EST-001", PreguntaPromedioPonderado, "Tu promedio es 4.39." },
        { "EST-004", "¿Aprobé Estadística?", "Ya aprobaste Estadística." },
        { "EST-001", "¿Cómo voy en Bases de Datos?", "Llevas 2 faltas; el corte 3 ya está calificado." },
        { "EST-001", "¿Cómo voy en Bases de Datos?", "Te quedan 0 faltas." }
    };

    public static TheoryData<string> CoordinadorConEstadoFalso => new()
    {
        "Mateo Vargas reprobó Estadística.",
        "Mateo Vargas no aprobó Estadística.",
        "Mateo Vargas: Estadística (reprobada).",
        "Mateo Vargas está en prueba académica."
    };

    public static TheoryData<string> CoordinadorConEstadoCorrecto => new()
    {
        "Valentina Castro está en prueba académica."
    };

    private static IndiceFijo IndiceDeNormas() => Indice(NotaDefinitiva, PromedioPonderado, Articulo5, Articulo6, Articulo20);

    private static void AfirmarNoVerificado(ResultadoPregunta resultado, EscenarioAsistente escenario, ResultadoAuditoria esperado)
    {
        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(esperado, resultado.Auditoria);
        var evento = Assert.Single(escenario.Auditoria.Eventos);
        Assert.Equal(esperado, evento.Resultado);
        ReintentoDeLlm.AfirmarReintentoRegistrado(evento);
    }

    private static string RespuestaDeEjemplo(string caso) =>
        RespuestaDelCaso().Match(caso).Groups["respuesta"].Value;

    [GeneratedRegex(@"Respuesta:\s*(?<respuesta>.*)", RegexOptions.Singleline | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RespuestaDelCaso();

    [GeneratedRegex(@"\b(tu|tus|tienes|te)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SegundaPersona();

    [Theory]
    [MemberData(nameof(EstudianteConCifraOEstadoFalso))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-76")]
    public async Task CA_26_La_cifra_o_el_estado_que_no_cuadra_con_el_campo_ligado_da_no_verificable_con_reintento(string usuario, string pregunta, string texto, ResultadoAuditoria resultadoEsperado)
    {
        var escenario = Con(Fijo(texto), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, usuario), pregunta);

        AfirmarNoVerificado(resultado, escenario, resultadoEsperado);
        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
    }

    [Theory]
    [MemberData(nameof(EstudianteConCifraOEstadoCorrecto))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-76")]
    public async Task CA_26_La_coma_decimal_el_promedio_exacto_y_el_estado_verdadero_se_entregan(string usuario, string pregunta, string texto)
    {
        var escenario = Con(Fijo(texto), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, usuario), pregunta);

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
        Assert.Single(escenario.Llm.Solicitudes);
    }

    [Theory]
    [MemberData(nameof(CoordinadorConEstadoFalso))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-76")]
    public async Task ESC_175_st_El_estado_de_un_estudiante_del_programa_que_los_datos_contradicen_da_no_verificable(string texto)
    {
        var escenario = Con(Fijo(texto), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, "COORD-02"), PreguntaPerdidas);

        AfirmarNoVerificado(resultado, escenario, ResultadoAuditoria.EstadoNoVerificado);
    }

    [Theory]
    [MemberData(nameof(CoordinadorConEstadoCorrecto))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-76")]
    public async Task ESC_175_t_La_prueba_academica_de_quien_tiene_promedio_menor_a_3_se_entrega(string texto)
    {
        var escenario = Con(Fijo(texto), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, "COORD-02"), PreguntaPerdidas);

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
    }

    [Fact]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-76")]
    public async Task ESC_179_x_La_voz_en_segunda_persona_sobre_datos_de_un_estudiante_nombrado_por_el_coordinador_falla()
    {
        const string texto = "Tu promedio es 2.65. Definitivas: Microeconomía 1.8, Estadística 2.3.";
        var escenario = Con(SalidasVerificadas.LeeElProgramaYElDetalleYResponde(texto, "EST-005"), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, "COORD-02"), "¿Cuál es el promedio de Valentina Castro?");

        AfirmarNoVerificado(resultado, escenario, ResultadoAuditoria.EstadoNoVerificado);
    }

    [Theory]
    [InlineData("El promedio de Valentina Castro es 2.65.")]
    [InlineData("Tu programa tiene 3 estudiantes.")]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-76")]
    public async Task ESC_179_x_La_voz_correcta_o_la_referencia_al_programa_se_entrega(string texto)
    {
        var escenario = Con(SalidasVerificadas.LeeElProgramaYElDetalleYResponde(texto, "EST-005"), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, "COORD-02"), "¿Cuál es el promedio de Valentina Castro?");

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
    }

    [Fact]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-76")]
    public void ESC_179_y_Los_ejemplos_2_y_4_del_coordinador_van_en_tercera_persona()
    {
        var casos = PartesDelPrompt.CasosDeEjemplo(ConstructorPrompts.PromptSistema(Rol.Coordinador, null));

        Assert.Equal(4, casos.Count);
        Assert.Contains("Definitivas:", casos[1], StringComparison.Ordinal);
        Assert.Contains("clases programadas", casos[3], StringComparison.Ordinal);
        Assert.DoesNotMatch(SegundaPersona(), RespuestaDeEjemplo(casos[1]));
        Assert.DoesNotMatch(SegundaPersona(), RespuestaDeEjemplo(casos[3]));
        Assert.DoesNotContain("Tu promedio", string.Concat(casos), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tienes", string.Concat(casos), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-76")]
    public void ESC_179_y_Los_ejemplos_del_estudiante_siguen_en_segunda_persona()
    {
        var casos = PartesDelPrompt.CasosDeEjemplo(ConstructorPrompts.PromptSistema(Rol.Estudiante, null));

        Assert.Equal(4, casos.Count);
        Assert.Contains("Tu promedio es", casos[1], StringComparison.Ordinal);
        Assert.Contains("Tienes", casos[3], StringComparison.Ordinal);
    }
}
