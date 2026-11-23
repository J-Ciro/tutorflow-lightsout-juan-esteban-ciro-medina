using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public static class ConstructorDirectorio
{
    public static DirectorioAcademico Vacio { get; } = new([], [], [], []);

    public static async Task<DirectorioAcademico?> ConstruirAsync(
        Usuario usuario,
        IApiAcademica api,
        IReadOnlyCollection<string> codigosAsignatura,
        CancellationToken cancellationToken)
    {
        var estudiante = usuario.Rol == Rol.Estudiante ? await api.ObtenerEstudianteAsync(usuario.Id, cancellationToken) : null;
        var coordinador = usuario.Rol == Rol.Coordinador ? await api.ObtenerCoordinadorAsync(usuario.Id, cancellationToken) : null;
        if (estudiante is null && coordinador is null)
        {
            return null;
        }

        var programas = (await api.ObtenerProgramasAsync(cancellationToken)).Where(p => Identificadores.EsIdPrograma(p.Id)).ToList();
        var estudiantes = await LeerEstudiantesAsync(programas, estudiante, api, cancellationToken);
        var coordinadores = await LeerCoordinadoresAsync(programas, coordinador, api, cancellationToken);
        return new DirectorioAcademico(estudiantes, coordinadores, programas, codigosAsignatura);
    }

    private static async Task<List<Estudiante>> LeerEstudiantesAsync(
        List<Programa> programas,
        Estudiante? propio,
        IApiAcademica api,
        CancellationToken cancellationToken)
    {
        var estudiantes = new List<Estudiante>();
        foreach (var programa in programas)
        {
            estudiantes.AddRange(await api.ObtenerEstudiantesDeProgramaAsync(programa.Id, cancellationToken));
        }

        if (propio is not null)
        {
            estudiantes.Add(propio);
        }

        return [.. estudiantes.DistinctBy(e => e.Id)];
    }

    private static async Task<List<Coordinador>> LeerCoordinadoresAsync(
        List<Programa> programas,
        Coordinador? propio,
        IApiAcademica api,
        CancellationToken cancellationToken)
    {
        var coordinadores = new List<Coordinador>();
        var ids = programas.Select(p => p.CoordinadorId).Where(Identificadores.EsIdCoordinador).Distinct();
        foreach (var id in ids.Where(id => id != propio?.Id))
        {
            var coordinador = await api.ObtenerCoordinadorAsync(id, cancellationToken);
            if (coordinador is not null)
            {
                coordinadores.Add(coordinador);
            }
        }

        if (propio is not null)
        {
            coordinadores.Add(propio);
        }

        return coordinadores;
    }
}
