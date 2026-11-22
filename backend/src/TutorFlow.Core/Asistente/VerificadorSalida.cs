using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed record SalidaVerificada(string Respuesta, IReadOnlyList<Cita> Citas, ResultadoAuditoria Resultado);

public sealed class VerificadorSalida
{
    public SalidaVerificada Verificar(
        Usuario usuario,
        string respuesta,
        IReadOnlyList<Cita> citas,
        IReadOnlyCollection<string> idsPersonasAutorizadas,
        IReadOnlyCollection<string> textosFuente,
        IBaseConocimiento baseConocimiento,
        DirectorioAcademico directorio) => throw new NotImplementedException();

    public IReadOnlyList<Cita> DescartarCitasInexistentes(IReadOnlyList<Cita> citas, IBaseConocimiento baseConocimiento) => throw new NotImplementedException();

    public bool ContienePersonaNoAutorizada(string respuesta, IReadOnlyCollection<string> idsPersonasAutorizadas, DirectorioAcademico directorio) => throw new NotImplementedException();

    public bool TodosLosNumerosSonVerificables(string respuesta, IReadOnlyCollection<string> textosFuente) => throw new NotImplementedException();
}
