using System.Diagnostics;
using System.Globalization;
using Microsoft.Net.Http.Headers;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api;

public sealed partial class ProcesadorPregunta
{
    private const string TipoDeContenidoJson = "application/json";
    private const string IpDesconocida = "desconocida";
    private const string TituloTipoNoSoportado = "El tipo de contenido de la solicitud no está soportado.";
    private const string TituloSolicitudInvalida = "La solicitud no es válida.";
    private const string TituloCuerpoDemasiadoGrande = "El cuerpo de la solicitud supera el tamaño permitido.";
    private const string TituloDemasiadasSolicitudes = "Se superó el límite de preguntas. Intenta de nuevo más tarde.";
    private const string TituloServicioNoDisponible = "El servicio no está disponible en este momento. Intenta de nuevo.";

    private readonly LimitesAsistente _limites;
    private readonly LimitadorTasaPorIp _limitadorTasa;
    private readonly LimitadorIdentidades _limitadorIdentidades;
    private readonly AuditorPeticiones _auditor;
    private readonly PreguntarAsistente _preguntarAsistente;
    private readonly ILogger<ProcesadorPregunta> _logger;

    public ProcesadorPregunta(
        LimitesAsistente limites,
        LimitadorTasaPorIp limitadorTasa,
        LimitadorIdentidades limitadorIdentidades,
        AuditorPeticiones auditor,
        PreguntarAsistente preguntarAsistente,
        ILogger<ProcesadorPregunta> logger)
    {
        _limites = limites;
        _limitadorTasa = limitadorTasa;
        _limitadorIdentidades = limitadorIdentidades;
        _auditor = auditor;
        _preguntarAsistente = preguntarAsistente;
        _logger = logger;
    }

    public async Task<IResult> ProcesarAsync(HttpContext contexto, CancellationToken cancellationToken)
    {
        var peticion = new DatosPeticion(IpDe(contexto), contexto.TraceIdentifier, Stopwatch.StartNew());
        var tasa = _limitadorTasa.Intentar(peticion.Ip);
        return tasa.Permitido
            ? await ProcesarEntradaAsync(contexto, peticion, cancellationToken)
            : await RechazarPorTasaAsync(contexto, peticion, tasa.SegundosParaReintentar, cancellationToken);
    }

    private async Task<IResult> ProcesarEntradaAsync(HttpContext contexto, DatosPeticion peticion, CancellationToken cancellationToken)
    {
        if (!EsJson(contexto.Request.ContentType))
        {
            return RechazarInvalida(peticion, null, StatusCodes.Status415UnsupportedMediaType, TituloTipoNoSoportado);
        }

        var cuerpo = await LectorCuerpo.LeerAsync(contexto.Request.Body, _limites.MaximoBytesCuerpo, cancellationToken);
        if (cuerpo.ExcedeElLimite)
        {
            return RechazarInvalida(peticion, null, StatusCodes.Status413PayloadTooLarge, TituloCuerpoDemasiadoGrande);
        }

        var solicitud = ValidadorSolicitud.Validar(cuerpo.Bytes, _limites.MaximoCaracteresPregunta);
        return solicitud.EsValida
            ? await ResponderAsync(contexto, peticion, solicitud, cancellationToken)
            : RechazarInvalida(peticion, solicitud.Pregunta, StatusCodes.Status400BadRequest, TituloSolicitudInvalida);
    }

    private async Task<IResult> RechazarPorTasaAsync(HttpContext contexto, DatosPeticion peticion, int segundosParaReintentar, CancellationToken cancellationToken)
    {
        var solicitud = await LeerSolicitudParaAuditoriaAsync(contexto, cancellationToken);
        return Rechazar429(contexto, peticion, solicitud, segundosParaReintentar);
    }

    private async Task<SolicitudValidada?> LeerSolicitudParaAuditoriaAsync(HttpContext contexto, CancellationToken cancellationToken)
    {
        if (!EsJson(contexto.Request.ContentType))
        {
            return null;
        }

        var cuerpo = await LectorCuerpo.LeerAsync(contexto.Request.Body, _limites.MaximoBytesCuerpo, cancellationToken);
        return cuerpo.ExcedeElLimite ? null : ValidadorSolicitud.Validar(cuerpo.Bytes, _limites.MaximoCaracteresPregunta);
    }

    private static bool EsJson(string? tipoDeContenido) =>
        MediaTypeHeaderValue.TryParse(tipoDeContenido, out var tipo)
        && string.Equals(tipo.MediaType.Value, TipoDeContenidoJson, StringComparison.OrdinalIgnoreCase);

