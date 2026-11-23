using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Infrastructure.Configuracion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests;

public sealed class FabricaApi : WebApplicationFactory<Program>
{
    public const string IpPorDefecto = "10.0.0.1";

    private readonly IReadOnlyDictionary<string, string?> _configuracion;
    private readonly IBaseConocimiento? _conocimiento;
    private readonly bool _reemplazarLlm;
    private readonly HttpMessageHandler? _manejadorHttp;
    private readonly bool _auditoriaReal;
    private readonly IClienteLlm? _clienteLlm;
    private readonly IReadOnlyDictionary<string, string?>? _variablesEntorno;

    public FabricaApi(
        ApiAcademicaDoble? api = null,
        ClienteLlmDoble? llm = null,
        IBaseConocimiento? conocimiento = null,
        IReadOnlyDictionary<string, string?>? configuracion = null,
        bool reemplazarLlm = true,
        HttpMessageHandler? manejadorHttp = null,
        bool conocimientoReal = false,
        bool auditoriaReal = false,
        IClienteLlm? clienteLlm = null,
        IReadOnlyDictionary<string, string?>? variablesEntorno = null)
    {
        _variablesEntorno = variablesEntorno;
        _clienteLlm = clienteLlm;
        Api = api ?? new ApiAcademicaDoble();
        Llm = llm ?? ClienteLlmDoble.Texto("Respuesta de prueba.");
        _conocimiento = ElegirConocimiento(conocimiento, conocimientoReal);
        _configuracion = configuracion ?? SinConfiguracion;
        _reemplazarLlm = reemplazarLlm;
        _manejadorHttp = manejadorHttp;
        _auditoriaReal = auditoriaReal;
    }

    private static readonly Dictionary<string, string?> SinConfiguracion = [];

    public ApiAcademicaDoble Api { get; }

    public ClienteLlmDoble Llm { get; }

    public AuditoriaEnMemoria Auditoria { get; } = new();

    public RelojControlado Reloj { get; } = new();

    public CapturaLogs Logs { get; } = new();

    private static IBaseConocimiento? ElegirConocimiento(IBaseConocimiento? conocimiento, bool real) =>
        real ? null : conocimiento ?? BaseConocimientoFalsa.Vigente();

    public static Dictionary<string, string?> Precios(string entrada, string salida) =>
        new Dictionary<string, string?> { ["LLM_PRECIO_ENTRADA"] = entrada, ["LLM_PRECIO_SALIDA"] = salida };

    public async Task<RespuestaApi> PreguntarAsync(string rol, string id, string pregunta, string ip = IpPorDefecto, IReadOnlyDictionary<string, string>? encabezados = null)
    {
        var cuerpo = JsonSerializer.Serialize(new { pregunta, usuario = new { rol, id } });
        return await EnviarAsync(HttpMethod.Post, "/api/asistente/preguntar", cuerpo, ip, encabezados);
    }

    public Task<RespuestaApi> PreguntarEstudianteAsync(string id, string pregunta, string ip = IpPorDefecto) =>
        PreguntarAsync("estudiante", id, pregunta, ip);

    public Task<RespuestaApi> PreguntarCoordinadorAsync(string id, string pregunta, string ip = IpPorDefecto) =>
        PreguntarAsync("coordinador", id, pregunta, ip);

    public async Task<RespuestaApi> EnviarAsync(HttpMethod metodo, string ruta, string? cuerpo, string ip = IpPorDefecto, IReadOnlyDictionary<string, string>? encabezados = null)
    {
        using var cliente = CreateClient();
        using var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Add(FiltroIpRemota.Encabezado, ip);
        foreach (var (clave, valor) in encabezados ?? new Dictionary<string, string>())
        {
            peticion.Headers.TryAddWithoutValidation(clave, valor);
        }

        if (cuerpo is not null)
        {
            peticion.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        }

        using var respuesta = await cliente.SendAsync(peticion);
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        return new RespuestaApi(respuesta.StatusCode, bytes, respuesta.Headers, respuesta.Content.Headers);
    }

