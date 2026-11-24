using System.Diagnostics;
using TutorFlow.Api.Tests;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Evals;

public sealed class EjecutorFaseV(bool offline, string carpeta, string? llave) : IEjecutor
{
    private const string CorrelacionEncabezado = "X-Correlation-Id";

    private readonly Dictionary<string, int> _corridas = [];

    public async Task<Ejecucion?> EjecutarAsync(CasoEval caso, CancellationToken cancellationToken)
    {
        if (caso.RequiereLlm && offline && caso.Offline is null)
        {
            return null;
        }

        var corrida = _corridas[caso.Id] = _corridas.GetValueOrDefault(caso.Id) + 1;
        GrabadorLlm? grabador = null;
        using var fabrica = Fabricar(caso, g => grabador = g);
        var reloj = Stopwatch.StartNew();
        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, Cuerpos.Ruta, Cuerpos.De(caso));
        reloj.Stop();
        var artefacto = ArtefactosFaseV.Armar(caso, corrida, grabador, Json.LeerSalida(respuesta.Texto), respuesta.Encabezado(CorrelacionEncabezado));
        ArtefactosFaseV.Guardar(carpeta, $"{TablaFaseV.Pregunta(caso)}-{caso.Id}-corrida-{corrida}", artefacto, llave);
        return new Ejecucion(respuesta.Estado, respuesta.Texto, reloj.Elapsed.TotalMilliseconds, respuesta.Encabezado(CorrelacionEncabezado));
    }

    private FabricaApi Fabricar(CasoEval caso, Action<GrabadorLlm> registrar)
    {
        IClienteLlm Grabar(IClienteLlm interno)
        {
            var grabador = new GrabadorLlm(interno);
            registrar(grabador);
            return grabador;
        }

        return offline
            ? new FabricaApi(llm: EjecutorEnProceso.LlmPara(caso.Offline), conocimientoReal: true, configuracion: FabricaApi.Precios("0.10", "0.50"), envolverLlm: Grabar)
            : new FabricaApi(reemplazarLlm: false, conocimientoReal: true, envolverLlm: Grabar);
    }
}
