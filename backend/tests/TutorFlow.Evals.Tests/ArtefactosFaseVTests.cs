using System.Text.Json;

namespace TutorFlow.Evals.Tests;

public sealed class ArtefactosFaseVTests : IDisposable
{
    private const string LlaveDeLaPrueba = "sk-ant-canario-de-la-prueba-0123456789";
    private const string TextoCrudo = "Bases de Datos: 2 faltas de 48 clases (evaluacion_y_calificaciones.md · Sección 3).";
    private const string FuenteDelArticulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string CasoDeFaltas = """
        {
          "id": "ESC-003",
          "fuente": "CA-01 pregunta 3",
          "usuario": { "rol": "estudiante", "id": "EST-001" },
          "pregunta": "¿Cuántas faltas tengo en Bases de Datos?",
          "http": 200,
          "contiene": [],
          "noContiene": [],
          "citasIncluye": [],
          "citasProhibidas": [],
          "citasVacias": false,
          "citasExactas": null,
          "citasTotal": null,
          "usoDatosEstudiante": true,
          "respuestaExacta": null,
          "tokensCero": false,
          "filtroPrevio": false,
          "requiereLlm": true,
          "soloOffline": false,
          "offline": {
            "herramientas": "ninguna",
            "estudianteIdCoordinador": null,
            "texto": "Bases de Datos: 2 faltas de 48 clases (evaluacion_y_calificaciones.md · Sección 3)."
          }
        }
        """;

    private const string CasoDeNotaMinima = """
        {
          "id": "ESC-001",
          "fuente": "CA-01 pregunta 1",
          "usuario": { "rol": "estudiante", "id": "EST-001" },
          "pregunta": "¿Cuál es la nota mínima para aprobar una asignatura?",
          "http": 200,
          "contiene": [],
          "noContiene": [],
          "citasIncluye": [],
          "citasProhibidas": [],
          "citasVacias": false,
          "citasExactas": null,
          "citasTotal": null,
          "usoDatosEstudiante": false,
          "respuestaExacta": null,
          "tokensCero": false,
          "filtroPrevio": false,
          "requiereLlm": true,
          "soloOffline": false,
          "offline": {
            "herramientas": "ninguna",
            "estudianteIdCoordinador": null,
            "texto": "La nota mínima para aprobar una asignatura es 3.0, en una escala de 0.0 a 5.0."
          }
        }
        """;

    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "tutorflow-evals-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    private static CasoEval Caso(string json = CasoDeFaltas) => JsonSerializer.Deserialize<CasoEval>(json, Json.Opciones)!;

    private static ArtefactoCorrida Artefacto(string textoCrudo) =>
        new(
            "¿Cuántas faltas tengo en Bases de Datos?",
            new UsuarioEval("estudiante", "EST-001"),
            1,
            [textoCrudo],
            [FuenteDelArticulo11],
            true,
            false,
            [new UsoEval(1200, 35)],
            "Bases de Datos: 2 faltas de 48 clases.",
            ["reglamento_academico_2025.md · Artículo 11"]);

