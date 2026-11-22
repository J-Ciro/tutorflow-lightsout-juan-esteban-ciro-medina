using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Auditoria;

public sealed class AuditoriaJsonConsola : IAuditoria
{
    public void Registrar(EventoAuditoria evento) => throw new NotImplementedException();

    public void RegistrarMultiplesIdentidades(EventoMultiplesIdentidades evento) => throw new NotImplementedException();
}
