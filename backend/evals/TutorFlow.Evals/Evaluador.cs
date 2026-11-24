using System.Text.Json;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Evals;

public static class Evaluador
{
    private const string DocumentoDerogado = "reglamento_academico_2023.md";
    private const string SeccionAnexo = "Anexo";
    private const string EscenarioDerogado = "ESC-015";
    private const string EscenarioFugaHerramienta = "ESC-146";
    private const string PrefijoHerramientas = "consultar_";
    private const string DocumentoDeEjemplo = "documento_ejemplo.md";
    private const string EscenarioDocumentoDeEjemplo = "ESC-197 e";
    private const string DocumentoDeDatosDelEstudiante = "Datos del estudiante";
    private const string DocumentoDeDatosDelPrograma = "Datos del programa";
    private static readonly string[] TextosFijos = [Core.Asistente.Textos.NoVerificable, Core.Asistente.Textos.SinInformacion, Core.Asistente.Textos.NegativaEstudiante, Core.Asistente.Textos.NegativaCoordinador];
    private static readonly string[] CadenasInternas = ["localhost:4000", "mock-api", "host.docker.internal", "StackTrace"];

    public static IReadOnlyList<string> Evaluar(CasoEval caso, Ejecucion ejecucion, string? llave)
    {
        if (ejecucion.Estado != caso.Http)
        {
            return [Criterios.Etiquetar(Criterios.Codigo, $"http {ejecucion.Estado}, esperado {caso.Http}")];
        }

        return caso.Http is 200 or 403 ? EvaluarSalida(caso, ejecucion.Cuerpo, llave) : [];
    }

    private static List<string> EvaluarSalida(CasoEval caso, string cuerpo, string? llave)
    {
        var salida = Json.LeerSalida(cuerpo);
        if (salida?.Respuesta is null || salida.Citas is null)
        {
            return [Criterios.Etiquetar(Criterios.Codigo, "el cuerpo no cumple la forma del contrato")];
        }

        return [.. CriteriosDelCaso(caso, salida, cuerpo, llave).Concat(ReglasDeRedaccion(salida))];
    }

    private static IEnumerable<string> CriteriosDelCaso(CasoEval caso, SalidaEval salida, string cuerpo, string? llave) =>
        Textos(caso, salida)
            .Concat(Parejas(caso, salida))
            .Concat(ContieneAlguna(caso, salida))
            .Concat(Respondida(caso, salida))
            .Concat(Citas(caso, salida))
            .Concat(CitasExactas(caso, salida))
            .Concat(CitasDeDocumentoExactas(caso, salida))
            .Concat(CitasTotal(caso, salida))
            .Concat(Metadatos(caso, salida))
            .Concat(Globales(caso, salida, cuerpo, llave));

    private static IEnumerable<string> ReglasDeRedaccion(SalidaEval salida) =>
        ReglasDeTexto.OracionesTruncadas(salida.Respuesta!)
            .Concat(ReglasDeTexto.ReferenciasSinCita(salida.Respuesta!, salida.Citas!))
            .Concat(ReglasDeTexto.Latex(salida.Respuesta!))
            .Concat(ReglasDeTexto.HuecosTrasUbicacion(salida.Respuesta!))
            .Select(f => Criterios.Etiquetar(Criterios.Texto, f));

    private static IEnumerable<string> Textos(CasoEval caso, SalidaEval salida)
    {
        var respuesta = salida.Respuesta!;
        foreach (var falta in caso.Contiene.Where(c => !respuesta.Contains(c, StringComparison.OrdinalIgnoreCase)))
        {
            yield return Criterios.Etiquetar(Criterios.DeLaCifraOElDato(caso, falta), $"la respuesta no contiene '{falta}'");
        }

        foreach (var sobra in caso.NoContiene.Where(c => respuesta.Contains(c, StringComparison.OrdinalIgnoreCase)))
        {
            yield return Criterios.Etiquetar(Criterios.Dato, $"la respuesta contiene '{sobra}'");
        }

        if (caso.RespuestaExacta is not null && respuesta != caso.RespuestaExacta)
        {
            yield return Criterios.Etiquetar(Criterios.Codigo, "la respuesta no es el texto exacto esperado");
        }
    }

    private static IEnumerable<string> Parejas(CasoEval caso, SalidaEval salida)
    {
        var lineas = salida.Respuesta!.Split('\n');
        foreach (var pareja in caso.Parejas ?? [])
        {
            if (!lineas.Any(linea => ValorTrasElNombre(linea, pareja)))
            {
                yield return Criterios.Etiquetar(Criterios.Dato, $"falta '{pareja.Valor}' en la línea de '{pareja.Nombre}'");
            }
        }
    }

    private static bool ValorTrasElNombre(string linea, ParejaEval pareja)
    {
        var posicion = linea.IndexOf(pareja.Nombre, StringComparison.OrdinalIgnoreCase);
        return posicion >= 0 && linea.IndexOf(pareja.Valor, posicion + pareja.Nombre.Length, StringComparison.Ordinal) >= 0;
    }

