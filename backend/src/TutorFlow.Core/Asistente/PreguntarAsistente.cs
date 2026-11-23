using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed record PreguntaEntrada(string Pregunta, Usuario Usuario);

public sealed record RespuestaAsistente(
    string Respuesta,
    IReadOnlyList<Cita> Citas,
    bool UsoDatosEstudiante,
    int Tokens,
    decimal CostoUsd);

public enum TipoResultadoPregunta
{
    Respondida,
    Denegada,
    NoDisponible,
    LimiteIdentidades
}

public sealed record ResultadoPregunta(
    TipoResultadoPregunta Tipo,
    RespuestaAsistente? Respuesta,
    ResultadoAuditoria Auditoria,
    MotivoDenegacion? Motivo)
{
    public int SegundosParaReintentar { get; init; }
}

public sealed class PreguntarAsistente
{
    private readonly IApiAcademica _apiAcademica;
    private readonly IClienteLlm _clienteLlm;
    private readonly IBaseConocimiento _baseConocimiento;
    private readonly AuditorPeticiones _auditor;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly LimitesAsistente _limites;
    private readonly ConfiguracionAsistente _configuracion;
    private readonly PresupuestoDiario _presupuesto;
    private readonly LectorResumenPropio _lectorResumen;
    private readonly RelevanciaPrevia _relevancia;
    private readonly EmisorDeResultados _emisor;
    private readonly FaseDelModelo _fase;
    private readonly LimitadorIdentidades? _limitadorIdentidades;

    public PreguntarAsistente(
        IApiAcademica apiAcademica,
        IClienteLlm clienteLlm,
        IBaseConocimiento baseConocimiento,
        AuditorPeticiones auditor,
        PoliticaAcceso politicaAcceso,
        VerificadorSalida verificadorSalida,
        LimitesAsistente limites,
        ConfiguracionAsistente configuracion,
        PresupuestoDiario presupuesto,
        LimitadorIdentidades? limitadorIdentidades = null)
    {
        _limitadorIdentidades = limitadorIdentidades;
        _apiAcademica = apiAcademica;
        _clienteLlm = clienteLlm;
        _baseConocimiento = baseConocimiento;
        _auditor = auditor;
        _politicaAcceso = politicaAcceso;
        _limites = limites;
        _configuracion = configuracion;
        _presupuesto = presupuesto;
        _lectorResumen = new LectorResumenPropio(politicaAcceso);
        _relevancia = new RelevanciaPrevia(politicaAcceso, configuracion.RelevanciaMinima);
        _emisor = new EmisorDeResultados(auditor, politicaAcceso, configuracion);
        _fase = new FaseDelModelo(clienteLlm, politicaAcceso, limites, configuracion, presupuesto, _emisor, new CierreDeRespuesta(baseConocimiento, politicaAcceso, verificadorSalida, configuracion));
    }

    public async Task<ResultadoPregunta> EjecutarAsync(
        PreguntaEntrada entrada,
        string ipOrigen,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var presupuestoApi = TimeSpan.FromSeconds(_limites.PresupuestoApiAcademicaSegundos);
        var api = new ApiAcademicaConPresupuesto(_apiAcademica, presupuestoApi);
        var sesion = new SesionPregunta(entrada, ipOrigen, correlationId, api);
        try
        {
            return await ResponderSesionAsync(sesion, cancellationToken);
        }
        catch (Exception)
        {
            _emisor.Auditar(sesion, ResultadoAuditoria.Error, null, []);
            throw;
        }
    }

    private async Task<ResultadoPregunta> ResponderSesionAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var directorio = await ConstructorDirectorio.ConstruirAsync(sesion.Entrada.Usuario, sesion.Api, _configuracion.CodigosAsignatura, cancellationToken);
        if (directorio is not null)
        {
            _auditor.RecordarPersonas(directorio);
            sesion.Directorio = ExcluirCodigosDePersonas(directorio);
        }

        var identidad = _politicaAcceso.EvaluarIdentidad(sesion.Entrada.Usuario, sesion.Directorio);
        if (!identidad.Permitido)
        {
            return _emisor.Denegar(sesion, identidad.Motivo, 0);
        }

