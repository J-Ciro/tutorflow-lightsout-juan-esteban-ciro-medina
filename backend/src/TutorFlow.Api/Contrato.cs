namespace TutorFlow.Api;

public sealed record UsuarioSolicitud(string? Rol, string? Id);

public sealed record PreguntaSolicitud(string? Pregunta, UsuarioSolicitud? Usuario);

public sealed record CitaRespuesta(string Documento, string Seccion);

public sealed record PreguntaRespuesta(
    string Respuesta,
    IReadOnlyList<CitaRespuesta> Citas,
    bool UsoDatosEstudiante,
    int Tokens,
    decimal CostoUsd);

public sealed record EstadoSalud(string Estado);
