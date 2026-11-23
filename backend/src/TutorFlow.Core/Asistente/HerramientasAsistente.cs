using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public sealed record ContextoUsuario(Usuario Usuario, DirectorioAcademico Directorio);

public sealed record ResultadoHerramienta(string Contenido, bool IdAjeno)
{
    public static ResultadoHerramienta Texto(string contenido) => new(contenido, false);

    public static ResultadoHerramienta Denegado() => new(string.Empty, true);
}

public sealed class HerramientasAsistente
{
    public const string DatosBasicos = "consultar_datos_basicos";
    public const string Notas = "consultar_notas";
    public const string Cortes = "consultar_cortes";
    public const string Asistencia = "consultar_asistencia";
    public const string Programa = "consultar_programa";
    public const string Estudiante = "consultar_estudiante";
    public const string ParametroEstudianteId = "estudianteId";
    public const string ParametroAsignatura = "asignatura";

    private static readonly IReadOnlyList<HerramientaLlm> HerramientasDeEstudiante =
    [
        new(Notas, "Consulta las definitivas de cada asignatura, el promedio ponderado del periodo, la prueba académica, la habilitación y la elegibilidad de beca de la persona que pregunta, ya calculados. Úsala para preguntas sobre notas, promedio, asignaturas perdidas, habilitación o beca.", []),
        new(Asistencia, "Consulta las inasistencias, las faltas permitidas y las faltas restantes de cada asignatura de la persona que pregunta. Úsala para preguntas sobre asistencia o faltas.", []),
        new(DatosBasicos, "Consulta el nombre, el programa y el coordinador del programa de la persona que pregunta. Úsala para preguntas sobre quién es su coordinador o a qué programa pertenece.", []),
        new(Cortes, "Consulta las notas de los tres cortes de una asignatura de la persona que pregunta. Úsala solo cuando pregunten por la nota de un corte concreto.", [new ParametroHerramienta(ParametroAsignatura, "Nombre o código de la asignatura.", true)])
    ];

    private static readonly IReadOnlyList<HerramientaLlm> HerramientasDeCoordinador =
    [
        new(Programa, "Consulta, para cada estudiante de los programas a cargo del coordinador, el promedio, la prueba académica, las asignaturas perdidas, las asignaturas sin definitiva y la asistencia sin registro. Úsala para listados y conteos del programa.", []),
        new(Estudiante, "Consulta el detalle de notas y asistencia de un estudiante de los programas del coordinador. Úsala cuando pregunten por un estudiante concreto.", [new ParametroHerramienta(ParametroEstudianteId, "Identificador del estudiante: las letras EST, un guion y tres dígitos.", true)])
    ];

    private readonly IApiAcademica _apiAcademica;
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly Dictionary<string, Func<SolicitudHerramienta, Task<ResultadoHerramienta>>> _manejadores;

    public HerramientasAsistente(IApiAcademica apiAcademica, PoliticaAcceso politicaAcceso)
    {
        _apiAcademica = apiAcademica;
        _politicaAcceso = politicaAcceso;
        _manejadores = new Dictionary<string, Func<SolicitudHerramienta, Task<ResultadoHerramienta>>>
        {
            [DatosBasicos] = ConsultarDatosBasicos,
            [Notas] = ConsultarNotas,
            [Cortes] = ConsultarCortes,
            [Asistencia] = ConsultarAsistencia,
            [Programa] = ConsultarPrograma,
            [Estudiante] = ConsultarEstudiante
        };
    }

    public static IReadOnlyList<HerramientaLlm> Definir(Rol rol) =>
        rol == Rol.Coordinador ? HerramientasDeCoordinador : HerramientasDeEstudiante;

    public async Task<ResultadoHerramienta> EjecutarAsync(
        LlamadaHerramienta llamada,
        ContextoUsuario contexto,
        RegistroConsultas registro,
        CancellationToken cancellationToken)
    {
        if (!EstaDisponible(llamada.Nombre, contexto.Usuario.Rol) || !_manejadores.TryGetValue(llamada.Nombre, out var manejador))
        {
            return ResultadoHerramienta.Texto("Herramienta no disponible.");
        }

        var estudianteId = ArgumentosHerramienta.LeerTexto(llamada.ArgumentosJson, ParametroEstudianteId);
        if (EsLecturaAjena(contexto, estudianteId))
        {
            return ResultadoHerramienta.Denegado();
        }

        var asignatura = ArgumentosHerramienta.LeerTexto(llamada.ArgumentosJson, ParametroAsignatura);
        return await manejador(new SolicitudHerramienta(contexto, registro, estudianteId, asignatura, cancellationToken));
    }

