using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Operacion;

[Collection("Consola")]
public sealed class AuditoriaDetalleYArchivoApiTests : IDisposable
{
    private const string FechaDelReloj = "2026-11-22";
    private static readonly string[] CamposDeUnDato = ["estudianteId", "programaId", "recurso", "asignatura"];
    private static readonly string[] ValoresProhibidos = ["48", "4.39", "4.5", "4.8", "4.1", "4.0", "3.8"];
    private static readonly string[] EstudiantesDeAdm = ["EST-004", "EST-005", "EST-006"];

    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), $"tutorflow-auditoria-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    private string ArchivoDelDia => Path.Combine(_carpeta, $"auditoria-{FechaDelReloj}.jsonl");

    private FabricaApi FabricaConArchivo(string? ruta = null) =>
        new(
            llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => "Resumen de tus datos académicos."),
            configuracion: new Dictionary<string, string?> { ["Auditoria:Ruta"] = ruta ?? _carpeta },
            auditoriaReal: true);

    private static async Task<IReadOnlyList<string>> CapturarConsolaAsync(Func<Task> accion)
    {
        var original = Console.Out;
        using var escritor = new EscritorSincronizado();
        Console.SetOut(escritor);
        try
        {
            await accion();
        }
        finally
        {
            Console.SetOut(original);
        }

        return escritor.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.StartsWith('{') && l.Contains("\"correlationId\"", StringComparison.Ordinal))
            .ToList();
    }

    private async Task<JsonElement> EventoDe(string rol, string id, string pregunta)
    {
        using var fabrica = FabricaConArchivo();
        var lineas = await CapturarConsolaAsync(() => fabrica.PreguntarAsync(rol, id, pregunta));
        return JsonElement.Parse(Assert.Single(lineas));
    }

    private static JsonElement DatosConsultados(JsonElement evento)
    {
        Assert.True(evento.TryGetProperty("datosConsultados", out var datos), "el evento no tiene datosConsultados");
        Assert.Equal(JsonValueKind.Array, datos.ValueKind);
        return datos;
    }

    private static string Campo(JsonElement dato, string nombre) =>
        dato.TryGetProperty(nombre, out var valor) ? valor.GetString() ?? string.Empty : string.Empty;

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-083")]
    [Trait("D", "S-11")]
    public async Task ESC_083b_Los_caracteres_Cf_incluidos_los_astrales_se_escapan_por_punto_de_codigo_en_la_consola_y_el_archivo()
    {
        var pregunta = "a\U000E0041b c​d‮e";
        using var fabrica = FabricaConArchivo();

        var consola = await CapturarConsolaAsync(() => fabrica.PreguntarEstudianteAsync("EST-001", pregunta));

        var linea = Assert.Single(consola);
        Assert.Equal("a\\uDB40\\uDC41b c\\u200Bd\\u202Ee", JsonElement.Parse(linea).GetProperty("pregunta").GetString());
        Assert.Equal(linea, Assert.Single(File.ReadAllLines(ArchivoDelDia)));
        Assert.All(new[] { linea }.Concat(File.ReadAllLines(ArchivoDelDia)), texto => Assert.DoesNotContain(texto.EnumerateRunes(), r => System.Text.Rune.GetUnicodeCategory(r) == System.Globalization.UnicodeCategory.Format));
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-143")]
    public async Task ESC_143_Las_faltas_en_bases_de_datos_dejan_una_entrada_de_asistencia_con_el_codigo_de_la_asignatura()
    {
        var evento = await EventoDe("estudiante", "EST-001", "¿Cuántas faltas tengo en Bases de Datos?");

        var datos = DatosConsultados(evento).EnumerateArray().ToList();
        Assert.Contains(datos, d =>
            Campo(d, "estudianteId") == "EST-001"
            && Campo(d, "programaId") == "ING-SIS"
            && Campo(d, "recurso") == "asistencia"
            && Campo(d, "asignatura") == "BD-203");
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-143")]
    public async Task ESC_143_Cada_entrada_solo_trae_identificadores_y_nunca_valores()
    {
        var evento = await EventoDe("estudiante", "EST-001", "¿Cuántas faltas tengo en Bases de Datos?");

        var datos = DatosConsultados(evento);
        Assert.NotEmpty(datos.EnumerateArray());
        Assert.All(datos.EnumerateArray(), d =>
            Assert.All(d.EnumerateObject(), p => Assert.Contains(p.Name, CamposDeUnDato)));
        Assert.All(ValoresProhibidos, valor => Assert.DoesNotContain(valor, datos.GetRawText(), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-143")]
    public async Task ESC_143_El_coordinador_de_adm_solo_deja_entradas_de_su_programa()
    {
        var evento = await EventoDe("coordinador", "COORD-02", "¿Qué estudiantes de mi programa tienen materias perdidas?");

        var datos = DatosConsultados(evento).EnumerateArray().ToList();
        Assert.NotEmpty(datos);
        Assert.All(datos, d =>
        {
            Assert.Equal("ADM", Campo(d, "programaId"));
            Assert.Contains(Campo(d, "estudianteId"), EstudiantesDeAdm);
        });
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-143")]
    public async Task ESC_143_Una_denegacion_previa_deja_datos_consultados_vacio()
    {
        var evento = await EventoDe("estudiante", "EST-001", "¿Cuál es la nota de Bases de Datos de Andrés Martínez?");

        Assert.Equal("denegada", evento.GetProperty("resultado").GetString());
        Assert.Empty(DatosConsultados(evento).EnumerateArray());
    }

    private static async Task PreguntarTresVecesAsync(FabricaApi fabrica)
    {
        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?");
        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota de Bases de Datos de Andrés Martínez?");
        await fabrica.PreguntarEstudianteAsync("EST-001", "   ");
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-144")]
    public async Task ESC_144_Tres_peticiones_dejan_tres_lineas_identicas_a_las_de_la_consola()
    {
        using var fabrica = FabricaConArchivo();

        var consola = await CapturarConsolaAsync(() => PreguntarTresVecesAsync(fabrica));

        Assert.Equal(3, consola.Count);
        Assert.True(File.Exists(ArchivoDelDia), $"no existe {ArchivoDelDia}");
        Assert.Equal(consola, File.ReadAllLines(ArchivoDelDia));
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-144")]
    public async Task ESC_144_Las_lineas_existentes_no_se_reescriben_y_las_nuevas_se_agregan_al_final()
    {
        Directory.CreateDirectory(_carpeta);
        const string previa = "{\"evento\":\"previo\"}";
        await File.WriteAllLinesAsync(ArchivoDelDia, [previa]);
        using var fabrica = FabricaConArchivo();

        var consola = await CapturarConsolaAsync(() => PreguntarTresVecesAsync(fabrica));

        var lineas = File.ReadAllLines(ArchivoDelDia);
        Assert.Equal(4, lineas.Length);
        Assert.Equal(previa, lineas[0]);
        Assert.Equal(consola, lineas.Skip(1));
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-144")]
    public async Task ESC_144_Con_el_dia_siguiente_del_reloj_se_escribe_en_otro_archivo()
    {
        using var fabrica = FabricaConArchivo();
        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");
        fabrica.Reloj.Avanzar(TimeSpan.FromDays(1));

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        Assert.Single(File.ReadAllLines(ArchivoDelDia));
        Assert.Single(File.ReadAllLines(Path.Combine(_carpeta, "auditoria-2026-11-23.jsonl")));
    }

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-144")]
    public async Task ESC_144_Si_la_carpeta_no_se_puede_escribir_la_peticion_igual_responde()
    {
        Directory.CreateDirectory(_carpeta);
        var ocupada = Path.Combine(_carpeta, "es-un-archivo");
        await File.WriteAllTextAsync(ocupada, "no soy una carpeta");
        using var fabrica = FabricaConArchivo(ocupada);
        RespuestaApi? respuesta = null;

        var consola = await CapturarConsolaAsync(async () => respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?"));

        Assert.Equal(200, respuesta!.Estado);
        Assert.Single(consola);
    }
}
