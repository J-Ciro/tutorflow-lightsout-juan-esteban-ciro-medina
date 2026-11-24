using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class AtribucionD87D88Tests
{
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est002 = new(Rol.Estudiante, "EST-002");
    private static readonly Cita CitaArticulo5 = new(Reglamento, "Artículo 5");

    private static IndiceFijo IndiceCompleto() =>
        Indice(NotaDefinitiva, PromedioPonderado, Articulo5, Articulo6, Articulo11, Articulo20);

    private static IEnumerable<Cita> CitasDeDocumentos(ResultadoPregunta resultado) =>
        resultado.Respuesta!.Citas.Where(c => c.Documento is Reglamento or Evaluacion or Bienestar);

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-87")]
    public async Task ESC_177_l_La_palabra_definitiva_con_cifras_y_asignaturas_no_cita_la_Seccion_3()
    {
        const string texto = "Valentina Castro: Microeconomía (definitiva 1.8) y Estadística (definitiva 2.3) (reglamento_academico_2025.md · Artículo 5).";
        var escenario = Con(Fijo(texto), IndiceCompleto());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Equal(ResultadoAuditoria.Respondida, resultado.Auditoria);
        Assert.Contains("Microeconomía (definitiva 1.8) y Estadística (definitiva 2.3)", resultado.Respuesta!.Respuesta, StringComparison.Ordinal);
        Assert.Equal([CitaArticulo5], CitasDeDocumentos(resultado));
        Assert.Single(escenario.Llm.Solicitudes);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-87")]
    public async Task ESC_177_l_La_palabra_definitiva_sola_sin_cifra_ni_asignatura_sigue_usando_la_Seccion_3()
    {
        var escenario = Con(Fijo("La definitiva se calcula con los tres cortes."), IndiceCompleto());

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedio);

        Assert.Contains(new Cita(Evaluacion, "Sección 3"), resultado.Respuesta!.Citas);
    }

    public static TheoryData<string, string, string> ProsaConDefinitivaDeLaAsignaturaAnterior => new()
    {
        { "COORD-01", "andres-1", "Andrés Martínez perdió Cálculo Integral, cuya definitiva es 2.6, y Bases de Datos por inasistencia." },
        { "COORD-02", "valentina-1", "Valentina Castro perdió Microeconomía, cuya definitiva es 1.8, y Estadística, cuya definitiva es 2.3." }
    };

    [Theory]
    [MemberData(nameof(ProsaConDefinitivaDeLaAsignaturaAnterior))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-88")]
    public async Task ESC_179_z_La_cifra_tras_cuya_definitiva_es_se_atribuye_a_la_asignatura_anterior_con_una_sola_llamada(string coordinador, string caso, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = Con(Fijo(texto), IndiceCompleto());

        var resultado = await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, coordinador), PreguntaPerdidas);

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
        Assert.Single(escenario.Llm.Solicitudes);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-88")]
    public async Task ESC_179_z_La_cifra_de_la_asignatura_siguiente_sigue_bloqueandose()
    {
        var escenario = Con(Fijo("Definitiva: 3.5 de Cálculo Integral."), IndiceCompleto());

        var resultado = await escenario.PreguntarAsync(Est002, "¿Cuál es mi definitiva en Cálculo Integral?");

        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-88")]
    public async Task ESC_179_z_La_definitiva_falsa_con_cuya_definitiva_es_sigue_bloqueandose()
    {
        var escenario = Con(Fijo("Valentina Castro perdió Microeconomía, cuya definitiva es 2.3, y Estadística."), IndiceCompleto());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-179")]
    [Trait("D", "D-88")]
    public async Task ESC_179_z_La_cifra_ajena_atribuida_a_la_asignatura_anterior_en_ING_SIS_sigue_verificandose()
    {
        var escenario = Con(Fijo("Andrés Martínez perdió Cálculo Integral, cuya definitiva es 0.0, y Bases de Datos por inasistencia."), IndiceCompleto());

        var resultado = await escenario.PreguntarAsync(Coord01, PreguntaPerdidas);

        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }
}