    private static bool EstaDisponible(string nombre, Rol rol) =>
        Definir(rol).Any(herramienta => herramienta.Nombre == nombre);

    private bool EsLecturaAjena(ContextoUsuario contexto, string? estudianteId) =>
        estudianteId is not null
        && !_politicaAcceso.EvaluarLecturaEstudiante(contexto.Usuario, estudianteId, contexto.Directorio).Permitido;

    private Task<ResultadoHerramienta> ConsultarDatosBasicos(SolicitudHerramienta solicitud)
    {
        var estudiante = solicitud.Contexto.Directorio.Estudiantes.First(e => e.Id == solicitud.Contexto.Usuario.Id);
        var contenido = FormateadorDatos.DatosBasicos(estudiante, solicitud.Contexto.Directorio);
        solicitud.Registro.RegistrarDatos([DatosLeidos.SinAsignatura(estudiante.Id, estudiante.ProgramaId, RecursoConsultado.DatosBasicos)]);
        return Task.FromResult(Responder(solicitud, contenido, estudiante.Id, [RecursoConsultado.DatosBasicos], Textos.SeccionDatosBasicos));
    }

    private async Task<ResultadoHerramienta> ConsultarNotas(SolicitudHerramienta solicitud)
    {
        var id = solicitud.Contexto.Usuario.Id;
        var resumen = await ResumirAsync(id, solicitud.CancellationToken);
        solicitud.Registro.RegistrarReglas(ReglasAplicadas.DeNotas([resumen]));
        solicitud.Registro.RegistrarDatos(DatosLeidos.PorAsignatura(resumen, ProgramaDe(solicitud, id), RecursoConsultado.Notas, RecursoConsultado.Asistencia));
        return Responder(solicitud, FormateadorDatos.Notas(resumen), id, [RecursoConsultado.Notas, RecursoConsultado.Asistencia], Textos.SeccionNotas);
    }

