using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

public sealed class SegundaLineaD98Tests
{
    private static readonly Usuario Est001 = new(Rol.Estudiante, "EST-001");
    private static readonly Usuario Est005 = new(Rol.Estudiante, "EST-005");
    private static readonly Usuario Est006 = new(Rol.Estudiante, "EST-006");
    private static readonly Usuario Coord01 = new(Rol.Coordinador, "COORD-01");
    private static readonly Usuario Coord02 = new(Rol.Coordinador, "COORD-02");
    private static readonly string[] Adm = ["EST-004", "EST-005", "EST-006"];

    public static TheoryData<string, string, string[], string, bool> Casos => new()
    {
        { "ESC-175", "COORD-02", Adm, "Mateo Vargas perdió Estadística.", true },
        { "ESC-175", "COORD-02", Adm, "Camilo Rojas perdió Contabilidad.", true },
        { "ESC-175", "COORD-02", Adm, "Valentina Castro perdió Estadística (2.3) por inasistencia.", true },
        { "ESC-175", "COORD-02", Adm, "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).", false },
        { "ESC-175", "COORD-02", Adm, "Mateo Vargas no ha perdido Estadística: le falta calificar el corte 3.", false },
        { "ESC-175", "COORD-01", ["EST-002"], "Andrés Martínez perdió Inglés III.", true },
        { "ESC-175", "COORD-01", ["EST-002"], "Andrés Martínez perdió Cálculo Integral (2.6) y Bases de Datos por inasistencia.", false },
        { "ESC-175", "EST-006", ["EST-006"], "Tus asignaturas perdidas:\n- Estadística", true },
        { "ESC-175", "EST-005", ["EST-005"], "Asignaturas perdidas:\n- Microeconomía (1.8)\n- Estadística (2.3)", false },
        { "ESC-179", "EST-001", ["EST-001"], "Llevas 10 faltas en Bases de Datos.", true },
        { "ESC-179", "EST-001", ["EST-001"], "Llevas 2 faltas; el corte 3 ya está calificado.", false },
        { "ESC-179", "EST-001", ["EST-001"], "Te quedan 0 faltas.", false },
        { "ESC-179", "EST-001", ["EST-001"], "Tu definitiva en Bases de Datos es 4.0.", true },
        { "ESC-179", "EST-001", ["EST-001"], "Tu definitiva en Bases de Datos es 4.1.", false },
        { "ESC-184", "COORD-02", Adm, "Mateo Vargas:\n- Asignatura perdida: Estadística", true },
        { "ESC-184", "COORD-02", Adm, "| Estudiante | Asignaturas perdidas |\n| --- | --- |\n| Mateo Vargas | Estadística |", true },
        { "ESC-184", "COORD-02", Adm, "Estudiantes con asignaturas perdidas:\n- Valentina Castro: Microeconomía (1.8) y Estadística (2.3)", false },
        { "ESC-185", "EST-006", ["EST-006"], "Tu definitiva en Estadística es 4.0.", true },
        { "ESC-185", "EST-006", ["EST-006"], "Tu definitiva es 4.0 en Estadística.", true },
        { "ESC-193", "EST-001", ["EST-001"], "Tu promedio es 4.40.", true },
        { "ESC-193", "EST-001", ["EST-001"], "Tu promedio es 4.39.", false },
        { "ESC-199 h", "EST-001", ["EST-001"], "Tu promedio es 4.4.", true }
    };

    private static async Task<(DirectorioAcademico Directorio, Dictionary<string, ResumenEstudiante> Resumenes)> PrepararAsync(Usuario usuario, string[] leidos)
    {
        var api = new ApiAcademicaDoble(DatosReferencia.Cargar());
        var directorio = await ConstructorDirectorio.ConstruirAsync(usuario, api, [], CancellationToken.None);
        var resumenes = new Dictionary<string, ResumenEstudiante>();
        foreach (var id in leidos)
        {
            resumenes[id] = await ResumenesAcademicos.ResumirAsync(api, id, CancellationToken.None);
        }

        return (directorio!, resumenes);
    }

    private static string? Motivo(Usuario usuario, string texto, DirectorioAcademico directorio, Dictionary<string, ResumenEstudiante> resumenes)
    {
        var autorizados = new PoliticaAcceso().IdsPersonasAutorizadas(usuario, directorio);
        var bloques = BloquesDeRespuesta.De(texto);
        var catalogo = CatalogoDePrueba.Asignaturas;
        var contexto = new ContextoDeAfirmaciones(usuario, autorizados, directorio, resumenes);
        return new AfirmacionesDePerdida(catalogo).Motivo(bloques, usuario, autorizados, directorio, resumenes)
            ?? new AfirmacionesDeEstado(catalogo).Motivo(bloques, contexto)
            ?? new DefinitivaPorAsignatura(catalogo).Motivo(bloques, usuario, autorizados, directorio, resumenes)
            ?? CamposLigados.Motivo(bloques, contexto);
    }

    [Theory]
    [MemberData(nameof(Casos))]
    [Trait("CA", "CA-26")]
    [Trait("ESC", "ESC-175")]
    [Trait("D", "D-98")]
    public async Task Las_clases_verificadoras_de_f_y_g_actuan_sobre_el_texto_sin_pasar_por_la_poda(string escenario, string id, string[] leidos, string texto, bool debeFallar)
    {
        Assert.NotEmpty(escenario);
        var usuario = id.StartsWith("COORD", StringComparison.Ordinal) ? new Usuario(Rol.Coordinador, id) : new Usuario(Rol.Estudiante, id);
        var (directorio, resumenes) = await PrepararAsync(usuario, leidos);

        var motivo = Motivo(usuario, texto, directorio, resumenes);

        Assert.Equal(debeFallar, motivo is not null);
    }

    [Theory]
    [InlineData("Andres Martinez sacó 2.6 en Cálculo")]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-193")]
    [Trait("D", "D-98")]
    public async Task ESC_193_El_verificador_de_personas_bloquea_a_un_tercero_aunque_la_poda_no_exista(string texto)
    {
        var (directorio, _) = await PrepararAsync(Est001, ["EST-001"]);
        var autorizados = new PoliticaAcceso().IdsPersonasAutorizadas(Est001, directorio);

        Assert.True(new VerificadorSalida().ContienePersonaNoAutorizada(texto, autorizados, directorio));
    }

    [Fact]
    [Trait("CA", "CA-21")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-98")]
    public async Task ESC_199_g_Los_motivos_de_reintento_de_f_y_g_se_calculan_sobre_el_texto()
    {
        var (directorio, resumenes) = await PrepararAsync(Coord02, Adm);

        var motivo = Motivo(Coord02, "Mateo Vargas perdió Estadística.", directorio, resumenes);

        Assert.NotNull(motivo);
        Assert.Contains("Mateo Vargas", motivo, StringComparison.Ordinal);
        Assert.Contains("Estadística", motivo, StringComparison.Ordinal);
    }
}
