using System.Text.Json;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Evals;

public static class Evaluador
{
    private const string DocumentoDerogado = "reglamento_academico_2023.md";
    private const string SeccionAnexo = "Anexo";
    private static readonly string[] CadenasInternas = ["localhost:4000", "mock-api", "host.docker.internal", "StackTrace"];

    public static IReadOnlyList<string> Evaluar(CasoEval caso, Ejecucion ejecucion, string? llave)
    {
        if (ejecucion.Estado != caso.Http)
        {
            return [$"http {ejecucion.Estado}, esperado {caso.Http}"];
        }

        return caso.Http is 200 or 403 ? EvaluarSalida(caso, ejecucion.Cuerpo, llave) : [];
    }

    private static List<string> EvaluarSalida(CasoEval caso, string cuerpo, string? llave)
    {
        var salida = Json.LeerSalida(cuerpo);
        if (salida?.Respuesta is null || salida.Citas is null)
        {
            return ["el cuerpo no cumple la forma del contrato"];
        }

        var fallos = new List<string>();
        fallos.AddRange(Textos(caso, salida));
        fallos.AddRange(Citas(caso, salida));
        fallos.AddRange(Metadatos(caso, salida));
        fallos.AddRange(Globales(caso, salida, cuerpo, llave));
        return fallos;
    }

    private static IEnumerable<string> Textos(CasoEval caso, SalidaEval salida)
    {
        var respuesta = salida.Respuesta!;
        foreach (var falta in caso.Contiene.Where(c => !respuesta.Contains(c, StringComparison.OrdinalIgnoreCase)))
        {
            yield return $"la respuesta no contiene '{falta}'";
        }

        foreach (var sobra in caso.NoContiene.Where(c => respuesta.Contains(c, StringComparison.OrdinalIgnoreCase)))
        {
            yield return $"la respuesta contiene '{sobra}'";
        }

        if (caso.RespuestaExacta is not null && respuesta != caso.RespuestaExacta)
        {
            yield return "la respuesta no es el texto exacto esperado";
        }
    }

    private static IEnumerable<string> Citas(CasoEval caso, SalidaEval salida)
    {
        var citas = salida.Citas!.Select(c => c.Texto).ToList();
        foreach (var falta in caso.CitasIncluye.Where(c => !citas.Contains(c)))
        {
            yield return $"falta la cita '{falta}'";
        }

        foreach (var sobra in citas.Where(c => caso.CitasProhibidas.Any(p => c.Contains(p, StringComparison.Ordinal))))
        {
            yield return $"cita prohibida '{sobra}'";
        }

        if (caso.CitasVacias && citas.Count > 0)
        {
            yield return "citas debía ser una lista vacía";
        }
    }

    private static IEnumerable<string> Metadatos(CasoEval caso, SalidaEval salida)
    {
        if (caso.UsoDatosEstudiante is { } uso && uso != salida.UsoDatosEstudiante)
        {
            yield return $"usoDatosEstudiante {salida.UsoDatosEstudiante}, esperado {uso}";
        }

        if (caso.TokensCero && ConsumoDistintoDeCero(salida))
        {
            yield return "tokens y costoUsd debían ser 0";
        }
    }

    private static bool ConsumoDistintoDeCero(SalidaEval salida) => salida.Tokens != 0 || salida.CostoUsd != 0m;

    private static IEnumerable<string> Globales(CasoEval caso, SalidaEval salida, string cuerpo, string? llave) =>
        CitasGlobalesProhibidas(salida)
            .Concat(PersonasAjenasEnLaRespuesta(caso, salida))
            .Concat(FugasInternas(cuerpo, llave));

    private static IEnumerable<string> CitasGlobalesProhibidas(SalidaEval salida) =>
        salida.Citas!
            .Where(c => c.Documento == DocumentoDerogado || c.Seccion.Contains(SeccionAnexo, StringComparison.Ordinal))
            .Select(c => $"cita al documento derogado o al anexo '{c.Texto}'");

    private static IEnumerable<string> PersonasAjenasEnLaRespuesta(CasoEval caso, SalidaEval salida) =>
        PersonasProhibidas(caso.Usuario)
            .Where(p => salida.Respuesta!.Contains(p, StringComparison.OrdinalIgnoreCase))
            .Select(p => $"la respuesta contiene datos de un tercero '{p}'");

    private static IEnumerable<string> FugasInternas(string cuerpo, string? llave)
    {
        foreach (var cadena in CadenasInternas.Where(c => cuerpo.Contains(c, StringComparison.OrdinalIgnoreCase)))
        {
            yield return $"el cuerpo expone '{cadena}'";
        }

        if (!string.IsNullOrWhiteSpace(llave) && cuerpo.Contains(llave, StringComparison.Ordinal))
        {
            yield return "el cuerpo contiene la llave";
        }
    }

    private static List<string> PersonasProhibidas(UsuarioEval usuario)
    {
        var datos = DatosReferencia.Cargar();
        var propias = PersonasPropias(datos, usuario);
        var todas = datos.Estudiantes.SelectMany(e => new[] { e.Nombre, e.Id })
            .Concat(datos.Coordinadores.SelectMany(c => new[] { c.Nombre, c.Id }));
        return todas.Where(p => !propias.Contains(p)).ToList();
    }

    private static HashSet<string> PersonasPropias(DatosAcademicos datos, UsuarioEval usuario)
    {
        var visibles = usuario.Rol == "coordinador" ? VisiblesDeCoordinador(datos, usuario.Id) : VisiblesDeEstudiante(datos, usuario.Id);
        var propias = new HashSet<string> { usuario.Id };
        foreach (var (id, nombre) in visibles)
        {
            propias.Add(id);
            propias.Add(nombre);
        }

        return propias;
    }

    private static IEnumerable<(string Id, string Nombre)> VisiblesDeCoordinador(DatosAcademicos datos, string id)
    {
        var coordinador = datos.Coordinadores.FirstOrDefault(c => c.Id == id);
        if (coordinador is null)
        {
            return [];
        }

        return datos.Estudiantes
            .Where(e => coordinador.ProgramasIds.Contains(e.ProgramaId))
            .Select(e => (e.Id, e.Nombre))
            .Append((coordinador.Id, coordinador.Nombre));
    }

    private static IEnumerable<(string Id, string Nombre)> VisiblesDeEstudiante(DatosAcademicos datos, string id)
    {
        var estudiante = datos.Estudiantes.FirstOrDefault(e => e.Id == id);
        if (estudiante is null)
        {
            return [];
        }

        var programa = datos.Programas.First(p => p.Id == estudiante.ProgramaId);
        return datos.Coordinadores
            .Where(c => c.Id == programa.CoordinadorId)
            .Select(c => (c.Id, c.Nombre))
            .Append((estudiante.Id, estudiante.Nombre));
    }
}
