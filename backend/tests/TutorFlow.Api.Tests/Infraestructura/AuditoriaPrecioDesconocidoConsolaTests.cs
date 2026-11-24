using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Infrastructure.Auditoria;

namespace TutorFlow.Api.Tests.Infraestructura;

[Collection("Consola")]
public sealed class AuditoriaPrecioDesconocidoConsolaTests
{
    private const string CampoPrecioDesconocido = "precioDesconocido";
    private static readonly DateTimeOffset Instante = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private static EventoAuditoria Evento(string correlacion, bool precioDesconocido) =>
        new(
            Instante,
            correlacion,
            "10.0.0.5",
            Rol.Estudiante,
            "EST-001",
            "¿Cuál es mi promedio?",
            ResultadoAuditoria.Respondida,
            null,
            ["EST-001"],
            [RecursoConsultado.Notas],
            [],
            1200,
            0m,
            35,
            precioDesconocido);

    private static JsonElement Emitir(EventoAuditoria evento)
    {
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

        return JsonElement.Parse(escritor.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Single(l => l.Contains(evento.CorrelationId, StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("ESC", "ESC-180")]
    [Trait("SEG", "11.3")]
    public void Un_evento_con_modelo_sin_precio_lleva_precio_desconocido_true_y_costo_cero()
    {
        var json = Emitir(Evento("corr-sin-precio", precioDesconocido: true));

        Assert.True(json.GetProperty(CampoPrecioDesconocido).GetBoolean());
        Assert.Equal(0m, json.GetProperty("costoUsd").GetDecimal());
    }

    [Fact]
    [Trait("CA", "CA-11")]
    [Trait("ESC", "ESC-180")]
    [Trait("SEG", "11.3")]
    public void Un_evento_con_precio_conocido_omite_precio_desconocido()
    {
        var json = Emitir(Evento("corr-con-precio", precioDesconocido: false));

        Assert.False(json.TryGetProperty(CampoPrecioDesconocido, out _));
    }
}
