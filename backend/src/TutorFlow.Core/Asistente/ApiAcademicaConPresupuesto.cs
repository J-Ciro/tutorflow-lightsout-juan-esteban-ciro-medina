using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

public sealed class ApiAcademicaConPresupuesto : IApiAcademica
{
    private readonly IApiAcademica _interna;
    private readonly TimeProvider _tiempo;
    private TimeSpan _restante;

    public ApiAcademicaConPresupuesto(IApiAcademica interna, TimeSpan presupuesto, TimeProvider? tiempo = null)
    {
        _tiempo = tiempo ?? TimeProvider.System;
        _interna = interna;
        _restante = presupuesto;
    }

    public Task<Estudiante?> ObtenerEstudianteAsync(string id, CancellationToken cancellationToken) =>
        Medir(token => _interna.ObtenerEstudianteAsync(id, token), cancellationToken);

    public Task<Coordinador?> ObtenerCoordinadorAsync(string id, CancellationToken cancellationToken) =>
        Medir(token => _interna.ObtenerCoordinadorAsync(id, token), cancellationToken);

    public Task<IReadOnlyList<NotasAsignatura>> ObtenerNotasAsync(string estudianteId, CancellationToken cancellationToken) =>
        Medir(token => _interna.ObtenerNotasAsync(estudianteId, token), cancellationToken);

    public Task<IReadOnlyList<AsistenciaAsignatura>> ObtenerAsistenciaAsync(string estudianteId, CancellationToken cancellationToken) =>
        Medir(token => _interna.ObtenerAsistenciaAsync(estudianteId, token), cancellationToken);

    public Task<IReadOnlyList<Estudiante>> ObtenerEstudiantesDeProgramaAsync(string programaId, CancellationToken cancellationToken) =>
        Medir(token => _interna.ObtenerEstudiantesDeProgramaAsync(programaId, token), cancellationToken);

    public Task<IReadOnlyList<Programa>> ObtenerProgramasAsync(CancellationToken cancellationToken) =>
        Medir(token => _interna.ObtenerProgramasAsync(token), cancellationToken);

    private async Task<T> Medir<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken)
    {
        using var temporizador = new CancellationTokenSource(_restante, _tiempo);
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, temporizador.Token);
        var inicio = _tiempo.GetTimestamp();
        try
        {
            return await operacion(limite.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependenciaNoDisponibleException("Se agotó el presupuesto de tiempo de la API académica.");
        }
        finally
        {
            var restante = _restante - _tiempo.GetElapsedTime(inicio);
            _restante = restante > TimeSpan.Zero ? restante : TimeSpan.Zero;
        }
    }
}
