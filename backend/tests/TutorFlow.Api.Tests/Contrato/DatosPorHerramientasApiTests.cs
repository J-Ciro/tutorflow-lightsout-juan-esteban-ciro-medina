using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class DatosPorHerramientasApiTests
{
    public static TheoryData<string, string, string> DatosDelEstudiante => new()
    {
        { "EST-001", "¿Cuál es mi promedio ponderado del periodo?", "4.39|4.5|4.8|4.1|4.0" },
        { "EST-001", "¿Cuántas faltas tengo en Bases de Datos?", "48" },
        { "EST-004", "¿Qué nota definitiva tengo en Contabilidad?", "3.9|3.88" },
        { "EST-002", "¿Cuál es mi promedio del periodo?", "2.28|2.6|3.5|3.2|0.0" },
        { "EST-005", "¿Estoy en prueba académica?", "2.65|1.8|2.3|3.2|3.8" },
        { "EST-006", "¿Cuál es mi definitiva en Estadística?", "4.05|2.4|4.2|3.8" },
        { "EST-003", "¿Cuál es mi promedio?", "4.11|4.1|3.8|4.6" }
    };

    public static TheoryData<string, string, string, string> DatosDelCoordinador => new()
    {
        { "COORD-01", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?", "Andrés Martínez|Cálculo Integral|2.6|Bases de Datos|Sofía Herrera|Inglés III", "Camilo Rojas|Valentina Castro|Mateo Vargas" },
        { "COORD-02", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?", "Valentina Castro|Microeconomía|1.8|Estadística|2.3|2.65|Mateo Vargas", "Laura Gómez|Andrés Martínez|Sofía Herrera" }
    };

    private static string[] Dividir(string valores) => valores.Split('|');

    [Theory]
    [MemberData(nameof(DatosDelEstudiante))]
    [Trait("CA", "CA-02")]
    [Trait("CA", "CA-01")]
    public async Task Las_herramientas_entregan_al_estudiante_los_valores_calculados_por_el_backend(string id, string pregunta, string esperados)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarEstudianteAsync(id, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.True(respuesta.UsoDatosEstudiante);
        foreach (var esperado in Dividir(esperados))
        {
            Assert.Contains(esperado, respuesta.Respuesta, StringComparison.Ordinal);
        }

        Assert.All(OtrasPersonas(id), nombre => Assert.DoesNotContain(nombre, respuesta.Respuesta, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(DatosDelCoordinador))]
    [Trait("CA", "CA-02")]
    [Trait("CA", "CA-04")]
    public async Task Las_herramientas_entregan_al_coordinador_solo_los_datos_de_su_programa(string id, string pregunta, string esperados, string prohibidos)
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarCoordinadorAsync(id, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.All(Dividir(esperados), esperado => Assert.Contains(esperado, respuesta.Respuesta, StringComparison.Ordinal));
        Assert.All(Dividir(prohibidos), prohibido => Assert.DoesNotContain(prohibido, respuesta.Respuesta, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("ESC", "ESC-115")]
    public async Task ESC_115_El_coordinador_de_adm_consulta_solo_ids_de_adm_y_el_recurso_programa()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("Datos del programa · ADM", respuesta.Citas);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(["EST-004", "EST-005", "EST-006"], evento.EstudiantesConsultados.Order().ToArray());
        Assert.Contains(RecursoConsultado.Programa, evento.RecursosConsultados);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("ESC", "ESC-042")]
    public async Task ESC_042_El_coordinador_consulta_el_detalle_de_un_estudiante_de_su_programa()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos("EST-003"));

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", "¿Cuál es el promedio de EST-003?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("4.11", respuesta.Respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("ESC", "ESC-045")]
    public async Task ESC_045_Coordinador_sin_herramienta_de_datos_propios_no_usa_datos_de_estudiantes()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Texto("Como coordinador no tienes notas propias en el sistema."));

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", "¿Cuál es mi promedio?");

        Assert.Equal(200, respuesta.Estado);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain(fabrica.Api.Llamadas, l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("ESC", "ESC-135")]
    public async Task ESC_135_Coordinador_sin_programas_no_recibe_datos_de_ningun_estudiante()
    {
        var datos = DatosReferencia.Cargar().ConCoordinador(new TutorFlow.Core.Academico.Coordinador("COORD-03", "Sin Programas", []));
        using var fabrica = new FabricaApi(api: new ApiAcademicaDoble(datos), llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-03", "¿Qué estudiantes de mi programa perdieron materias?");

        Assert.Equal(200, respuesta.Estado);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.All(TodasLasPersonas(), nombre => Assert.DoesNotContain(nombre, respuesta.Respuesta, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Api.Llamadas, l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("ESC", "ESC-136")]
    public async Task ESC_136_Coordinador_con_dos_programas_ve_la_union_con_una_cita_por_programa()
    {
        var datos = DatosReferencia.Cargar().ConCoordinador(new TutorFlow.Core.Academico.Coordinador("COORD-09", "Doble Programa", ["ING-SIS", "ADM"]));
        using var fabrica = new FabricaApi(api: new ApiAcademicaDoble(datos), llm: ClienteLlmDoble.ConsultaDatos());

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-09", "¿Qué estudiantes de mis programas perdieron materias?");

        Assert.Equal(200, respuesta.Estado);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains("Andrés Martínez", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Valentina Castro", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Datos del programa · ING-SIS", respuesta.Citas);
        Assert.Contains("Datos del programa · ADM", respuesta.Citas);
    }

    private static IEnumerable<string> OtrasPersonas(string estudianteId)
    {
        var directorio = DatosReferencia.Cargar();
        var propio = directorio.Estudiantes.Single(e => e.Id == estudianteId);
        var coordinador = directorio.Programas.Single(p => p.Id == propio.ProgramaId).CoordinadorId;
        var estudiantes = directorio.Estudiantes.Where(e => e.Id != estudianteId).SelectMany(e => new[] { e.Nombre, e.Id });
        var coordinadores = directorio.Coordinadores.Where(c => c.Id != coordinador).SelectMany(c => new[] { c.Nombre, c.Id });
        return estudiantes.Concat(coordinadores);
    }

    private static IEnumerable<string> TodasLasPersonas()
    {
        var datos = DatosReferencia.Cargar();
        return datos.Estudiantes.SelectMany(e => new[] { e.Nombre, e.Id });
    }
}
