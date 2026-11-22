using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Infrastructure.Conocimiento;

public sealed class BaseConocimientoEnMemoria : IBaseConocimiento
{
    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo) => throw new NotImplementedException();

    public bool ExisteCita(Cita cita) => throw new NotImplementedException();
}
