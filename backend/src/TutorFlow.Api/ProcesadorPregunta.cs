using System.Diagnostics;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api;

public sealed partial class ProcesadorPregunta
{
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
        if (!tasa.Permitido)
        {
            return Rechazar429(contexto, peticion, null, tasa.SegundosParaReintentar);
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

    private static string IpDe(HttpContext contexto)
    {
        var direccion = contexto.Connection.RemoteIpAddress;
        if (direccion is null)
        {
            return "desconocida";
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
        contexto.Response.Headers.RetryAfter = segundosParaReintentar.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Results.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: TituloDemasiadasSolicitudes);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Una dependencia no respondió ({TipoDeError}).")]
    private static partial void RegistrarDependenciaCaida(ILogger logger, string tipoDeError);

    private sealed record DatosPeticion(string Ip, string CorrelationId, Stopwatch Cronometro);
}
