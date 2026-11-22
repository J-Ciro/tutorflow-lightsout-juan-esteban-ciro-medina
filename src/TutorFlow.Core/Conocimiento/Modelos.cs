namespace TutorFlow.Core.Conocimiento;

public sealed record Cita(string Documento, string Seccion);

public sealed record SeccionDocumento(string Documento, string Seccion, string Titulo, string Texto);

public sealed record DocumentoConocimiento(string Nombre, bool Vigente, IReadOnlyList<SeccionDocumento> Secciones);
