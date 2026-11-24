using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public sealed record SolicitudAuditoria(
    string IpOrigen,
    string CorrelationId,
    Rol? Rol,
    string? UsuarioId,
    string? Pregunta,
    ResultadoAuditoria Resultado,
    long DuracionMs,
    int? BloqueoSegundos = null);

public sealed class AuditorPeticiones
{
    public const int LongitudMaximaPregunta = 1000;
    public const string PreguntaNoEvaluada = "[NO_EVALUADA]";

    private readonly IAuditoria _auditoria;
    private readonly IReloj _reloj;
    private DirectorioAcademico? _personasConocidas;

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

        return Limpiar(DetectorTerceros.EnmascararTerceros(usuario, pregunta, directorio));
    }

    public void RecordarPersonas(DirectorioAcademico directorio) => Volatile.Write(ref _personasConocidas, directorio);

    public string PreguntaSinIdentidad(string? pregunta)
    {
        var personas = Volatile.Read(ref _personasConocidas);
        return pregunta is null || personas is null
            ? PreguntaNoEvaluada
            : Limpiar(DetectorTerceros.EnmascararPersonas(pregunta, personas));
    }

    private static string Limpiar(string enmascarada)
    {
        var sinControl = new string([.. enmascarada.Select(caracter => char.IsControl(caracter) ? ' ' : caracter)]);
        return sinControl.Length > LongitudMaximaPregunta ? sinControl[..LongitudMaximaPregunta] : sinControl;
    }

    public void Registrar(EventoAuditoria evento) => _auditoria.Registrar(evento);

    public void RegistrarSinConsultas(SolicitudAuditoria solicitud)
    {
        _auditoria.Registrar(new EventoAuditoria(
            _reloj.AhoraUtc,
            solicitud.CorrelationId,
            solicitud.IpOrigen,
            solicitud.Rol,
            solicitud.UsuarioId,
            PreguntaSinIdentidad(solicitud.Pregunta),
            solicitud.Resultado,
            null,
            [],
            [],
            [],
            0,
            0m,
            solicitud.DuracionMs,
            false)
        {
            BloqueoSegundos = solicitud.BloqueoSegundos
        });
    }

    public void RegistrarMultiplesIdentidades(string ipOrigen, IReadOnlyList<string> usuarioIds) =>
        _auditoria.RegistrarMultiplesIdentidades(new EventoMultiplesIdentidades(_reloj.AhoraUtc, ipOrigen, usuarioIds));
}
