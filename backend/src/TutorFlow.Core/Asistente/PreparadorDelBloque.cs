using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class PreparadorDelBloque
{
    private readonly PoliticaAcceso _politicaAcceso;
    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public PreparadorDelBloque(PoliticaAcceso politicaAcceso, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _politicaAcceso = politicaAcceso;
        _catalogo = catalogo;
    }

    public async Task<string?> DePersonasAsync(SesionPregunta sesion, ContextoUsuario contexto, CancellationToken cancellationToken)
    {
        var palabras = TokenizadorTexto.Valores(sesion.Entrada.Pregunta);
        var seleccion = Seleccion(sesion, contexto, palabras);
        var bloques = new List<string>();
        foreach (var personaId in PersonasDelBloque(sesion, contexto, palabras))
        {
            var resumen = await ResumenDeAsync(sesion, contexto, personaId, cancellationToken);
            if (resumen is not null)
            {
                sesion.PersonasDelBloque.Add(personaId);
                bloques.Add(FormateadorDePersona.Bloque(contexto.Directorio.Estudiantes.First(e => e.Id == personaId), resumen, seleccion));
            }
        }

        return bloques.Count == 0 ? null : string.Join("\n\n", bloques);
    }

    public string? DelPrograma(SesionPregunta sesion, ContextoUsuario contexto)
    {
        var programas = _politicaAcceso.ProgramasAutorizados(contexto.Usuario, contexto.Directorio);
        var porPrograma = contexto.Directorio.Programas
            .Where(programa => programas.Contains(programa.Id))
            .Select(programa => (programa, EstudiantesDe(sesion, contexto, programa)))
            .ToList();
        var pedidos = ListadosDePrograma.Pedidos(TokenizadorTexto.Valores(sesion.Entrada.Pregunta));
        return porPrograma.Count == 0 ? null : ListadosDePrograma.Bloque(porPrograma, pedidos);
    }

    private static IReadOnlyList<EstudianteDePrograma> EstudiantesDe(SesionPregunta sesion, ContextoUsuario contexto, Programa programa) =>
        [.. contexto.Directorio.Estudiantes
            .Where(estudiante => estudiante.ProgramaId == programa.Id && sesion.Registro.Resumenes.ContainsKey(estudiante.Id))
            .Select(estudiante => new EstudianteDePrograma(estudiante, sesion.Registro.Resumenes[estudiante.Id]))];

    private SeleccionDelBloque Seleccion(SesionPregunta sesion, ContextoUsuario contexto, IReadOnlyList<string> palabras)
    {
        var vigente = CatalogoAsignaturas.Vigente(_catalogo, sesion.Directorio);
        var claves = CatalogoAsignaturas.CodigosNombrados(sesion.Entrada.Pregunta, vigente, sesion.Directorio.CodigosAsignatura);
        var nombres = vigente.Where(asignatura => claves.Contains(AmbitoPersonal.Clave(asignatura.Codigo))).Select(asignatura => asignatura.Nombre).ToList();
        return new SeleccionDelBloque(CamposDePersona.De(palabras), claves, nombres, contexto.Usuario.Rol == Rol.Coordinador);
    }

    private IReadOnlyList<string> PersonasDelBloque(SesionPregunta sesion, ContextoUsuario contexto, IReadOnlyList<string> palabras)
    {
        if (contexto.Usuario.Rol == Rol.Estudiante)
        {
            return sesion.Activacion.Entrega ? [contexto.Usuario.Id] : [];
        }

        var programas = _politicaAcceso.ProgramasAutorizados(contexto.Usuario, contexto.Directorio);
        return ActivadorPrelectura.EstudiantesMencionadosDeSusProgramas(contexto, palabras, programas);
    }

    private async Task<ResumenEstudiante?> ResumenDeAsync(SesionPregunta sesion, ContextoUsuario contexto, string personaId, CancellationToken cancellationToken)
    {
        if (sesion.Registro.Resumenes.TryGetValue(personaId, out var leido))
        {
            return leido;
        }

        if (!_politicaAcceso.EvaluarLecturaEstudiante(contexto.Usuario, personaId, contexto.Directorio).Permitido)
        {
            return null;
        }

        var resumen = await ResumenesAcademicos.ResumirAsync(sesion.Api, personaId, cancellationToken);
        Registrar(sesion.Registro, contexto.Directorio, resumen);
        return resumen;
    }

    private static void Registrar(RegistroConsultas registro, DirectorioAcademico directorio, ResumenEstudiante resumen)
    {
        var programaId = directorio.Estudiantes.First(estudiante => estudiante.Id == resumen.EstudianteId).ProgramaId;
        registro.Registrar(resumen.EstudianteId, [RecursoConsultado.Notas, RecursoConsultado.Asistencia], new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionNotas));
        registro.RegistrarCita(new Cita(Textos.DocumentoDatosEstudiante, Textos.SeccionAsistencia));
        registro.RegistrarDatos(DatosLeidos.PorAsignatura(resumen, programaId, RecursoConsultado.Notas, RecursoConsultado.Asistencia));
        registro.RegistrarResumen(resumen);
        registro.RegistrarReglas(ReglasAplicadas.DeNotasYAsistencia([resumen]));
    }
}
