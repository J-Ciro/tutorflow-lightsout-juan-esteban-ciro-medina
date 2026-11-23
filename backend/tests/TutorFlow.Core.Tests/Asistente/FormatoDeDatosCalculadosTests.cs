using System.Text.RegularExpressions;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

public sealed partial class FormatoDeDatosCalculadosTests
{
    private const string PreguntaDeResumen = "¿Cuál es mi promedio?";
    private const string PreguntaDePerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string EncabezadoDeReglas = "Reglas aplicadas a estos datos: ";
    private const string Perdida = "Perdida";
    private const string SinDefinitiva = "Sin definitiva";
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");

    private static readonly string BloqueAdm = string.Join(
        "\n",
        "Programa ADM (Administración de Empresas): 3 estudiantes.",
        string.Empty,
        "Estudiante: Camilo Rojas (ID EST-004)",
        "Promedio del periodo: 3.88",
        "Prueba académica: no",
        string.Empty,
        "Estudiante: Valentina Castro (ID EST-005)",
        "Promedio del periodo: 2.65",
        "Prueba académica: sí",
        "Perdida por nota: Microeconomía (definitiva 1.8)",
        "Perdida por nota: Estadística (definitiva 2.3)",
        string.Empty,
        "Estudiante: Mateo Vargas (ID EST-006)",
        "Promedio del periodo: 4.05",
        "Prueba académica: no",
        "Sin definitiva (falta calificar un corte): Estadística",
        string.Empty,
        "Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)",
        "Estudiantes en prueba académica: Valentina Castro (promedio 2.65)");

    private static readonly string[] ReglasAdm =
    [
        "reglamento_academico_2025.md · Artículo 5",
        "reglamento_academico_2025.md · Artículo 6",
        "reglamento_academico_2025.md · Artículo 20",
        "evaluacion_y_calificaciones.md · Sección 3"
    ];

    private static readonly string[] ReglasIngSis =
    [
        "reglamento_academico_2025.md · Artículo 5",
        "reglamento_academico_2025.md · Artículo 6",
        "reglamento_academico_2025.md · Artículo 7",
        "reglamento_academico_2025.md · Artículo 11",
        "reglamento_academico_2025.md · Artículo 20",
        "reglamento_academico_2025.md · Artículo 12"
    ];

    public static TheoryData<string, string> LineasDeNotas => new()
    {
        { "EST-001", "Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0" },
        { "EST-001", "Promedio ponderado del periodo: 4.39" },
        { "EST-002", "Definitivas: Cálculo Integral 2.6, Programación Orientada a Objetos 3.5, Bases de Datos 0.0 (por inasistencia), Inglés III 3.2" },
        { "EST-002", "Promedio ponderado del periodo: 2.28" },
        { "EST-006", "Definitivas: Contabilidad 4.2, Microeconomía 3.8, Comunicación Oral y Escrita 4.2" },
        { "EST-006", "Sin definitiva (falta calificar un corte): Estadística (acumulada 2.4 sobre el 60% evaluado)" },
        { "EST-006", "Promedio ponderado del periodo: 4.05" }
    };

    public static TheoryData<string, string> LineasDeAsistencia => new()
    {
        { "EST-001", "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia." },
        { "EST-001", "Cálculo Integral: faltas 1 de 64 clases programadas (faltas restantes: 8; máximo sin perder: 9). Si superas 9 inasistencias pierdes la asignatura por inasistencia." },
        { "EST-001", "Programación Orientada a Objetos: faltas 0 de 48 clases programadas (faltas restantes: 7; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia." },
        { "EST-001", "Inglés III: faltas 1 de 32 clases programadas (faltas restantes: 3; máximo sin perder: 4). Si superas 4 inasistencias pierdes la asignatura por inasistencia." },
        { "EST-002", "Bases de Datos: faltas 9 de 48 clases programadas (faltas restantes: 0; máximo sin perder: 7). Perdida por inasistencia: superaste 7 inasistencias." },
        { "EST-003", "Inglés III: sin registro de inasistencias en 32 clases programadas (máximo sin perder: 4). Solicita la revisión a tu coordinador (Artículo 12)." }
    };