    private async Task<ResultadoHerramienta> ConsultarCortes(SolicitudHerramienta solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Asignatura))
        {
            return ResultadoHerramienta.Texto("Indica el nombre o el código de la asignatura.");
        }

        var id = solicitud.Contexto.Usuario.Id;
        var notas = await _apiAcademica.ObtenerNotasAsync(id, solicitud.CancellationToken);
        var contenido = FormateadorDatos.Cortes(notas, solicitud.Asignatura);
        var codigos = FormateadorDatos.BuscarAsignaturas(notas, solicitud.Asignatura).Select(nota => nota.Codigo);
        solicitud.Registro.RegistrarDatos(DatosLeidos.PorCodigos(id, ProgramaDe(solicitud, id), codigos, RecursoConsultado.Notas));
        return Responder(solicitud, contenido, id, [RecursoConsultado.Notas], Textos.SeccionNotas);
    }

    private async Task<ResultadoHerramienta> ConsultarAsistencia(SolicitudHerramienta solicitud)
    {
        var id = solicitud.Contexto.Usuario.Id;
        var resumen = await ResumirAsync(id, solicitud.CancellationToken);
        solicitud.Registro.RegistrarReglas(ReglasAplicadas.DeAsistencia([resumen]));
        solicitud.Registro.RegistrarDatos(DatosLeidos.PorAsignatura(resumen, ProgramaDe(solicitud, id), RecursoConsultado.Notas, RecursoConsultado.Asistencia));
        return Responder(solicitud, FormateadorDatos.Asistencia(resumen), id, [RecursoConsultado.Notas, RecursoConsultado.Asistencia], Textos.SeccionAsistencia);
    }

    private async Task<ResultadoHerramienta> ConsultarEstudiante(SolicitudHerramienta solicitud)
    {
        if (string.IsNullOrEmpty(solicitud.EstudianteId))
        {
            return ResultadoHerramienta.Texto("Falta el identificador del estudiante.");
        }

        var directorio = solicitud.Contexto.Directorio;
        var estudiante = directorio.Estudiantes.First(e => e.Id == solicitud.EstudianteId);
        var resumen = await ResumirAsync(estudiante.Id, solicitud.CancellationToken);
        var contenido = string.Join("\n", FormateadorDatos.DatosBasicos(estudiante, directorio), FormateadorDatos.Notas(resumen), FormateadorDatos.Asistencia(resumen));
        var recursos = new[] { RecursoConsultado.DatosBasicos, RecursoConsultado.Notas, RecursoConsultado.Asistencia };
        solicitud.Registro.RegistrarReglas([.. ReglasAplicadas.DeNotas([resumen]), .. ReglasAplicadas.DeAsistencia([resumen])]);
        solicitud.Registro.Registrar(estudiante.Id, recursos, new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionAsistencia));
        solicitud.Registro.RegistrarDatos(DatosDeEstudiante(estudiante, resumen, RecursoConsultado.DatosBasicos));
        return Responder(solicitud, contenido, estudiante.Id, recursos, Textos.SeccionNotas);
    }

    private async Task<ResultadoHerramienta> ConsultarPrograma(SolicitudHerramienta solicitud)
    {
        var programas = _politicaAcceso.ProgramasAutorizados(solicitud.Contexto.Usuario, solicitud.Contexto.Directorio);
        if (programas.Count == 0)
        {
            return ResultadoHerramienta.Texto("No tienes programas asignados, por lo que no hay datos de estudiantes para consultar.");
        }

        var bloques = new List<string>();
        var resumenes = new List<ResumenEstudiante>();
        foreach (var programa in solicitud.Contexto.Directorio.Programas.Where(p => programas.Contains(p.Id)))
        {
            bloques.Add(await DescribirProgramaAsync(solicitud, programa, resumenes));
        }

        solicitud.Registro.RegistrarReglas(ReglasAplicadas.DePrograma(resumenes));
        return ResultadoHerramienta.Texto(string.Join("\n\n", bloques));
    }

    private async Task<string> DescribirProgramaAsync(SolicitudHerramienta solicitud, Programa programa, List<ResumenEstudiante> resumenes)
    {
        var estudiantes = solicitud.Contexto.Directorio.Estudiantes.Where(e => e.ProgramaId == programa.Id).ToList();
        var lineas = new List<string> { FormateadorDatos.EncabezadoPrograma(programa, estudiantes.Count) };
        foreach (var estudiante in estudiantes)
        {
            var resumen = await ResumirAsync(estudiante.Id, solicitud.CancellationToken);
            resumenes.Add(resumen);
            lineas.Add(FormateadorDatos.LineaDePrograma(estudiante, resumen));
            solicitud.Registro.Registrar(estudiante.Id, [RecursoConsultado.Programa, RecursoConsultado.Notas, RecursoConsultado.Asistencia], null);
            solicitud.Registro.RegistrarDatos(DatosDeEstudiante(estudiante, resumen, RecursoConsultado.Programa));
        }

        solicitud.Registro.RegistrarCita(new Cita(Textos.DocumentoDatosPrograma, programa.Id));
        return FormateadorDatos.Delimitar($"{Textos.DocumentoDatosPrograma} · {programa.Id}", string.Join("\n", lineas));
    }

    private static string ProgramaDe(SolicitudHerramienta solicitud, string estudianteId) =>
        solicitud.Contexto.Directorio.Estudiantes.First(e => e.Id == estudianteId).ProgramaId;

    private static IEnumerable<DatoConsultado> DatosDeEstudiante(Estudiante estudiante, ResumenEstudiante resumen, RecursoConsultado recursoSinAsignatura) =>
        DatosLeidos.PorAsignatura(resumen, estudiante.ProgramaId, RecursoConsultado.Notas, RecursoConsultado.Asistencia)
            .Prepend(DatosLeidos.SinAsignatura(estudiante.Id, estudiante.ProgramaId, recursoSinAsignatura));

    private static ResultadoHerramienta Responder(
        SolicitudHerramienta solicitud,
        string contenido,
        string estudianteId,
        IEnumerable<RecursoConsultado> recursos,
        string seccion)
    {
        var cita = new Cita(Textos.DocumentoDatosEstudiante, seccion);
        solicitud.Registro.Registrar(estudianteId, recursos, cita);
        return ResultadoHerramienta.Texto(FormateadorDatos.Delimitar($"{cita.Documento} · {cita.Seccion}", contenido));
    }

    private async Task<ResumenEstudiante> ResumirAsync(string estudianteId, CancellationToken cancellationToken)
    {
        var notas = await _apiAcademica.ObtenerNotasAsync(estudianteId, cancellationToken);
        var asistencia = await _apiAcademica.ObtenerAsistenciaAsync(estudianteId, cancellationToken);
        var asignaturas = notas.Select(nota => new Asignatura(
            nota.Codigo,
            nota.Nombre,
            nota.Creditos,
            nota.Cortes,
            asistencia.FirstOrDefault(a => a.Codigo == nota.Codigo)?.Asistencia ?? new Asistencia(0, null))).ToList();
        return CalculosAcademicos.ResumirEstudiante(estudianteId, asignaturas);
    }

    private sealed record SolicitudHerramienta(
        ContextoUsuario Contexto,
        RegistroConsultas Registro,
        string? EstudianteId,
        string? Asignatura,
        CancellationToken CancellationToken);
}
