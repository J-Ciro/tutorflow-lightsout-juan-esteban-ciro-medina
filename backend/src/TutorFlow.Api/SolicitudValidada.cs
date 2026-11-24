using System.Text.Json;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Api;

public sealed record SolicitudValidada(bool EsValida, string? Pregunta, Rol? Rol, string? UsuarioId)
{
    public static SolicitudValidada Invalida(string? pregunta = null) => new(false, pregunta, null, null);
}

public static class ValidadorSolicitud
{
    private const string RolEstudiante = "estudiante";
    private const string RolCoordinador = "coordinador";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    public static SolicitudValidada Validar(byte[] cuerpo, int maximoCaracteresPregunta)
    {
        var solicitud = Deserializar(cuerpo);
        if (solicitud is null)
        {
            return SolicitudValidada.Invalida();
        }

        return ValidarCampos(solicitud, maximoCaracteresPregunta);
    }

    private static SolicitudValidada ValidarCampos(PreguntaSolicitud solicitud, int maximoCaracteresPregunta)
    {
        var usuario = solicitud.Usuario ?? new UsuarioSolicitud(null, null);
        var rol = ConvertirRol(usuario.Rol);
        var camposValidos = TieneFormaDeId(usuario.Id) && EsPreguntaValida(solicitud.Pregunta, maximoCaracteresPregunta);
        return rol is not null && camposValidos
            ? new SolicitudValidada(true, solicitud.Pregunta, rol, usuario.Id)
            : SolicitudValidada.Invalida(solicitud.Pregunta);
    }

    private static PreguntaSolicitud? Deserializar(byte[] cuerpo)
    {
        try
        {
            return JsonSerializer.Deserialize<PreguntaSolicitud>(cuerpo, OpcionesJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Rol? ConvertirRol(string? rol) => rol switch
    {
        RolEstudiante => Rol.Estudiante,
        RolCoordinador => Rol.Coordinador,
        _ => null
    };

    private static bool TieneFormaDeId(string? id) =>
        Identificadores.EsIdEstudiante(id) || Identificadores.EsIdCoordinador(id);

    private static bool EsPreguntaValida(string? pregunta, int maximo) =>
        !string.IsNullOrWhiteSpace(pregunta) && pregunta.Length <= maximo;
}