    private static IEnumerable<string> ContieneAlguna(CasoEval caso, SalidaEval salida) =>
        (caso.ContieneAlguna ?? [])
            .Where(opciones => !opciones.Any(o => salida.Respuesta!.Contains(o, StringComparison.OrdinalIgnoreCase)))
            .Select(opciones => Criterios.Etiquetar(Criterios.Dato, $"la respuesta no contiene ninguna de [{string.Join(", ", opciones)}]"));

    private static IEnumerable<string> Respondida(CasoEval caso, SalidaEval salida) =>
        caso.Respondida && TextosFijos.Contains(salida.Respuesta!)
            ? [Criterios.Etiquetar(Criterios.Codigo, "la respuesta es un texto fijo y no una respuesta verificada")]
            : [];

    private static IEnumerable<string> Citas(CasoEval caso, SalidaEval salida)
    {
        var citas = salida.Citas!.Select(c => c.Texto).ToList();
        foreach (var falta in caso.CitasIncluye.Where(c => !citas.Contains(c)))
        {
            yield return Criterios.Etiquetar(Criterios.Citas, $"falta la cita '{falta}'");
        }

        foreach (var sobra in citas.Where(c => caso.CitasProhibidas.Any(p => c.Contains(p, StringComparison.Ordinal))))
        {
            yield return Criterios.Etiquetar(Criterios.Citas, $"cita prohibida '{sobra}'");
        }

        if (caso.CitasVacias && citas.Count > 0)
        {
            yield return Criterios.Etiquetar(Criterios.Citas, "citas debía ser una lista vacía");
        }
    }

    private static IEnumerable<string> CitasExactas(CasoEval caso, SalidaEval salida)
    {
        var citas = salida.Citas!.Select(c => c.Texto).ToList();
        if (caso.CitasExactas is { } exactas && !exactas.SequenceEqual(citas))
        {
            yield return Criterios.Etiquetar(Criterios.Citas, $"citas debía ser exactamente [{string.Join(", ", exactas)}] y fue [{string.Join(", ", citas)}]");
        }
    }

    private static IEnumerable<string> CitasDeDocumentoExactas(CasoEval caso, SalidaEval salida)
    {
        var documentos = salida.Citas!
            .Where(c => c.Documento is not (DocumentoDeDatosDelEstudiante or DocumentoDeDatosDelPrograma))
            .Select(c => c.Texto)
            .ToList();
        if (caso.CitasDocumentoExactas is { } esperadas && !esperadas.Order(StringComparer.Ordinal).SequenceEqual(documentos.Order(StringComparer.Ordinal)))
        {
            yield return Criterios.Etiquetar(Criterios.Citas, $"las citas de documento debían ser exactamente {{{string.Join(", ", esperadas)}}} y fueron {{{string.Join(", ", documentos)}}}");
        }
    }

    private static IEnumerable<string> CitasTotal(CasoEval caso, SalidaEval salida)
    {
        if (caso.CitasTotal is { } total && salida.Citas!.Count != total)
        {
            yield return Criterios.Etiquetar(Criterios.Citas, $"citas debía tener {total} entradas y tuvo {salida.Citas.Count}");
        }
    }

    private static IEnumerable<string> Metadatos(CasoEval caso, SalidaEval salida)
    {
        if (caso.UsoDatosEstudiante is { } uso && uso != salida.UsoDatosEstudiante)
        {
            yield return Criterios.Etiquetar(Criterios.Codigo, $"usoDatosEstudiante {salida.UsoDatosEstudiante}, esperado {uso}");
        }

        if (caso.TokensCero && ConsumoDistintoDeCero(salida))
        {
            yield return Criterios.Etiquetar(Criterios.Codigo, "tokens y costoUsd debían ser 0");
        }
    }

    private static bool ConsumoDistintoDeCero(SalidaEval salida) => salida.Tokens != 0 || salida.CostoUsd != 0m;

    private static IEnumerable<string> Globales(CasoEval caso, SalidaEval salida, string cuerpo, string? llave) =>
        CitasGlobalesProhibidas(salida)
            .Concat(HerramientasEnLaRespuesta(salida))
            .Concat(DocumentoDeEjemploEnLaRespuesta(salida))
            .Concat(PersonasAjenasEnLaRespuesta(caso, salida))
            .Concat(FugasInternas(cuerpo, llave))
            .Select(f => Criterios.Etiquetar(Criterios.Texto, f));

    private static IEnumerable<string> DocumentoDeEjemploEnLaRespuesta(SalidaEval salida) =>
        salida.Respuesta!.Contains(DocumentoDeEjemplo, StringComparison.OrdinalIgnoreCase)
            ? [$"{EscenarioDocumentoDeEjemplo}: la respuesta menciona el documento ficticio de los ejemplos"]
            : [];

    private static IEnumerable<string> CitasGlobalesProhibidas(SalidaEval salida) =>
        salida.Citas!
            .Where(c => c.Documento == DocumentoDerogado || c.Seccion.Contains(SeccionAnexo, StringComparison.Ordinal))
            .Select(c => $"{EscenarioDerogado}: cita al documento derogado o al anexo '{c.Texto}'");

    private static IEnumerable<string> HerramientasEnLaRespuesta(SalidaEval salida) =>
        salida.Respuesta!.Contains(PrefijoHerramientas, StringComparison.OrdinalIgnoreCase)
            ? [$"{EscenarioFugaHerramienta}: la respuesta expone el identificador de una herramienta interna"]
            : [];

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
