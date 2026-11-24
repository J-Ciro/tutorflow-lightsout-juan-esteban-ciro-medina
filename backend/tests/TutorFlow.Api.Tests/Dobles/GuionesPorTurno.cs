using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Dobles;

public static class GuionesPorTurno
{
    public const string Prefijo = "Tu respuesta anterior no se pudo verificar: ";

    public static bool EsReintento(SolicitudLlm solicitud) =>
        solicitud.Mensajes.Any(m => m.Rol == RolMensaje.Usuario && m.Contenido.StartsWith(Prefijo, StringComparison.Ordinal));

    public static int Reintentos(ClienteLlmDoble llm) => llm.Solicitudes.Count(EsReintento);

    public static ClienteLlmDoble Texto(string primerTurno, string reintento, UsoTokens? uso = null) =>
        new((solicitud, _) => new RespuestaLlm(EsReintento(solicitud) ? reintento : primerTurno, [], uso ?? ClienteLlmDoble.UsoPorDefecto, false));

    public static ClienteLlmDoble LeeElProgramaYResponde(string primerTurno, string reintento, UsoTokens? uso = null) =>
        new((solicitud, _) =>
        {
            var consumo = uso ?? ClienteLlmDoble.UsoPorDefecto;
            if (EsReintento(solicitud))
            {
                return new RespuestaLlm(reintento, [], consumo, false);
            }

            var yaLeyo = solicitud.Mensajes.Any(m => m.Rol == RolMensaje.Herramienta);
            var herramienta = solicitud.Herramientas.FirstOrDefault(h => h.Parametros.Count == 0);
            return !yaLeyo && herramienta is not null
                ? new RespuestaLlm(null, [new LlamadaHerramienta("lee-programa", herramienta.Nombre, "{}")], consumo, false)
                : new RespuestaLlm(primerTurno, [], consumo, false);
        });
}

public sealed class ClienteLlmConDemoraPorTurno : IClienteLlm
{
    private readonly RelojControlado _reloj;
    private readonly TimeSpan _primerTurno;
    private readonly TimeSpan _reintento;
    private readonly string _textoPrimerTurno;
    private readonly string _textoReintento;
    private int _llamadas;

    public ClienteLlmConDemoraPorTurno(RelojControlado reloj, TimeSpan primerTurno, TimeSpan reintento, string textoPrimerTurno, string textoReintento)
    {
        _reloj = reloj;
        _primerTurno = primerTurno;
        _reintento = reintento;
        _textoPrimerTurno = textoPrimerTurno;
        _textoReintento = textoReintento;
    }

    public int Llamadas => Volatile.Read(ref _llamadas);

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _llamadas);
        var esReintento = GuionesPorTurno.EsReintento(solicitud);
        await _reloj.Pausar(esReintento ? _reintento : _primerTurno, cancellationToken);
        return new RespuestaLlm(esReintento ? _textoReintento : _textoPrimerTurno, [], ConsumoRealista.UsoDe(solicitud, ClienteLlmDoble.UsoPorDefecto.Salida), false);
    }
}
