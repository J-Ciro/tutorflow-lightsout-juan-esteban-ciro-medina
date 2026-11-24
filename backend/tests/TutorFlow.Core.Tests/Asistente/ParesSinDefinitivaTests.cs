using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class ParesSinDefinitivaTests
{
    private const string PreguntaContabilidad = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaPerdidas = "¿Qué dice el reglamento sobre la habilitación?";
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");
    private static readonly Usuario Est006 = new(Rol.Estudiante, "EST-006");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    public static TheoryData<string, string, string, string> ParesQueNoCuadran => new()
    {
        { "g1", "EST-004", PreguntaContabilidad, "Contabilidad: 3.3." },
        { "g2", "EST-006", "¿Cómo voy en Estadística?", "Estadística: 4.2." }
    };

    private static IndiceFijo IndiceDeNormas() => Indice(NotaDefinitiva, Articulo5, Articulo6);

    private static Func<SolicitudLlm, int, RespuestaLlm> LeeLosCortesYResponde(string texto) =>
        (_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Cortes, """{"asignatura":"Contabilidad"}""") : Texto(texto);

    [Theory]
    [MemberData(nameof(ParesQueNoCuadran))]
    [Trait("CA", "CA-17")]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-64")]
    public async Task ESC_198g1g2_Un_par_asignatura_cifra_sin_la_palabra_definitiva_que_no_cuadra_da_estado_no_verificado_con_reintento(string caso, string usuario, string pregunta, string texto)
    {
        Assert.NotEmpty(caso);
        Assert.NotEmpty(pregunta);

        await ClasesVerificadoras.AfirmarFallaAsync(usuario, texto, usuario);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-64")]
    public async Task ESC_198g3_Un_par_con_el_primer_corte_de_la_asignatura_se_entrega_sin_cambios()
    {
        const string texto = "Contabilidad 3.6 en el primer corte.";
        var escenario = Con(LeeLosCortesYResponde(texto), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(Est004, "¿Qué dice el reglamento sobre la habilitación?");

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-64")]
    public async Task ESC_198g4_Los_pares_de_Valentina_que_son_sus_definitivas_en_la_prelectura_se_entregan()
    {
        const string texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
        var escenario = Con(SalidasVerificadas.LeeElProgramaYResponde(texto), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
        Assert.Equal(2, escenario.Llm.Solicitudes.Count);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-64")]
    [Trait("SEG", "SEG-10c")]
    public async Task ESC_198g5_La_definitiva_de_una_aprobada_que_la_prelectura_no_lista_da_numero_no_verificado_antes_que_el_par()
    {
        var escenario = Con(SalidasVerificadas.LeeElProgramaYResponde("Camilo Rojas: Contabilidad 3.9."), IndiceDeNormas());

        var resultado = await escenario.PreguntarAsync(Coord02, PreguntaPerdidas);

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Empty(resultado.Respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(escenario.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-64")]
    public async Task ESC_198g_La_acumulada_de_la_asignatura_sin_definitiva_cuadra_como_par()
    {
        const string texto = "Estadística: 2.4.";
        await ClasesVerificadoras.AfirmarPasaAsync("EST-006", texto, "EST-006");
    }
}
