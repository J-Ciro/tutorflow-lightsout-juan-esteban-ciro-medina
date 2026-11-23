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

    private readonly ArchivoAuditoriaDiario? _archivo;

    public AuditoriaJsonConsola(ArchivoAuditoriaDiario? archivo = null)
    {
        _archivo = archivo;
    }

    public void Registrar(EventoAuditoria evento) => Emitir(Proyectar(evento));

    public void RegistrarMultiplesIdentidades(EventoMultiplesIdentidades evento) => Emitir(Proyectar(evento));

    private void Emitir(object proyeccion)
    {
        var linea = JsonSerializer.Serialize(proyeccion, Opciones);
        Console.Out.WriteLine(linea);
        _archivo?.Agregar(linea);
    }

    private static Dictionary<string, object?> Proyectar(EventoAuditoria evento)
    {
        var proyeccion = CamposDe(evento);
        if (evento.IdSolicitado is { } idSolicitado)
        {
            proyeccion["idSolicitado"] = idSolicitado;
        }

        if (evento.PrecioDesconocido)
        {
            proyeccion["precioDesconocido"] = true;
        }

        return proyeccion;
    }

    private static Dictionary<string, object?> CamposDe(EventoAuditoria evento) =>
        new()
        {
            ["timestamp"] = evento.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["correlationId"] = evento.CorrelationId,
            ["ipOrigen"] = evento.IpOrigen,
            ["usuario"] = new { rol = evento.Rol is { } rol ? NombreSnake(rol) : null, id = evento.UsuarioId },
            ["pregunta"] = evento.Pregunta,
            ["resultado"] = NombreSnake(evento.Resultado),
            ["motivoDenegacion"] = evento.MotivoDenegacion is { } motivo ? NombreSnake(motivo) : null,
            ["estudiantesConsultados"] = evento.EstudiantesConsultados,
            ["recursosConsultados"] = evento.RecursosConsultados.Select(NombreSnake).ToList(),
            ["datosConsultados"] = evento.DatosConsultados.Select(Proyectar).ToList(),
            ["citas"] = evento.Citas.Select(cita => new { documento = cita.Documento, seccion = cita.Seccion }).ToList(),
            ["tokens"] = evento.Tokens,
            ["costoUsd"] = evento.CostoUsd,
            ["duracionMs"] = evento.DuracionMs
        };

    private static Dictionary<string, string> Proyectar(DatoConsultado dato)
    {
        var proyeccion = new Dictionary<string, string>
        {
            ["estudianteId"] = dato.EstudianteId,
            ["programaId"] = dato.ProgramaId,
            ["recurso"] = NombreSnake(dato.Recurso)
        };
        if (dato.Asignatura is { } asignatura)
        {
            proyeccion["asignatura"] = asignatura;
        }

        return proyeccion;
    }

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