    public static TheoryData<string, string, string> LineasDeDetalleDelCoordinador => new()
    {
        { "COORD-01", "EST-001", "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si supera 7 inasistencias pierde la asignatura por inasistencia." },
        { "COORD-01", "EST-002", "Bases de Datos: faltas 9 de 48 clases programadas (faltas restantes: 0; máximo sin perder: 7). Perdida por inasistencia: superó 7 inasistencias." },
        { "COORD-01", "EST-003", "Inglés III: sin registro de inasistencias en 32 clases programadas (máximo sin perder: 4). Solicite la revisión al coordinador del programa (Artículo 12)." }
    };

    public static TheoryData<string, string> LineasDeIngSis => new()
    {
        { "Programa", "Programa ING-SIS (Ingeniería de Sistemas): 3 estudiantes." },
        { "pérdida por nota", "Perdida por nota: Cálculo Integral (definitiva 2.6)" },
        { "pérdida por inasistencia", "Perdida por inasistencia: Bases de Datos (definitiva 0.0)" },
        { "sin registro", "Asistencia sin registro: Inglés III" },
        { "pérdidas con motivo", "Estudiantes con asignaturas perdidas: Andrés Martínez (Cálculo Integral 2.6; Bases de Datos 0.0 por inasistencia)" },
        { "prueba académica", "Estudiantes en prueba académica: Andrés Martínez (promedio 2.28)" },
        { "promedio de Laura", "Promedio del periodo: 4.39" },
        { "promedio de Andrés", "Promedio del periodo: 2.28" },
        { "promedio de Sofía", "Promedio del periodo: 4.11" }
    };

    public static TheoryData<string> TodosLosEstudiantes => new() { "EST-001", "EST-002", "EST-003", "EST-004", "EST-005", "EST-006" };

    public static TheoryData<string> EstudiantesSinPerdidas => new() { "EST-001", "EST-003", "EST-004", "EST-006" };