    private static string IpDe(HttpContext contexto)
    {
        var direccion = contexto.Connection.RemoteIpAddress;
        if (direccion is null)
        {
            return IpDesconocida;
        }

        return (direccion.IsIPv4MappedToIPv6 ? direccion.MapToIPv4() : direccion).ToString();
    }

    private async Task<IResult> ResponderAsync(HttpContext contexto, DatosPeticion peticion, SolicitudValidada solicitud, CancellationToken cancellationToken)
    {
        var usuario = new Usuario(solicitud.Rol!.Value, solicitud.UsuarioId!);
        var identidades = _limitadorIdentidades.Registrar(peticion.Ip, usuario.Id);
        if (identidades.SuperacionNueva)
        {
            _auditor.RegistrarMultiplesIdentidades(peticion.Ip, identidades.Ids);
        }

        if (identidades.Superado && _limites.BloquearMultiplesIdentidades)
        {
            return Rechazar429(contexto, peticion, solicitud, (int)TimeSpan.FromMinutes(_limites.VentanaIdentidadesMinutos).TotalSeconds);
        }

        return await PreguntarAsync(peticion, new PreguntaEntrada(solicitud.Pregunta!, usuario), cancellationToken);
    }

    private async Task<IResult> PreguntarAsync(DatosPeticion peticion, PreguntaEntrada entrada, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _preguntarAsistente.EjecutarAsync(entrada, peticion.Ip, peticion.CorrelationId, cancellationToken);
            return Mapear(resultado);
        }
        catch (Exception excepcion) when (EsFalloDeDependencia(excepcion, cancellationToken))
        {
            RegistrarDependenciaCaida(_logger, excepcion.GetType().Name);
            RegistrarError(peticion, entrada);
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: TituloServicioNoDisponible);
        }
        catch (Exception)
        {
            RegistrarError(peticion, entrada);
            throw;
        }
    }

    private static bool EsFalloDeDependencia(Exception excepcion, CancellationToken cancellationToken) =>
        excepcion is DependenciaNoDisponibleException or HttpRequestException or TimeoutException
        || (excepcion is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private void RegistrarError(DatosPeticion peticion, PreguntaEntrada entrada) =>
        _auditor.RegistrarSinConsultas(new SolicitudAuditoria(
            peticion.Ip,
            peticion.CorrelationId,
            entrada.Usuario.Rol,
            entrada.Usuario.Id,
            entrada.Pregunta,
            ResultadoAuditoria.Error,
            peticion.Cronometro.ElapsedMilliseconds));

    private static IResult Mapear(ResultadoPregunta resultado)
    {
        if (resultado.Respuesta is not { } respuesta)
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: TituloServicioNoDisponible);
        }

        var cuerpo = new PreguntaRespuesta(
            respuesta.Respuesta,
            [.. respuesta.Citas.Select(cita => new CitaRespuesta(cita.Documento, cita.Seccion))],
            respuesta.UsoDatosEstudiante,
            respuesta.Tokens,
            respuesta.CostoUsd);
        var estado = resultado.Tipo == TipoResultadoPregunta.Denegada ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK;
        return Results.Json(cuerpo, statusCode: estado);
    }

    private IResult RechazarInvalida(DatosPeticion peticion, string? pregunta, int estado, string titulo)
    {
        _auditor.RegistrarSinConsultas(new SolicitudAuditoria(
            peticion.Ip,
            peticion.CorrelationId,
            null,
            null,
            pregunta,
            ResultadoAuditoria.Invalida,
            peticion.Cronometro.ElapsedMilliseconds));
        return Results.Problem(statusCode: estado, title: titulo);
    }

    private IResult Rechazar429(HttpContext contexto, DatosPeticion peticion, SolicitudValidada? solicitud, int segundosParaReintentar)
    {
        _auditor.RegistrarSinConsultas(new SolicitudAuditoria(
            peticion.Ip,
            peticion.CorrelationId,
            solicitud?.Rol,
            solicitud?.UsuarioId,
            solicitud?.Pregunta,
            ResultadoAuditoria.Denegada,
            peticion.Cronometro.ElapsedMilliseconds));
        contexto.Response.Headers.RetryAfter = segundosParaReintentar.ToString(CultureInfo.InvariantCulture);
        return Results.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: TituloDemasiadasSolicitudes);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Una dependencia no respondió ({TipoDeError}).")]
    private static partial void RegistrarDependenciaCaida(ILogger logger, string tipoDeError);

    private sealed record DatosPeticion(string Ip, string CorrelationId, Stopwatch Cronometro);
}
