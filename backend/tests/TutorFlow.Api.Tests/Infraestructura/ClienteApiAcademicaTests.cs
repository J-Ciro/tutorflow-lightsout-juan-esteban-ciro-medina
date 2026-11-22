using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using TutorFlow.Infrastructure.ApiAcademica;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Infraestructura;

public sealed class ManejadorApiAcademicaFalso : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpRequestMessage> _peticiones = new();
    private readonly JsonElement _datos;
    private readonly Func<string, HttpResponseMessage?> _sobrescribir;

    public ManejadorApiAcademicaFalso(Func<string, HttpResponseMessage?>? sobrescribir = null)
    {
        _datos = JsonElement.Parse(File.ReadAllText(Path.Combine(DatosReferencia.RutaRepositorio(), "mock-api", "datos.json")));
        _sobrescribir = sobrescribir ?? (_ => null);
    }

    public IReadOnlyList<HttpRequestMessage> Peticiones => [.. _peticiones];

    public IReadOnlyList<string> Rutas => [.. _peticiones.Select(p => p.RequestUri!.PathAndQuery)];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _peticiones.Enqueue(request);
        var ruta = request.RequestUri!.AbsolutePath;
        var sobrescrita = _sobrescribir(ruta);
        if (sobrescrita is not null)
        {
            return sobrescrita;
        }

        await Task.CompletedTask;
        return Responder(ruta);
    }

    private HttpResponseMessage Responder(string ruta)
    {
        var partes = ruta.Trim('/').Split('/');
        var cuerpo = partes switch
        {
            ["programas"] => _datos.GetProperty("programas").GetRawText(),
            ["estudiantes", var id] => Uno("estudiantes", id),
            ["coordinadores", var id] => Uno("coordinadores", id),
            ["estudiantes", var id, "notas"] => Filtrar("notas", "estudianteId", id),
            ["estudiantes", var id, "asistencia"] => Filtrar("asistencia", "estudianteId", id),
            ["programas", var id, "estudiantes"] => Filtrar("estudiantes", "programaId", id),
            _ => null
        };
        return cuerpo is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"error":"No encontrado"}""", Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
    }

    private string? Uno(string coleccion, string id) =>
        _datos.GetProperty(coleccion).EnumerateArray().Where(e => e.GetProperty("id").GetString() == Uri.UnescapeDataString(id)).Select(e => e.GetRawText()).FirstOrDefault();

    private string Filtrar(string coleccion, string campo, string id) =>
        "[" + string.Join(",", _datos.GetProperty(coleccion).EnumerateArray().Where(e => e.GetProperty(campo).GetString() == Uri.UnescapeDataString(id)).Select(e => e.GetRawText())) + "]";
}

public sealed class ClienteApiAcademicaTests
{
    private static readonly Uri UrlBase = new("http://api-academica.test:4000/");

    private static (ClienteApiAcademica Cliente, ManejadorApiAcademicaFalso Manejador) Crear(Func<string, HttpResponseMessage?>? sobrescribir = null)
    {
        var manejador = new ManejadorApiAcademicaFalso(sobrescribir);
        return (new ClienteApiAcademica(new HttpClient(manejador) { BaseAddress = UrlBase }), manejador);
    }

    private static HttpResponseMessage Json(string contenido) =>
        new(HttpStatusCode.OK) { Content = new StringContent(contenido, Encoding.UTF8, "application/json") };

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "9.3")]
    public async Task Obtiene_el_estudiante_y_el_coordinador_con_sus_programas()
    {
        var (cliente, _) = Crear();

        var estudiante = await cliente.ObtenerEstudianteAsync("EST-001", CancellationToken.None);
        var coordinador = await cliente.ObtenerCoordinadorAsync("COORD-02", CancellationToken.None);

        Assert.Equal(("EST-001", "Laura Gómez", "ING-SIS"), (estudiante!.Id, estudiante.Nombre, estudiante.ProgramaId));
        Assert.Equal(("COORD-02", "Julián Pardo"), (coordinador!.Id, coordinador.Nombre));
        Assert.Equal(["ADM"], coordinador.ProgramasIds.ToArray());
    }

    [Theory]
    [InlineData("EST-999")]
    [InlineData("EST-100")]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "SEG-03")]
    public async Task Un_estudiante_inexistente_es_nulo(string id)
    {
        var (cliente, _) = Crear();

        Assert.Null(await cliente.ObtenerEstudianteAsync(id, CancellationToken.None));
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "SEG-03")]
    public async Task Un_coordinador_inexistente_es_nulo()
    {
        var (cliente, _) = Crear();

        Assert.Null(await cliente.ObtenerCoordinadorAsync("COORD-99", CancellationToken.None));
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-05")]
    public async Task Las_notas_conservan_el_corte_nulo()
    {
        var (cliente, _) = Crear();

        var notas = await cliente.ObtenerNotasAsync("EST-006", CancellationToken.None);

        Assert.Equal(4, notas.Count);
        var estadistica = notas.Single(n => n.Codigo == "EST-103");
        Assert.Equal(3.9m, estadistica.Cortes.Corte1);
        Assert.Equal(4.1m, estadistica.Cortes.Corte2);
        Assert.Null(estadistica.Cortes.Corte3);
        Assert.Equal(4, estadistica.Creditos);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("RN", "RN-09")]
    public async Task La_asistencia_conserva_las_inasistencias_nulas()
    {
        var (cliente, _) = Crear();

        var asistencia = await cliente.ObtenerAsistenciaAsync("EST-003", CancellationToken.None);

        var ingles = asistencia.Single(a => a.Codigo == "ING-204");
        Assert.Equal(32, ingles.Asistencia.ClasesProgramadas);
        Assert.Null(ingles.Asistencia.Inasistencias);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    public async Task Lista_los_estudiantes_de_un_programa_y_los_programas()
    {
        var (cliente, _) = Crear();

        var estudiantes = await cliente.ObtenerEstudiantesDeProgramaAsync("ADM", CancellationToken.None);
        var programas = await cliente.ObtenerProgramasAsync(CancellationToken.None);

        Assert.Equal(["EST-004", "EST-005", "EST-006"], estudiantes.Select(e => e.Id).Order().ToArray());
        Assert.Equal(["ADM", "ING-SIS"], programas.Select(p => p.Id).Order().ToArray());
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-089")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_089_Un_programa_id_con_formato_invalido_se_descarta()
    {
        var (cliente, _) = Crear(ruta => ruta == "/coordinadores/COORD-01"
            ? Json("""{"id":"COORD-01","nombre":"Marcela Ríos","programas":["../estudiantes/EST-004","ING-SIS","adm","ADMINISTRACION-DE-EMPRESAS-X"]}""")
            : null);

        var coordinador = await cliente.ObtenerCoordinadorAsync("COORD-01", CancellationToken.None);

        Assert.Equal(["ING-SIS"], coordinador!.ProgramasIds.ToArray());
    }

    [Theory]
    [InlineData("EST-001/../EST-002")]
    [InlineData("EST-001?x=1")]
    [InlineData("EST-001/notas")]
    [InlineData("../coordinadores/COORD-01")]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-082")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_082_Un_id_con_caracteres_de_ruta_nunca_cambia_el_recurso_solicitado(string id)
    {
        var (cliente, manejador) = Crear();

        var estudiante = await cliente.ObtenerEstudianteAsync(id, CancellationToken.None);

        Assert.Null(estudiante);
        Assert.All(manejador.Peticiones, p => Assert.Equal(2, p.RequestUri!.AbsolutePath.Trim('/').Split('/').Length));
        Assert.All(manejador.Peticiones, p => Assert.DoesNotContain("?x=1", p.RequestUri!.Query, StringComparison.Ordinal));
        Assert.DoesNotContain(manejador.Rutas, r => r.StartsWith("/coordinadores", StringComparison.Ordinal) || r.EndsWith("/notas", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-13")]
    public async Task Un_error_5xx_de_la_api_falla_sin_reintentos()
    {
        var (cliente, manejador) = Crear(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAnyAsync<Exception>(() => cliente.ObtenerNotasAsync("EST-001", CancellationToken.None));

        Assert.Single(manejador.Peticiones);
    }

    [Fact]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "SEG-07")]
    public async Task Solo_usa_get_y_rutas_relativas_a_la_url_base()
    {
        var (cliente, manejador) = Crear();

        await cliente.ObtenerEstudianteAsync("EST-001", CancellationToken.None);
        await cliente.ObtenerNotasAsync("EST-001", CancellationToken.None);

        Assert.All(manejador.Peticiones, p => Assert.Equal(HttpMethod.Get, p.Method));
        Assert.All(manejador.Peticiones, p => Assert.Equal("api-academica.test", p.RequestUri!.Host));
        Assert.Equal(["/estudiantes/EST-001", "/estudiantes/EST-001/notas"], manejador.Rutas.ToArray());
    }
}
