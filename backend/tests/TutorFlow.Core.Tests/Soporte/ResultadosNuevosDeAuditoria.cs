using TutorFlow.Core.Asistente;

namespace TutorFlow.Pruebas.Soporte;

public static class ResultadosNuevosDeAuditoria
{
    public static ResultadoAuditoria EstadoNoVerificado =>
        Enum.TryParse<ResultadoAuditoria>("EstadoNoVerificado", out var resultado)
            ? resultado
            : throw new InvalidOperationException("ResultadoAuditoria no define EstadoNoVerificado (estado_no_verificado, plan §12).");
}
