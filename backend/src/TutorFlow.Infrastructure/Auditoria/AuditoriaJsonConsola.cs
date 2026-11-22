using System.Text.Encodings.Web;
using System.Text.Json;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Auditoria;

public sealed class AuditoriaJsonConsola : IAuditoria
{
    private const string NombreEventoMultiplesIdentidades = "multiples_identidades";

    private static readonly JsonSerializerOptions Opciones = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public void Registrar(EventoAuditoria evento) =>
        Console.Out.WriteLine(JsonSerializer.Serialize(Proyectar(evento), Opciones));

    public void RegistrarMultiplesIdentidades(EventoMultiplesIdentidades evento) =>
        Console.Out.WriteLine(JsonSerializer.Serialize(Proyectar(evento), Opciones));

    private static object Proyectar(EventoAuditoria evento) => new
    {
        timestamp = evento.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        correlationId = evento.CorrelationId,
        ipOrigen = evento.IpOrigen,
        usuario = new
        {
            rol = evento.Rol is { } rol ? NombreSnake(rol) : null,
            id = evento.UsuarioId
        },
        pregunta = evento.Pregunta,
        resultado = NombreSnake(evento.Resultado),
        motivoDenegacion = evento.MotivoDenegacion is { } motivo ? NombreSnake(motivo) : null,
        estudiantesConsultados = evento.EstudiantesConsultados,
        recursosConsultados = evento.RecursosConsultados.Select(NombreSnake).ToList(),
        citas = evento.Citas.Select(cita => new { documento = cita.Documento, seccion = cita.Seccion }).ToList(),
        tokens = evento.Tokens,
        costoUsd = evento.CostoUsd,
        duracionMs = evento.DuracionMs,
        precioDesconocido = evento.PrecioDesconocido
    };

    private static object Proyectar(EventoMultiplesIdentidades evento) => new
    {
        evento = NombreEventoMultiplesIdentidades,
        timestamp = evento.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        ipOrigen = evento.IpOrigen,
        usuarioIds = evento.UsuarioIds
    };

    private static string NombreSnake<T>(T valor)
        where T : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(valor.ToString());
}