        return ControlarIdentidades(sesion) ?? await ResponderSiEstaAutorizadoAsync(sesion, cancellationToken);
    }

    private async Task<ResultadoPregunta> ResponderSiEstaAutorizadoAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var decision = _politicaAcceso.EvaluarPregunta(sesion.Entrada.Usuario, sesion.Entrada.Pregunta, sesion.Directorio);
        return decision.Permitido
            ? await ResponderSiEsRelevanteAsync(sesion, cancellationToken)
            : _emisor.Denegar(sesion, decision.Motivo, 0);
    }

    private ResultadoPregunta? ControlarIdentidades(SesionPregunta sesion)
    {
        if (_limitadorIdentidades is null)
        {
            return null;
        }

        var identidades = _limitadorIdentidades.Registrar(sesion.IpOrigen, sesion.Entrada.Usuario.Id);
        var bloqueada = identidades.Superado && _limites.BloquearMultiplesIdentidades;
        if (identidades.SuperacionNueva)
        {
            _auditor.RegistrarMultiplesIdentidades(sesion.IpOrigen, identidades.Ids, bloqueada);
        }

        return bloqueada ? RechazarPorIdentidades(sesion, identidades.SegundosParaReintentar) : null;
    }

    private ResultadoPregunta RechazarPorIdentidades(SesionPregunta sesion, int segundosParaReintentar)
    {
        var usuario = sesion.Entrada.Usuario;
        _auditor.RegistrarSinConsultas(new SolicitudAuditoria(
            sesion.IpOrigen,
            sesion.CorrelationId,
            usuario.Rol,
            usuario.Id,
            sesion.Entrada.Pregunta,
            ResultadoAuditoria.Denegada,
            sesion.Cronometro.ElapsedMilliseconds));
        return new ResultadoPregunta(TipoResultadoPregunta.LimiteIdentidades, null, ResultadoAuditoria.Denegada, null)
        {
            SegundosParaReintentar = segundosParaReintentar
        };
    }

    private DirectorioAcademico ExcluirCodigosDePersonas(DirectorioAcademico directorio)
    {
        var catalogo = _configuracion.Catalogo;
        for (var indice = 0; indice < catalogo.Count; indice++)
        {
            if (CatalogoAsignaturas.EsIdDePersona(catalogo[indice].Codigo, directorio))
            {
                _configuracion.AdvertirUnaVez($"{Textos.PrefijoConfiguracionIgnorada}: Autorizacion:Asignaturas:{indice}:Codigo = \"{catalogo[indice].Codigo}\" (coincide con el ID de una persona del registro); se excluye del catálogo");
            }
        }

        return directorio with { CodigosAsignatura = [.. directorio.CodigosAsignatura.Where(codigo => !CatalogoAsignaturas.EsIdDePersona(codigo, directorio))] };
    }

    private async Task<ResultadoPregunta> ResponderSiEsRelevanteAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var pregunta = sesion.Entrada.Pregunta;
        var contexto = new ContextoUsuario(sesion.Entrada.Usuario, sesion.Directorio);
        sesion.Secciones = _baseConocimiento.BuscarSecciones(ConsultaDeRecuperacion.DeTexto(pregunta), _limites.MaximoSeccionesRecuperadas);
        var activacion = ActivadorResumenPropio.Evaluar(contexto, pregunta, CatalogoAsignaturas.Vigente(_configuracion.Catalogo, sesion.Directorio));
        if (_relevancia.PeriodoNoDisponible(contexto, pregunta, activacion))
        {
            return _emisor.SinInformacion(sesion);
        }

        sesion.Activacion = await _lectorResumen.VerificarMatriculaAsync(contexto, activacion, sesion.Api, sesion.Registro, cancellationToken);
        return _relevancia.DebeResponderSinInformacion(contexto, pregunta, sesion.Secciones, sesion.Activacion)
            ? _emisor.SinInformacion(sesion)
            : await ResponderConReservaAsync(sesion, cancellationToken);
    }

    private async Task<ResultadoPregunta> ResponderConReservaAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var reserva = _configuracion.ReservaPorPeticionUsd;
        if (!_presupuesto.TryReservar(reserva))
        {
            return _emisor.NoDisponible(sesion, ResultadoAuditoria.Error) with { SegundosParaReintentar = _presupuesto.SegundosHastaElReinicio() };
        }

        sesion.ReservaHecha = true;
        try
        {
            return await ResponderConTimeoutAsync(sesion, cancellationToken);
        }
        finally
        {
            Sustituir(sesion, reserva);
        }
    }

    private void Sustituir(SesionPregunta sesion, decimal reserva)
    {
        _presupuesto.Sustituir(reserva, _configuracion.GastoDe(sesion.Uso.Valor, 1 + sesion.ReservasExtra));
        for (var extra = 0; extra < sesion.ReservasExtra; extra++)
        {
            _presupuesto.Sustituir(reserva, 0m);
        }
    }

    private async Task<ResultadoPregunta> ResponderConTimeoutAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TimeSpan.FromSeconds(_limites.TimeoutLlmSegundos));
        try
        {
            return await _fase.ResponderAsync(sesion, limite.Token, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("El tiempo total de la petición se agotó.");
        }
    }

}