    [GeneratedRegex(@"\b(perdid[ao]s?|perdio)\b|\bpérdida\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PalabraDePerdida();

    private static ResumenEstudiante ResumenDe(string estudianteId) =>
        CalculosAcademicos.ResumirEstudiante(estudianteId, DatosReferencia.Cargar().AsignaturasDe(estudianteId));

    private static string[] Lineas(string texto) => [.. texto.Split('\n').Select(l => l.TrimEnd('\r'))];

    private static async Task<string[]> LineasDelResumenAsync(string estudianteId)
    {
        var escenario = Con(Fijo(Textos.SinInformacion));
        await escenario.PreguntarAsync(new Usuario(Rol.Estudiante, estudianteId), PreguntaDeResumen);
        return Lineas(escenario.Llm.TextoDeLlamada(0));
    }

    private static async Task<string> ResultadoDeProgramaAsync(Usuario coordinador, string pregunta = PreguntaDePerdidas)
    {
        var escenario = Con((_, llamada) => llamada == 0 ? Herramienta(HerramientasAsistente.Programa) : Texto(Textos.SinInformacion));
        await escenario.PreguntarAsync(coordinador, pregunta);
        return ContenidoDeHerramienta(escenario.Llm, 1);
    }

    private static string ContenidoDeHerramienta(LlmDoble llm, int indice)
    {
        var mensaje = llm.Solicitudes[indice].Mensajes.First(m => m.Rol == RolMensaje.Herramienta).Contenido;
        var lineas = Lineas(mensaje);
        return string.Join("\n", lineas.Skip(1).Take(lineas.Length - 2));
    }

    private static string[] ReglasDe(string resultado)
    {
        var linea = Lineas(resultado).Last(l => l.StartsWith(EncabezadoDeReglas, StringComparison.Ordinal));
        return [.. linea[EncabezadoDeReglas.Length..].TrimEnd('.').Split(';').Select(r => r.Trim())];
    }

    private static string SinLineaDeReglas(string resultado) =>
        string.Join("\n", Lineas(resultado).TakeWhile(l => !l.StartsWith(EncabezadoDeReglas, StringComparison.Ordinal)));

    [Theory]
    [MemberData(nameof(LineasDeNotas))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-05")]
    public async Task ESC_189abc_El_resumen_propio_trae_la_linea_exacta_de_notas(string estudianteId, string linea)
    {
        var lineas = await LineasDelResumenAsync(estudianteId);

        Assert.Contains(linea, lineas);
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-66")]
    public async Task ESC_189g_El_resumen_de_EST_006_trae_el_equivalente_de_la_acumulada_y_la_nota_minima()
    {
        var lineas = await LineasDelResumenAsync("EST-006");

        Assert.Contains("Equivalente de la acumulada de Estadística sobre la nota completa: 4.0", lineas);
        Assert.Contains("Nota mínima para aprobar: 3.0", lineas);
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("D", "D-66")]
    public async Task ESC_189g_El_resumen_de_EST_001_trae_la_nota_minima_y_ninguna_linea_de_equivalente()
    {
        var lineas = await LineasDelResumenAsync("EST-001");

        Assert.Contains("Nota mínima para aprobar: 3.0", lineas);
        Assert.DoesNotContain(lineas, l => l.StartsWith("Equivalente", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(LineasDeNotas))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-05")]
    public void ESC_189abc_La_herramienta_de_notas_trae_la_linea_exacta(string estudianteId, string linea)
    {
        var lineas = Lineas(FormateadorDatos.Notas(ResumenDe(estudianteId)));

        Assert.Contains(linea, lineas);
    }

    [Theory]
    [MemberData(nameof(LineasDeAsistencia))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-13")]
    public async Task ESC_189abc_El_resumen_propio_trae_la_linea_exacta_de_asistencia(string estudianteId, string linea)
    {
        var lineas = await LineasDelResumenAsync(estudianteId);

        Assert.Contains(linea, lineas);
    }

    [Theory]
    [MemberData(nameof(LineasDeAsistencia))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-13")]
    public void ESC_189abc_La_herramienta_de_asistencia_trae_la_linea_exacta(string estudianteId, string linea)
    {
        var lineas = Lineas(FormateadorDatos.Asistencia(ResumenDe(estudianteId)));

        Assert.Contains(linea, lineas);
    }

    [Theory]
    [MemberData(nameof(LineasDeDetalleDelCoordinador))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    [Trait("RN", "RN-13")]
    public async Task ESC_189_El_detalle_del_coordinador_pasa_las_frases_a_tercera_persona(string coordinador, string estudianteId, string linea)
    {
        var escenario = Con((_, llamada) => llamada == 0
            ? Herramienta(HerramientasAsistente.Estudiante, $$"""{"estudianteId":"{{estudianteId}}"}""")
            : Texto(Textos.SinInformacion));

        await escenario.PreguntarAsync(new Usuario(Rol.Coordinador, coordinador), $"¿Cómo va {estudianteId} en asistencia?");

        Assert.Contains(linea, Lineas(ContenidoDeHerramienta(escenario.Llm, 1)));
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189d_El_resultado_de_ADM_es_exactamente_el_bloque_de_la_spec_y_termina_con_las_reglas_aplicadas()
    {
        var resultado = await ResultadoDeProgramaAsync(Coord02);

        Assert.Equal(BloqueAdm, SinLineaDeReglas(resultado));
        Assert.Equal(ReglasAdm, ReglasDe(resultado));
        Assert.StartsWith(EncabezadoDeReglas, Lineas(resultado).Last(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189e_El_resultado_de_ING_SIS_termina_con_las_reglas_aplicadas_de_perdidas_inasistencia_y_prueba()
    {
        var resultado = await ResultadoDeProgramaAsync(Coord01, "¿Qué estudiantes de mi programa perdieron materias?");

        Assert.Equal(ReglasIngSis, ReglasDe(resultado));
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-63")]
    public async Task ESC_198m_La_ultima_linea_del_resultado_de_ADM_es_exactamente_la_de_la_spec_y_termina_en_punto()
    {
        var resultado = await ResultadoDeProgramaAsync(Coord02);

        Assert.Equal("Reglas aplicadas a estos datos: reglamento_academico_2025.md · Artículo 5; reglamento_academico_2025.md · Artículo 6; reglamento_academico_2025.md · Artículo 20; evaluacion_y_calificaciones.md · Sección 3.", Lineas(resultado).Last());
    }

    [Theory]
    [MemberData(nameof(TodosLosEstudiantes))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-63")]
    public async Task ESC_198m_La_linea_de_reglas_aplicadas_del_resumen_propio_termina_en_punto(string estudianteId)
    {
        var lineas = await LineasDelResumenAsync(estudianteId);

        var reglas = Assert.Single(lineas, l => l.StartsWith(EncabezadoDeReglas, StringComparison.Ordinal));
        Assert.EndsWith(".", reglas, StringComparison.Ordinal);
        Assert.DoesNotContain(". ", reglas[..^1], StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(LineasDeIngSis))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189e_El_resultado_de_ING_SIS_trae_la_linea_exacta(string caso, string linea)
    {
        Assert.NotEmpty(caso);

        var resultado = await ResultadoDeProgramaAsync(Coord01, "¿Qué estudiantes de mi programa perdieron materias?");

        Assert.Contains(linea, Lineas(resultado));
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189d_El_bloque_de_Mateo_Vargas_no_contiene_perdida_ni_ninguna()
    {
        var resultado = await ResultadoDeProgramaAsync(Coord02);

        var bloque = Lineas(resultado)
            .SkipWhile(l => !l.StartsWith("Estudiante: Mateo Vargas", StringComparison.Ordinal))
            .TakeWhile(l => l.Length > 0)
            .ToList();
        Assert.NotEmpty(bloque);
        Assert.All(bloque, linea => Assert.DoesNotMatch(PalabraDePerdida(), linea));
        Assert.All(bloque, linea => Assert.DoesNotContain("ninguna", linea, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("COORD-01")]
    [InlineData("COORD-02")]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189de_Ninguna_linea_del_programa_afirma_perdida_y_falta_de_definitiva_a_la_vez(string coordinador)
    {
        var resultado = await ResultadoDeProgramaAsync(new Usuario(Rol.Coordinador, coordinador));

        Assert.All(
            Lineas(resultado).Where(l => l.Contains(Perdida, StringComparison.OrdinalIgnoreCase)),
            linea => Assert.DoesNotContain(SinDefinitiva, linea, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("COORD-01")]
    [InlineData("COORD-02")]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189de_Cada_linea_Perdida_por_nombra_exactamente_una_asignatura_con_su_definitiva(string coordinador)
    {
        var resultado = await ResultadoDeProgramaAsync(new Usuario(Rol.Coordinador, coordinador));

        var perdidas = Lineas(resultado).Where(l => l.StartsWith("Perdida por", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(perdidas);
        Assert.All(perdidas, linea => Assert.Matches(@"^Perdida por (nota|inasistencia): [^,;()]+ \(definitiva \d\.\d\)$", linea));
    }

    [Theory]
    [InlineData("COORD-01")]
    [InlineData("COORD-02")]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189de_Ninguna_linea_del_programa_tiene_un_valor_vacio_ni_ninguna_como_valor_de_un_campo(string coordinador)
    {
        var resultado = await ResultadoDeProgramaAsync(new Usuario(Rol.Coordinador, coordinador));

        Assert.All(
            Lineas(resultado).Where(l => l.Contains(':', StringComparison.Ordinal)),
            linea => Assert.DoesNotMatch(@":\s*(ninguna|ninguno)?\s*\.?$", linea));
    }

    [Theory]
    [MemberData(nameof(EstudiantesSinPerdidas))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189abc_El_resumen_de_quien_no_perdio_nada_no_contiene_palabras_de_perdida(string estudianteId)
    {
        var lineas = await LineasDelResumenAsync(estudianteId);

        var resumen = lineas.SkipWhile(l => !l.Contains("DATOS_NO_CONFIABLES", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(resumen);
        Assert.All(resumen, linea => Assert.DoesNotMatch(PalabraDePerdida(), linea));
    }

    [Theory]
    [MemberData(nameof(TodosLosEstudiantes))]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189abc_Ninguna_linea_del_resumen_afirma_perdida_y_falta_de_definitiva_a_la_vez(string estudianteId)
    {
        var lineas = await LineasDelResumenAsync(estudianteId);

        Assert.All(
            lineas.Where(l => PalabraDePerdida().IsMatch(l)),
            linea => Assert.DoesNotContain("sin definitiva", linea, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("CA", "CA-18")]
    [Trait("ESC", "ESC-189")]
    public async Task ESC_189d_Las_definitivas_de_las_asignaturas_aprobadas_no_se_listan_en_el_programa()
    {
        var resultado = await ResultadoDeProgramaAsync(Coord02);

        Assert.DoesNotContain("Contabilidad", resultado, StringComparison.Ordinal);
        Assert.DoesNotContain("Comunicación Oral", resultado, StringComparison.Ordinal);
    }
}