    public async Task<RespuestaApi> EnviarConTipoAsync(string cuerpo, string? tipoContenido, string ip = IpPorDefecto, IReadOnlyDictionary<string, string>? encabezados = null)
    {
        using var cliente = CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/asistente/preguntar");
        peticion.Headers.Add(FiltroIpRemota.Encabezado, ip);
        foreach (var (clave, valor) in encabezados ?? new Dictionary<string, string>())
        {
            peticion.Headers.TryAddWithoutValidation(clave, valor);
        }

        peticion.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(cuerpo));
        if (tipoContenido is not null)
        {
            peticion.Content.Headers.TryAddWithoutValidation("Content-Type", tipoContenido);
        }

        using var respuesta = await cliente.SendAsync(peticion);
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();
        return new RespuestaApi(respuesta.StatusCode, bytes, respuesta.Headers, respuesta.Content.Headers);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseContentRoot(Path.Combine(DatosReferencia.RutaRepositorio(), "backend", "src", "TutorFlow.Api"));
        foreach (var (clave, valor) in _configuracion)
        {
            builder.UseSetting(clave, valor);
        }

        builder.ConfigureAppConfiguration((_, configuracion) => configuracion.AddInMemoryCollection(_configuracion));
        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(Logs);
        });
        builder.ConfigureTestServices(Reemplazar);
    }

    private void Reemplazar(IServiceCollection servicios)
    {
        servicios.RemoveAll<IApiAcademica>();
        servicios.RemoveAll<IReloj>();
        if (_variablesEntorno is not null)
        {
            servicios.RemoveAll<IVariablesEntorno>();
            servicios.AddSingleton<IVariablesEntorno>(new VariablesEntornoFijas(_variablesEntorno));
        }

        servicios.AddSingleton<IApiAcademica>(Api);
        servicios.AddSingleton<IReloj>(Reloj);
        if (!_auditoriaReal)
        {
            servicios.RemoveAll<IAuditoria>();
            servicios.AddSingleton<IAuditoria>(Auditoria);
        }

        servicios.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, FiltroIpRemota>();
        if (_conocimiento is not null)
        {
            servicios.RemoveAll<IBaseConocimiento>();
            servicios.AddSingleton(_conocimiento);
        }

        if (_manejadorHttp is not null)
        {
            servicios.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => _manejadorHttp));
        }

        ReemplazarLlm(servicios);
    }

    private void ReemplazarLlm(IServiceCollection servicios)
    {
        if (!_reemplazarLlm)
        {
            return;
        }

        servicios.RemoveAll<IClienteLlm>();
        servicios.AddSingleton(_clienteLlm ?? Llm);
    }
}

public sealed record RespuestaApi(HttpStatusCode Codigo, byte[] Bytes, HttpResponseHeaders Encabezados, HttpContentHeaders EncabezadosContenido)
{
    public int Estado => (int)Codigo;

    public string Texto => Encoding.UTF8.GetString(Bytes);

    public JsonElement Json => JsonDocument.Parse(Bytes).RootElement.Clone();

    public string? TipoContenido => EncabezadosContenido.ContentType?.ToString();

    public string Respuesta => Json.GetProperty("respuesta").GetString()!;

    public int Tokens => Json.GetProperty("tokens").GetInt32();

    public decimal CostoUsd => Json.GetProperty("costoUsd").GetDecimal();

    public bool UsoDatosEstudiante => Json.GetProperty("usoDatosEstudiante").GetBoolean();

    public IReadOnlyList<string> Citas =>
        Json.GetProperty("citas").EnumerateArray()
            .Select(c => $"{c.GetProperty("documento").GetString()} · {c.GetProperty("seccion").GetString()}")
            .ToList();

    public bool TieneEncabezado(string nombre) =>
        Encabezados.Contains(nombre) || EncabezadosContenido.Contains(nombre);

    public string? Encabezado(string nombre) =>
        Encabezados.TryGetValues(nombre, out var valores) ? valores.FirstOrDefault() : null;
}
