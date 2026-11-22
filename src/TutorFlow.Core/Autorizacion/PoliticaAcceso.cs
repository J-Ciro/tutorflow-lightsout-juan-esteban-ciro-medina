namespace TutorFlow.Core.Autorizacion;

public sealed class PoliticaAcceso
{
    public DecisionAcceso EvaluarIdentidad(Usuario usuario, DirectorioAcademico directorio) => throw new NotImplementedException();

    public DecisionAcceso EvaluarPregunta(Usuario usuario, string pregunta, DirectorioAcademico directorio) => throw new NotImplementedException();

    public DecisionAcceso EvaluarLecturaEstudiante(Usuario usuario, string estudianteId, DirectorioAcademico directorio) => throw new NotImplementedException();

    public IReadOnlyList<string> ProgramasAutorizados(Usuario usuario, DirectorioAcademico directorio) => throw new NotImplementedException();
}
