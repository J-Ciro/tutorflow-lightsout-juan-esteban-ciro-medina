using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;
using static TutorFlow.Core.Tests.Asistente.SeccionesDeReferencia;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class BloquesDeDefinitivaTests
{
    private const string PreguntaDefinitivaEstadistica = "¿Cuál es mi nota definitiva en Estadística?";
    private const string PreguntaAprobarEstadistica = "¿Qué necesito para aprobar Estadística?";
    private const string PreguntaHabilitar = "¿Puedo habilitar Microeconomía?";
    private const string PreguntaDefinitivas = "¿Cuáles son mis notas definitivas?";
    private const string PreguntaPromedioPonderado = "¿Cuál es mi promedio ponderado del periodo?";
    private const string CitaNotas = "Datos del estudiante";
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est004 = new(Rol.Estudiante, "EST-004");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");
    private static readonly Usuario Est006 = new(Rol.Estudiante, "EST-006");

    public static TheoryData<string, string> Est006QueDebenFallar => new()
    {
        { "a-sin-duda", "Sin duda, tu definitiva en Estadística es 4.0." },
        { "b-aun-asi", "Aun así, tu definitiva en Estadística es 4.0." },
        { "c-no-dudes", "No dudes: tu definitiva en Estadística es 4.0." },
        { "d-umbral-con-superior-a", "Tu definitiva en Estadística, superior a 3.0, es 4.0." },
        { "e-umbral-entre-parentesis", "Tu definitiva en Estadística (mínimo 3.0 para aprobar) es 4.0." },
        { "f-necesita-no-es-requisito", "Necesita revisión: definitiva de Estadística 4.0." },
        { "umbral-y-luego-la-cifra", "Tu definitiva en Estadística es mayor a 3.0 y llega a 4.0." }
    };

    public static TheoryData<string, string> Est006QueSeEntregan => new()
    {
        { "g-no-tiene-nota-definitiva", "Estadística no tiene nota definitiva: llevas 2.4 sobre el 60% evaluado." },
        { "aun-no-tiene-definitiva", "Estadística aún no tiene definitiva: llevas 2.4 sobre el 60% evaluado." }
    };

    public static TheoryData<string, string, string, string> OtrosEstudiantes => new()
    {
        { "h-umbral-y-siguiente-cifra", "EST-005", PreguntaAprobarEstadistica, "Estadística se aprueba con definitiva ≥ 3.0 y la tuya es 2.3." },
        { "i-umbral-sin-otra-cifra", "EST-005", PreguntaHabilitar, "Microeconomía no es habilitable porque tu definitiva es menor a 2.0." },
        { "j-necesitas-una-definitiva-de", "EST-005", PreguntaAprobarEstadistica, "Para aprobar Estadística necesitas una definitiva de 3.0." },
        { "k-pares-fieles", "EST-004", PreguntaDefinitivas, "Tus definitivas:\n- Contabilidad: 3.9\n- Microeconomía: 3.3" }
    };

    public static TheoryData<string, string, string> ParesQueDebenFallar => new()
    {
        { "l-pares-cruzados", "EST-004", "Tus definitivas:\n- Contabilidad: 3.3\n- Microeconomía: 3.9" },
        { "lista-cruzada-de-est-001", "EST-001", "Tus definitivas:\n- Cálculo Integral: 4.1\n- Bases de Datos: 4.5" },
        { "linea-de-definitivas-cruzada", "EST-001", "Definitivas: Cálculo Integral 4.8, Programación Orientada a Objetos 4.5, Bases de Datos 4.1, Inglés III 4.0." }
    };

    public static TheoryData<string, string> ParesFieles => new()
    {
        { "lista-fiel-de-est-001", "Tus definitivas:\n- Cálculo Integral: 4.5\n- Bases de Datos: 4.1" },
        { "q10-2-literal", "4.39. Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0." },
        { "q10-2-con-promedio", "Tu promedio ponderado del periodo es 4.39. Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0." }
    };

    private static Usuario UsuarioDe(string id) => id switch
    {
        "EST-001" => Est001,
        "EST-004" => Est004,
        "EST-005" => Est005,
        _ => Est006
    };

    private static EscenarioAsistente ConArticulos(string texto) => Con(Fijo(texto), Indice(Articulo5, Articulo6));

    private static void AfirmarEntregada(ResultadoPregunta resultado, EscenarioAsistente escenario, string texto)
    {
        SalidasVerificadas.AfirmarRespondida(resultado, escenario, texto);
        Assert.Contains(resultado.Respuesta!.Citas, c => c.Documento == CitaNotas && c.Seccion == "Notas");
    }

    [Theory]
    [MemberData(nameof(Est006QueDebenFallar))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_Una_definitiva_de_Estadistica_en_el_formato_de_un_modelo_se_verifica_y_falla_cerrada(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = ConArticulos(texto);

        var resultado = await escenario.PreguntarAsync(Est006, PreguntaDefinitivaEstadistica);

        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }

    [Theory]
    [MemberData(nameof(Est006QueSeEntregan))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_La_negacion_que_gobierna_definitiva_deja_pasar_la_respuesta_fiel(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = ConArticulos(texto);

        var resultado = await escenario.PreguntarAsync(Est006, PreguntaDefinitivaEstadistica);

        AfirmarEntregada(resultado, escenario, texto);
    }

    [Theory]
    [MemberData(nameof(OtrosEstudiantes))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_Un_umbral_se_salta_y_los_pares_fieles_se_entregan_sin_cambios(string caso, string usuarioId, string pregunta, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = ConArticulos(texto);

        var resultado = await escenario.PreguntarAsync(UsuarioDe(usuarioId), pregunta);

        AfirmarEntregada(resultado, escenario, texto);
    }

    [Theory]
    [MemberData(nameof(ParesQueDebenFallar))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_Un_par_asignatura_cifra_que_no_coincide_con_la_definitiva_calculada_falla_cerrado(string caso, string usuarioId, string texto)
    {
        Assert.NotEmpty(caso);
        var pregunta = usuarioId == "EST-005" ? PreguntaAprobarEstadistica : usuarioId == "EST-001" ? PreguntaPromedioPonderado : PreguntaDefinitivas;
        var escenario = Con(Fijo(texto), Indice(Articulo5, Articulo6, PromedioPonderado, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(UsuarioDe(usuarioId), pregunta);

        SalidasVerificadas.AfirmarEstadoNoVerificado(resultado, escenario, usoDatos: true);
    }

    [Theory]
    [MemberData(nameof(ParesFieles))]
    [Trait("CA", "CA-17")]
    [Trait("ESC", "ESC-185")]
    [Trait("RF", "SEG-10g")]
    public async Task ESC_185_Los_pares_de_una_lista_de_definitivas_que_coinciden_se_entregan_sin_cambios(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        var escenario = Con(Fijo(texto), Indice(PromedioPonderado, NotaDefinitiva));

        var resultado = await escenario.PreguntarAsync(Est001, PreguntaPromedioPonderado);

        AfirmarEntregada(resultado, escenario, texto);
    }
}