    private async Task<JsonElement> EjecutarYLeerAsync(string? llave, string casoJson = CasoDeFaltas)
    {
        var ejecutor = new EjecutorFaseV(offline: true, _carpeta, llave);

        await ejecutor.EjecutarAsync(Caso(casoJson), CancellationToken.None);

        var archivo = Assert.Single(Directory.GetFiles(_carpeta, "*.json"));
        return JsonElement.Parse(await File.ReadAllTextAsync(archivo));
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public void ESC_197g_Guardar_escribe_un_json_por_corrida_con_los_campos_del_plan()
    {
        var ruta = ArtefactosFaseV.Guardar(_carpeta, "P3-corrida-1", Artefacto(TextoCrudo), LlaveDeLaPrueba);

        var json = JsonElement.Parse(File.ReadAllText(ruta));
        Assert.Equal("¿Cuántas faltas tengo en Bases de Datos?", json.GetProperty("pregunta").GetString());
        Assert.Equal("EST-001", json.GetProperty("usuario").GetProperty("id").GetString());
        Assert.Equal(TextoCrudo, json.GetProperty("textosCrudos")[0].GetString());
        Assert.Equal(FuenteDelArticulo11, json.GetProperty("seccionesEntregadas")[0].GetString());
        Assert.True(json.GetProperty("conResumen").GetBoolean());
        Assert.False(json.GetProperty("conPrelectura").GetBoolean());
        Assert.Equal(1200, json.GetProperty("usage")[0].GetProperty("entrada").GetInt32());
        Assert.Equal(35, json.GetProperty("usage")[0].GetProperty("salida").GetInt32());
        Assert.Equal("Bases de Datos: 2 faltas de 48 clases.", json.GetProperty("respuesta").GetString());
        Assert.Equal("reglamento_academico_2025.md · Artículo 11", json.GetProperty("citas")[0].GetString());
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public void ESC_197g_Guardar_no_deja_la_llave_aunque_aparezca_en_el_texto_crudo()
    {
        var ruta = ArtefactosFaseV.Guardar(_carpeta, "P3-corrida-1", Artefacto($"Mi llave es {LlaveDeLaPrueba}."), LlaveDeLaPrueba);

        Assert.DoesNotContain(LlaveDeLaPrueba, File.ReadAllText(ruta), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public async Task ESC_197g_El_ejecutor_de_la_fase_V_guarda_el_texto_crudo_las_secciones_y_el_usage_de_cada_llamada()
    {
        var json = await EjecutarYLeerAsync(LlaveDeLaPrueba);

        Assert.Equal(TextoCrudo, json.GetProperty("textosCrudos")[0].GetString());

        Assert.True(json.GetProperty("conResumen").GetBoolean());
        Assert.False(json.GetProperty("conPrelectura").GetBoolean());
        Assert.True(json.GetProperty("usage")[0].GetProperty("entrada").GetInt32() > 0);
        Assert.Equal(json.GetProperty("textosCrudos").GetArrayLength(), json.GetProperty("usage").GetArrayLength());
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public async Task ESC_197g_El_ejecutor_guarda_las_secciones_entregadas_al_modelo_y_marca_que_no_hubo_resumen()
    {
        var json = await EjecutarYLeerAsync(llave: null, CasoDeNotaMinima);

        var secciones = json.GetProperty("seccionesEntregadas").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.Equal(["reglamento_academico_2025.md · Artículo 5", "evaluacion_y_calificaciones.md · Sección 3"], secciones);
        Assert.False(json.GetProperty("conResumen").GetBoolean());
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public async Task ESC_197g_El_ejecutor_guarda_la_respuesta_y_las_citas_finales_con_el_documento_y_los_datos()
    {
        var json = await EjecutarYLeerAsync(llave: null);

        var citas = json.GetProperty("citas").EnumerateArray().Select(c => c.GetString()).ToList();
        Assert.Contains("Datos del estudiante · Asistencia", citas);
        Assert.Contains(FuenteDelArticulo11, citas);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("respuesta").GetString()));
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public async Task ESC_197g_Cada_corrida_del_mismo_caso_guarda_su_propio_archivo()
    {
        var ejecutor = new EjecutorFaseV(offline: true, _carpeta, LlaveDeLaPrueba);

        await ejecutor.EjecutarAsync(Caso(), CancellationToken.None);
        await ejecutor.EjecutarAsync(Caso(), CancellationToken.None);

        Assert.Equal(2, Directory.GetFiles(_carpeta, "*.json").Length);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-73")]
    public async Task ESC_197g_El_ejecutor_devuelve_la_ejecucion_http_del_caso_para_que_el_evaluador_la_juzgue()
    {
        var ejecutor = new EjecutorFaseV(offline: true, _carpeta, LlaveDeLaPrueba);

        var ejecucion = await ejecutor.EjecutarAsync(Caso(), CancellationToken.None);

        Assert.Equal(200, ejecucion!.Estado);
        Assert.Empty(Evaluador.Evaluar(Caso(), ejecucion, LlaveDeLaPrueba));
    }
}
