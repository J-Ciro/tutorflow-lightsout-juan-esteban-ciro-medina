using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Infrastructure.Auditoria;

namespace TutorFlow.Api.Tests.Infraestructura;

[Collection("Consola")]
public sealed class AuditoriaCapacidadAjenaConsolaTests
{
    private const string Correlacion = "corr-capacidad-ajena";

    [Fact]
    [Trait("CA", "CA-08")]
    [Trait("SEG", "SEG-04c")]
    [Trait("DECISION", "D-67")]
    public void El_motivo_capacidad_ajena_se_serializa_en_snake_case_de_la_spec()
    {
        var motivo = Enum.Parse<MotivoDenegacion>("CapacidadAjena");
        var evento = new EventoAuditoria(
            new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero),
            Correlacion,
            "10.0.0.5",
            Rol.Estudiante,
            "EST-001",
            "¿Qué puede ver mi coordinador?",
            ResultadoAuditoria.Denegada,
            motivo,
            [],
            [],
            [],
            0,
            0m,
            3,
            false);
        var original = Console.Out;
        using var escritor = new EscritorSincronizado();
        Console.SetOut(escritor);
        try
        {
            new AuditoriaJsonConsola().Registrar(evento);
        }
        finally
        {
            Console.SetOut(original);
        }

        var linea = Assert.Single(escritor.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries), l => l.Contains(Correlacion, StringComparison.Ordinal));
        Assert.Equal("capacidad_ajena", JsonElement.Parse(linea).GetProperty("motivoDenegacion").GetString());
    }
}
