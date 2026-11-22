namespace TutorFlow.Core.Academico;

public sealed record Programa(string Id, string Nombre, string CoordinadorId);

public sealed record Coordinador(string Id, string Nombre, IReadOnlyList<string> ProgramasIds);

public sealed record Estudiante(string Id, string Nombre, string ProgramaId);

public sealed record NotasCorte(decimal? Corte1, decimal? Corte2, decimal? Corte3);

public sealed record Asistencia(int ClasesProgramadas, int? Inasistencias);

public sealed record NotasAsignatura(string Codigo, string Nombre, int Creditos, NotasCorte Cortes);

public sealed record AsistenciaAsignatura(string Codigo, Asistencia Asistencia);

public sealed record Asignatura(string Codigo, string Nombre, int Creditos, NotasCorte Cortes, Asistencia Asistencia);

public enum EstadoAsignatura
{
    Aprobada,
    Perdida,
    PerdidaPorInasistencia,
    SinDefinitiva
}

public enum EstadoAsistencia
{
    Registrada,
    SinRegistro
}

public enum ElegibilidadBeca
{
    Elegible,
    NoElegible,
    NoDeterminable
}

public sealed record NotaAcumulada(decimal Valor, decimal PorcentajeEvaluado, decimal EquivalenteSobreEvaluado);

public sealed record ResultadoAsignatura(
    Asignatura Asignatura,
    decimal? Definitiva,
    EstadoAsignatura Estado,
    bool Habilitable,
    EstadoAsistencia EstadoAsistencia,
    int FaltasPermitidas,
    int? FaltasRestantes,
    NotaAcumulada? Acumulada);

public sealed record ResumenEstudiante(
    string EstudianteId,
    IReadOnlyList<ResultadoAsignatura> Asignaturas,
    decimal? Promedio,
    bool PruebaAcademica,
    int CreditosMatriculados,
    ElegibilidadBeca Beca);
