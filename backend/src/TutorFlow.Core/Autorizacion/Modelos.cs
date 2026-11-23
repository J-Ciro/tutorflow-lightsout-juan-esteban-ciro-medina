using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Autorizacion;

public enum Rol
{
    Estudiante,
    Coordinador
}

public sealed record Usuario(Rol Rol, string Id);

public enum MotivoDenegacion
{
    IdentidadInvalida,
    TerceroMencionado,
    ProgramaAjeno,
    Agregado,
    HerramientaIdAjeno,
    CapacidadAjena
}

public sealed record DecisionAcceso(bool Permitido, MotivoDenegacion? Motivo)
{
    public static DecisionAcceso Permitir() => new(true, null);

    public static DecisionAcceso Denegar(MotivoDenegacion motivo) => new(false, motivo);
}

public sealed record DirectorioAcademico(
    IReadOnlyList<Estudiante> Estudiantes,
    IReadOnlyList<Coordinador> Coordinadores,
    IReadOnlyList<Programa> Programas,
    IReadOnlyCollection<string> CodigosAsignatura);
