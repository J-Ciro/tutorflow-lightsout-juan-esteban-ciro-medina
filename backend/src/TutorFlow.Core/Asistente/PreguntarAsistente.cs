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
    NoDisponible
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
    private readonly DependenciasDeAsistente _dependencias;
    private readonly LimitadorIdentidades? _limitadorIdentidades;
    private readonly LectorResumenPropio _lectorResumen;
    private readonly RelevanciaPrevia _relevancia;
    private readonly EmisorDeResultados _emisor;
    private readonly FaseDelModelo _fase;
    private readonly TimeProvider _tiempo;

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
        LimitadorIdentidades? limitadorIdentidades = null,
        TimeProvider? tiempo = null)
        : this(
            new DependenciasDeAsistente(apiAcademica, clienteLlm, baseConocimiento, auditor, politicaAcceso, verificadorSalida, limites, configuracion, presupuesto),
            limitadorIdentidades,
            tiempo ?? TimeProvider.System)
    {
    }

    private PreguntarAsistente(DependenciasDeAsistente dependencias, LimitadorIdentidades? limitadorIdentidades, TimeProvider tiempo)
    {
        _tiempo = tiempo;
        _dependencias = dependencias;
        _limitadorIdentidades = limitadorIdentidades;
        _lectorResumen = new LectorResumenPropio(dependencias.PoliticaAcceso);
        _relevancia = new RelevanciaPrevia(dependencias.PoliticaAcceso, dependencias.Configuracion.RelevanciaMinima);
        _emisor = new EmisorDeResultados(dependencias.Auditor, dependencias.PoliticaAcceso, dependencias.Configuracion);
        _fase = FaseDelModelo.Ensamblar(dependencias, _emisor);
    }

    public async Task<ResultadoPregunta> EjecutarAsync(
        PreguntaEntrada entrada,
        string ipOrigen,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var presupuestoApi = TimeSpan.FromSeconds(_dependencias.Limites.PresupuestoApiAcademicaSegundos);
        var api = new ApiAcademicaConPresupuesto(_dependencias.ApiAcademica, presupuestoApi, _tiempo);
        var sesion = new SesionPregunta(entrada, ipOrigen, correlationId, api, _tiempo);
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
        var directorio = await ConstructorDirectorio.ConstruirAsync(sesion.Entrada.Usuario, sesion.Api, _dependencias.Configuracion.CodigosAsignatura, cancellationToken);
        if (directorio is not null)
        {
            _dependencias.Auditor.RecordarPersonas(directorio);
            sesion.Directorio = ExcluirCodigosDePersonas(directorio);
        }

        var identidad = _dependencias.PoliticaAcceso.EvaluarIdentidad(sesion.Entrada.Usuario, sesion.Directorio);
        if (!identidad.Permitido)
        {
            return _emisor.Denegar(sesion, identidad.Motivo, 0);
        }

        AuditarIdentidades(sesion);
        return await ResponderSiEstaAutorizadoAsync(sesion, cancellationToken);
    }

    private async Task<ResultadoPregunta> ResponderSiEstaAutorizadoAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var decision = _dependencias.PoliticaAcceso.EvaluarPregunta(sesion.Entrada.Usuario, sesion.Entrada.Pregunta, sesion.Directorio);
        return decision.Permitido
            ? await ResponderSiEsRelevanteAsync(sesion, cancellationToken)
            : _emisor.Denegar(sesion, decision.Motivo, 0);
    }

    private void AuditarIdentidades(SesionPregunta sesion)
    {
        var identidades = _limitadorIdentidades?.Registrar(sesion.IpOrigen, sesion.Entrada.Usuario.Id);
        if (identidades is { SuperacionNueva: true })
        {
            _dependencias.Auditor.RegistrarMultiplesIdentidades(sesion.IpOrigen, identidades.Ids);
        }
    }

    private DirectorioAcademico ExcluirCodigosDePersonas(DirectorioAcademico directorio)
    {
        var catalogo = _dependencias.Configuracion.Catalogo;
        for (var indice = 0; indice < catalogo.Count; indice++)
        {
            if (CatalogoAsignaturas.EsIdDePersona(catalogo[indice].Codigo, directorio))
            {
                _dependencias.Configuracion.AdvertirUnaVez($"{Textos.PrefijoConfiguracionIgnorada}: Autorizacion:Asignaturas:{indice}:Codigo = \"{catalogo[indice].Codigo}\" (coincide con el ID de una persona del registro); se excluye del catálogo");
            }
        }

        return directorio with { CodigosAsignatura = [.. directorio.CodigosAsignatura.Where(codigo => !CatalogoAsignaturas.EsIdDePersona(codigo, directorio))] };
    }

    private async Task<ResultadoPregunta> ResponderSiEsRelevanteAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        var pregunta = sesion.Entrada.Pregunta;
        var contexto = new ContextoUsuario(sesion.Entrada.Usuario, sesion.Directorio);
        var recuperadas = _dependencias.BaseConocimiento.BuscarSecciones(ConsultaDeRecuperacion.DeTexto(pregunta), _dependencias.Limites.MaximoSeccionesRecuperadas);
        sesion.Secciones = SeccionDeDerogacion.Agregar(recuperadas, pregunta, _dependencias.BaseConocimiento);
        var activacion = ActivadorResumenPropio.Evaluar(contexto, pregunta, CatalogoAsignaturas.Vigente(_dependencias.Configuracion.Catalogo, sesion.Directorio));
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
        var reserva = _dependencias.Configuracion.ReservaPorPeticionUsd;
        if (!_dependencias.Presupuesto.TryReservar(reserva))
        {
            return _emisor.NoDisponible(sesion, ResultadoAuditoria.Error) with { SegundosParaReintentar = _dependencias.Presupuesto.SegundosHastaElReinicio() };
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
        _dependencias.Presupuesto.Sustituir(reserva, _dependencias.Configuracion.GastoDe(sesion.Uso.Valor, 1 + sesion.ReservasExtra));
        for (var extra = 0; extra < sesion.ReservasExtra; extra++)
        {
            _dependencias.Presupuesto.Sustituir(reserva, 0m);
        }
    }

    private async Task<ResultadoPregunta> ResponderConTimeoutAsync(SesionPregunta sesion, CancellationToken cancellationToken)
    {
        using var temporizador = new CancellationTokenSource(TimeSpan.FromSeconds(_dependencias.Limites.TimeoutLlmSegundos), _tiempo);
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, temporizador.Token);
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

internal sealed record DependenciasDeAsistente(
    IApiAcademica ApiAcademica,
    IClienteLlm ClienteLlm,
    IBaseConocimiento BaseConocimiento,
    AuditorPeticiones Auditor,
    PoliticaAcceso PoliticaAcceso,
    VerificadorSalida VerificadorSalida,
    LimitesAsistente Limites,
    ConfiguracionAsistente Configuracion,
    PresupuestoDiario Presupuesto);
