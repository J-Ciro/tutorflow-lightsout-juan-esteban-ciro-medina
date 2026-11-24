using System.Globalization;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Auditoria;

public sealed class ArchivoAuditoriaDiario
{
    public const string ClaveRuta = "Auditoria:Ruta";

    private const string FormatoFecha = "yyyy-MM-dd";
    private const string MensajeErrorEscritura = "No se pudo escribir la auditoría en archivo";

    private readonly string? _ruta;
    private readonly IReloj _reloj;
    private readonly object _candado = new();

    public ArchivoAuditoriaDiario(string? ruta, IReloj reloj)
    {
        _ruta = string.IsNullOrWhiteSpace(ruta) ? null : ruta;
        _reloj = reloj;
    }

    public void Agregar(string linea)
    {
        if (_ruta is null)
        {
            return;
        }

        try
        {
            lock (_candado)
            {
                Directory.CreateDirectory(_ruta);
                File.AppendAllText(RutaDelDia(), linea + "\n");
            }
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"{MensajeErrorEscritura}: {excepcion.GetType().Name}");
        }
    }

    private string RutaDelDia() =>
        Path.Combine(_ruta!, $"auditoria-{_reloj.AhoraUtc.UtcDateTime.ToString(FormatoFecha, CultureInfo.InvariantCulture)}.jsonl");
}
