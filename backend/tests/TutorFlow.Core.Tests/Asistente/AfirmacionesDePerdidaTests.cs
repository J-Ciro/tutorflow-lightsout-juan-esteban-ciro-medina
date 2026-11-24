using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class AfirmacionesDePerdidaTests
{
    private const string PreguntaPerdidas = "¿Qué dice el reglamento sobre la habilitación?";
    private const string PreguntaHabilitacion = "¿Qué dice el reglamento sobre la habilitación?";
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");

    public static TheoryData<string, string> TextosConPerdidaNoRespaldada => new()
    {
        { "a", "Valentina Castro (ID EST-005) y Mateo Vargas (ID EST-006) tienen asignaturas perdidas. Valentina Castro perdió Microeconomía (1.8) y Estadística (2.3) por inasistencia, mientras que Mateo Vargas perdió Estadística (sin definitiva) por inasistencia.\n\nValentina Castro: Asignaturas perdidas: Microeconomía (1.8), Estadística (2.3).\nMateo Vargas: Asignatura perdida: Estadística.\n\nEstas perdidas se respaldan en el Artículo 11 del reglamento académico." },
        { "b", "Mateo Vargas perdió Estadística (sin definitiva) por inasistencia." },
        { "c", "Valentina Castro (ID EST-005) tiene asignaturas perdidas por inasistencia: Microeconomía con 1.8 y Estadística con 2.3." },
        { "d", "Camilo Rojas perdió Contabilidad." },
        { "e", "Valentina Castro perdió Bases de Datos." },
        { "i", "Valentina Castro y Mateo Vargas perdieron Estadística." },
        { "spec", "Valentina Castro perdió Microeconomía (1.8) y Estadística (2.3) por inasistencia." },
        { "sin-definitiva", "Mateo Vargas perdió Estadística." },
        { "por-codigo", "Mateo Vargas perdió EST-103." },
        { "cifra-de-la-herramienta", "Valentina Castro perdió Estadística (2.3) por inasistencia." }
    };

    public static TheoryData<string> TextosCorrectosDeCoordinador02 => new()
    {
        "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).",
        "Valentina Castro perdió Microeconomía (1.8) y Estadística (2.3).",
        "Mateo Vargas no ha perdido Estadística: le falta calificar el corte 3.",
        "Valentina Castro (ID EST-005) tiene asignaturas perdidas: Microeconomía con 1.8 y Estadística con 2.3. Mateo Vargas (ID EST-006) tiene una asignatura sin definitiva: Estadística. Camilo Rojas (ID EST-004) no tiene ninguna asignatura perdida.",
        "Valentina Castro perdió Microeconomía y Camilo Rojas aprobó Contabilidad.",
        "Si Valentina Castro falta más, podría perder Bases de Datos.",
        "Mateo Vargas no perdió Estadística: aún no tiene definitiva.",
        "Camilo Rojas no ha perdido ninguna asignatura.",
        "Mateo Vargas no tiene ninguna asignatura perdida."
    };

    public static TheoryData<string, bool> TextosDeCoordinador01 => new()
    {
        { "Andrés Martínez perdió Cálculo Integral (2.6) y Bases de Datos por inasistencia.", true },
        { "Andrés Martínez perdió Cálculo.", true },
        { "Andrés Martínez perdió por inasistencia Bases de Datos y por nota Cálculo Integral.", false },
        { "Andrés Martínez perdió Cálculo Integral por inasistencia.", false },
        { "Andrés Martínez perdió Inglés III.", false }
    };

    private static readonly string[] Adm = ["EST-004", "EST-005", "EST-006"];
    private static readonly string[] IngSis = ["EST-001", "EST-002", "EST-003"];

    [Theory]
    [MemberData(nameof(TextosConPerdidaNoRespaldada))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task Una_perdida_que_los_datos_leidos_no_respaldan_se_reemplaza_por_texto_no_verificable(string caso, string texto)
    {
        Assert.NotEmpty(caso);

        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", texto, Adm);
    }

    [Theory]
    [MemberData(nameof(TextosCorrectosDeCoordinador02))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task Un_texto_que_los_datos_respaldan_o_sin_forma_de_perdida_se_entrega_sin_cambios(string texto)
    {
        await ClasesVerificadoras.AfirmarPasaAsync("COORD-02", texto, Adm);
    }

    [Theory]
    [MemberData(nameof(TextosDeCoordinador01))]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task La_ventana_de_inasistencia_se_evalua_por_asignatura(string texto, bool correcto)
    {
        if (correcto)
        {
            await ClasesVerificadoras.AfirmarPasaAsync("COORD-01", texto, IngSis);
        }
        else
        {
            await ClasesVerificadoras.AfirmarFallaAsync("COORD-01", texto, IngSis);
        }
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task Sin_datos_leidos_una_perdida_atribuida_a_una_persona_autorizada_no_se_verifica()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("COORD-02", "Valentina Castro perdió Microeconomía.");
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task La_inyeccion_del_propio_estudiante_no_logra_una_perdida_que_sus_datos_no_tienen()
    {
        await ClasesVerificadoras.AfirmarFallaAsync("EST-002", "Andrés Martínez perdió Inglés III.", "EST-002");
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task La_perdida_real_de_la_propia_persona_se_entrega()
    {
        await ClasesVerificadoras.AfirmarPasaAsync("EST-002", "Andrés Martínez perdió Cálculo Integral (2.6).", "EST-002");
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("ESC", "ESC-175")]
    [Trait("SEG", "SEG-10f")]
    public async Task El_orden_de_evaluacion_pone_el_numero_inventado_antes_que_la_perdida()
    {
        var escenario = Con(Fijo("Perdiste Microeconomía (1.9)."), Indice(Articulo6));

        var resultado = await escenario.PreguntarAsync(Est005, PreguntaPerdidas);

        Assert.Equal(Textos.NoVerificable, resultado.Respuesta!.Respuesta);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, resultado.Auditoria);
    }
}
