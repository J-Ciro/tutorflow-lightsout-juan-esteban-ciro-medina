using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public sealed record SolicitudAuditoria(
    string IpOrigen,
    string CorrelationId,
    Rol? Rol,
    string? UsuarioId,
    string? Pregunta,
    ResultadoAuditoria Resultado,
    long DuracionMs);

public sealed class AuditorPeticiones
{
    public const int LongitudMaximaPregunta = 1000;

    private readonly IAuditoria _auditoria;
    private readonly IReloj _reloj;

    public AuditorPeticiones(IAuditoria auditoria, IReloj reloj)
    {
        _auditoria = auditoria;
        _reloj = reloj;
    }

    public DateTimeOffset AhoraUtc => _reloj.AhoraUtc;

    public static string SanearPregunta(Usuario usuario, string? pregunta, DirectorioAcademico directorio)
    {
        if (string.IsNullOrEmpty(pregunta))
        {
            return string.Empty;
        }

        var enmascarada = DetectorTerceros.EnmascararTerceros(usuario, pregunta, directorio);
        var sinControl = new string([.. enmascarada.Select(caracter => char.IsControl(caracter) ? ' ' : caracter)]);
        return sinControl.Length > LongitudMaximaPregunta ? sinControl[..LongitudMaximaPregunta] : sinControl;
    }

    public void Registrar(EventoAuditoria evento) => _auditoria.Registrar(evento);

    public void RegistrarSinConsultas(SolicitudAuditoria solicitud)
    {
        var usuario = new Usuario(solicitud.Rol ?? Rol.Estudiante, solicitud.UsuarioId ?? string.Empty);
        _auditoria.Registrar(new EventoAuditoria(
            _reloj.AhoraUtc,
            solicitud.CorrelationId,
            solicitud.IpOrigen,
            solicitud.Rol,
            solicitud.UsuarioId,
            SanearPregunta(usuario, solicitud.Pregunta, ConstructorDirectorio.Vacio),
            solicitud.Resultado,
            null,
            [],
            [],
            [],
            0,
            0m,
            solicitud.DuracionMs,
            false));
    }

    public void RegistrarMultiplesIdentidades(string ipOrigen, IReadOnlyList<string> usuarioIds) =>
        _auditoria.RegistrarMultiplesIdentidades(new EventoMultiplesIdentidades(_reloj.AhoraUtc, ipOrigen, usuarioIds));
}
