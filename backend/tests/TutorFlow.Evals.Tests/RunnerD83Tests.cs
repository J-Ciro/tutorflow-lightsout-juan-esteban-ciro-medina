using System.Globalization;
using System.Text.Json;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Evals.Tests;

public sealed class RunnerD83Tests : IDisposable
{
    private const int CorridasDeLaFaseV = 5;
    private const string LlaveDeLaPrueba = "sk-ant-canario-de-la-prueba-0123456789";

    private const string CasoDePromedio = """
        {
          "id": "ESC-002",
          "fuente": "CA-01 pregunta 2",
          "usuario": { "rol": "estudiante", "id": "EST-001" },
          "pregunta": "¿Cuál es mi promedio ponderado del periodo?",
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
            "texto": "Tu promedio es 4.39. Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0."
          }
        }
        """;

    private const string CasoDeProgramaPerdidas = """
        {
          "id": "ESC-006",
          "fuente": "CA-01 pregunta 6",
          "usuario": { "rol": "coordinador", "id": "COORD-02" },
          "pregunta": "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?",
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
            "texto": "Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)"
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

    private static readonly string RespuestaSinInformacion = JsonSerializer.Serialize(new
    {
        respuesta = Textos.SinInformacion,
        citas = Array.Empty<object>(),
        usoDatosEstudiante = false,
        tokens = 0,
        costoUsd = 0
    });

    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "tutorflow-evals-d83-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    private static CasoEval Caso(string json) => JsonSerializer.Deserialize<CasoEval>(json, Json.Opciones)!;

    private async Task<JsonElement> ArtefactoDeLaEjecucionAsync(string casoJson)
    {
        var ejecutor = new EjecutorFaseV(offline: true, _carpeta, llave: null);
        await ejecutor.EjecutarAsync(Caso(casoJson), CancellationToken.None);
        return JsonElement.Parse(await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(_carpeta, "*.json"))));
    }

    private static string TextoDe(JsonElement artefacto, string propiedad) =>
        artefacto.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString()! : string.Empty;

    private string[] Archivos() =>
        Directory.Exists(_carpeta) ? Directory.GetFiles(_carpeta, "*.json") : [];

    private List<JsonElement> Artefactos() =>
        [.. Archivos().Select(ruta => JsonElement.Parse(File.ReadAllText(ruta)))];

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197g_El_artefacto_guarda_el_texto_del_resumen_entregado_al_modelo()
    {
        var artefacto = await ArtefactoDeLaEjecucionAsync(CasoDePromedio);

        var resumen = artefacto.GetProperty("textoResumen").GetString();
        Assert.Contains("Definitivas: Cálculo Integral 4.5", resumen, StringComparison.Ordinal);
        Assert.Contains("Promedio ponderado del periodo: 4.39", resumen, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197g_El_artefacto_guarda_el_texto_de_la_prelectura_del_coordinador()
    {
        var artefacto = await ArtefactoDeLaEjecucionAsync(CasoDeProgramaPerdidas);

        var prelectura = artefacto.GetProperty("textoPrelectura").GetString();
        Assert.Contains("Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)", prelectura, StringComparison.Ordinal);
        Assert.True(artefacto.GetProperty("conPrelectura").GetBoolean());
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197g_Sin_resumen_ni_prelectura_los_textos_del_artefacto_quedan_vacios()
    {
        var artefacto = await ArtefactoDeLaEjecucionAsync(CasoDeNotaMinima);

        Assert.Equal(string.Empty, TextoDe(artefacto, "textoResumen"));
        Assert.Equal(string.Empty, TextoDe(artefacto, "textoPrelectura"));
        Assert.False(artefacto.GetProperty("conResumen").GetBoolean());
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197h_Con_fase_V_y_url_las_peticiones_llegan_al_servidor_levantado()
    {
        using var servidor = new ServidorHttpFalso(RespuestaSinInformacion);

        await Principal.Main(["--fase-v", "--url", servidor.Url, "--filtro", "ESC-009", "--artefactos", _carpeta]);

        Assert.Equal(CorridasDeLaFaseV, servidor.Cuerpos.Count);
        Assert.All(servidor.Cuerpos, cuerpo => Assert.Contains("parqueaderos", cuerpo, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197h_Con_fase_V_y_url_el_artefacto_lleva_el_texto_de_la_respuesta_http_sin_texto_crudo()
    {
        using var servidor = new ServidorHttpFalso(RespuestaSinInformacion);

        await Principal.Main(["--fase-v", "--url", servidor.Url, "--filtro", "ESC-009", "--artefactos", _carpeta]);

        var artefactos = Artefactos();
        Assert.Equal(CorridasDeLaFaseV, artefactos.Count);
        Assert.All(artefactos, a => Assert.Equal(Textos.SinInformacion, a.GetProperty("respuesta").GetString()));
        Assert.All(artefactos, a => Assert.True(!a.TryGetProperty("textosCrudos", out var crudos) || crudos.GetArrayLength() == 0));
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197h_Con_fase_V_y_url_el_resultado_se_juzga_con_la_respuesta_del_servidor()
    {
        using var servidor = new ServidorHttpFalso("""{"respuesta":"Texto inventado por el servidor.","citas":[],"usoDatosEstudiante":false,"tokens":0,"costoUsd":0}""");

        var salida = await Principal.Main(["--fase-v", "--url", servidor.Url, "--filtro", "ESC-009", "--artefactos", _carpeta]);

        Assert.Equal(CorridasDeLaFaseV, servidor.Cuerpos.Count);
        Assert.NotEqual(0, salida);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197h_El_modo_por_caso_con_url_guarda_el_texto_de_cada_respuesta()
    {
        using var servidor = new ServidorHttpFalso(RespuestaSinInformacion);

        await Principal.Main(["--url", servidor.Url, "--filtro", "ESC-009", "--artefactos", _carpeta]);

        var artefacto = Assert.Single(Artefactos());
        Assert.Equal(Textos.SinInformacion, artefacto.GetProperty("respuesta").GetString());
        Assert.Equal("¿Qué dice el reglamento sobre el uso de parqueaderos?", artefacto.GetProperty("pregunta").GetString());
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-83")]
    public async Task ESC_197h_El_modo_por_caso_con_url_no_deja_la_llave_en_los_artefactos()
    {
        using var servidor = new ServidorHttpFalso(RespuestaSinInformacion.Replace("No encuentro", LlaveDeLaPrueba, StringComparison.Ordinal));
        Environment.SetEnvironmentVariable("LLM_API_KEY", LlaveDeLaPrueba);

        try
        {
            await Principal.Main(["--url", servidor.Url, "--filtro", "ESC-009", "--artefactos", _carpeta]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LLM_API_KEY", null);
        }

        var archivos = Archivos();
        Assert.NotEmpty(archivos);
        Assert.All(archivos, ruta => Assert.DoesNotContain(LlaveDeLaPrueba, File.ReadAllText(ruta), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-197")]
    [Trait("D", "D-85")]
    public void ESC_197f_La_tabla_de_la_fase_V_no_tiene_la_columna_de_semilla()
    {
        var entorno = EntornoFaseV.Leer(offline: true, Directory.GetCurrentDirectory());
        using var salida = new StringWriter(CultureInfo.InvariantCulture);

        TablaFaseV.Imprimir([], entorno, salida);

        Assert.DoesNotContain("Semilla", salida.ToString(), StringComparison.Ordinal);
        Assert.Contains("Llm:Temperatura", salida.ToString(), StringComparison.Ordinal);
    }
}
