using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Asistente;

public sealed partial class EjemplosDelPromptTests
{
    private const string DocumentoDeEjemplo = "documento_ejemplo.md";
    private const int PalabrasDeFragmento = 8;

    private static readonly BaseConocimientoFalsa Base = BaseConocimientoFalsa.Vigente();
    private static readonly DatosAcademicos Datos = DatosReferencia.Cargar();

    public static TheoryData<Rol> Roles => new() { Rol.Estudiante, Rol.Coordinador };

    private static string Ejemplos(Rol rol) => PartesDelPrompt.Ejemplos(ConstructorPrompts.PromptSistema(rol, null));

    private static string TextoDeLasPreguntasDeEjemplo()
    {
        var ruta = Path.Combine(DatosReferencia.RutaRepositorio(), "preguntas_ejemplo.json");
        using var documento = JsonDocument.Parse(File.ReadAllText(ruta));
        return string.Join(' ', documento.RootElement.EnumerateArray()
            .SelectMany(p => new[] { p.GetProperty("pregunta").GetString()!, p.GetProperty("respuestaEsperada").GetString()! }));
    }

    private static IEnumerable<string> NombresProhibidos() =>
        Datos.Estudiantes.SelectMany(e => new[] { e.Nombre, e.Id })
            .Concat(Datos.Coordinadores.SelectMany(c => new[] { c.Nombre, c.Id }))
            .Concat(Datos.Notas.SelectMany(n => new[] { n.Notas.Nombre, n.Notas.Codigo }))
            .Distinct();

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_bloque_de_ejemplos_cita_solo_el_documento_ficticio_que_no_esta_en_el_indice(Rol rol)
    {
        var documentos = DocumentosCitados().Matches(Ejemplos(rol)).Select(m => m.Value).Distinct().ToArray();

        Assert.Equal([DocumentoDeEjemplo], documentos);
        Assert.DoesNotContain(Base.Secciones, s => s.Documento == DocumentoDeEjemplo);
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_bloque_de_ejemplos_no_comparte_ocho_palabras_consecutivas_con_ninguna_seccion_del_indice(Rol rol)
    {
        var fragmentosDelEjemplo = PartesDelPrompt.Fragmentos(Ejemplos(rol), PalabrasDeFragmento);

        var coincidencias = Base.Secciones
            .Where(s => PartesDelPrompt.Fragmentos($"{s.Titulo} {s.Texto}", PalabrasDeFragmento).Overlaps(fragmentosDelEjemplo))
            .Select(s => $"{s.Documento} · {s.Seccion}")
            .ToArray();

        Assert.NotEmpty(fragmentosDelEjemplo);
        Assert.Empty(coincidencias);
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_bloque_de_ejemplos_no_comparte_ocho_palabras_consecutivas_con_las_preguntas_y_respuestas_de_Q10(Rol rol)
    {
        var fragmentosDelEjemplo = PartesDelPrompt.Fragmentos(Ejemplos(rol), PalabrasDeFragmento);
        var fragmentosDeQ10 = PartesDelPrompt.Fragmentos(TextoDeLasPreguntasDeEjemplo(), PalabrasDeFragmento);

        Assert.NotEmpty(fragmentosDeQ10);
        Assert.False(fragmentosDelEjemplo.Overlaps(fragmentosDeQ10));
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_bloque_de_ejemplos_no_contiene_nombres_ids_ni_asignaturas_del_registro(Rol rol)
    {
        var ejemplos = $" {NormalizadorTexto.Normalizar(Ejemplos(rol))} ";

        var presentes = NombresProhibidos()
            .Where(nombre => ejemplos.Contains($" {NormalizadorTexto.Normalizar(nombre)} ", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(presentes);
    }

    private static IReadOnlyList<string> RespuestasDeEjemplo(Rol rol) =>
        [.. PartesDelPrompt.CasosDeEjemplo(ConstructorPrompts.PromptSistema(rol, null))
            .Select(caso => caso[(caso.IndexOf("Respuesta:", StringComparison.Ordinal) + "Respuesta:".Length)..])];

    private static decimal Valor(string cifra) => decimal.Parse(cifra, CultureInfo.InvariantCulture);

    private static IEnumerable<decimal> CifrasDe(string texto) =>
        CifraDeNotaOPorcentaje().Matches(texto).Select(m => Valor(m.Groups["valor"].Value));

    private static IEnumerable<decimal> CifrasDeLosDatosDeReferencia() =>
        Datos.Estudiantes
            .Select(e => CalculosAcademicos.ResumirEstudiante(e.Id, Datos.AsignaturasDe(e.Id)))
            .SelectMany(resumen => resumen.Asignaturas.SelectMany(CifrasDeLaAsignatura).Concat(resumen.Promedio.HasValue ? [resumen.Promedio.Value] : []));

    private static IEnumerable<decimal> CifrasDeLaAsignatura(ResultadoAsignatura resultado)
    {
        var cortes = new[] { resultado.Asignatura.Cortes.Corte1, resultado.Asignatura.Cortes.Corte2, resultado.Asignatura.Cortes.Corte3 };
        var acumulada = resultado.Acumulada is null ? [] : new[] { resultado.Acumulada.Valor, resultado.Acumulada.PorcentajeEvaluado, resultado.Acumulada.EquivalenteSobreEvaluado };
        return cortes.Where(c => c.HasValue).Select(c => c!.Value)
            .Concat(resultado.Definitiva.HasValue ? [resultado.Definitiva.Value] : [])
            .Concat(acumulada);
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public void ESC_198i_El_bloque_tiene_exactamente_4_ejemplos_con_las_marcas_de_EJ_1_a_EJ_4(Rol rol)
    {
        var respuestas = RespuestasDeEjemplo(rol);

        Assert.Equal(4, respuestas.Count);
        Assert.Single(respuestas, r => r.Contains("(documento_ejemplo.md · Sección 1)", StringComparison.Ordinal));
        Assert.Single(respuestas, r => r.Contains("Definitivas:", StringComparison.Ordinal) && PromedioConDosDecimales().IsMatch(r) && ParDeAsignaturaYCifra().Count(r) >= 2);
        Assert.Single(respuestas, r => r.Contains("Requisitos:", StringComparison.Ordinal) && PorcentajeDeDescuento().IsMatch(r) && r.Contains("edificio", StringComparison.OrdinalIgnoreCase));
        Assert.Single(respuestas, r => FaltasDeClasesProgramadas().IsMatch(r));
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    public void ESC_198i_Ninguna_cifra_del_bloque_es_igual_como_decimal_a_una_cifra_del_indice_de_los_datos_o_de_las_preguntas_de_ejemplo(Rol rol)
    {
        var delBloque = CifrasDe(Ejemplos(rol)).Distinct().ToArray();
        var fuentes = Base.Secciones.SelectMany(s => CifrasDe($"{s.Titulo} {s.Texto}"))
            .Concat(CifrasDe(TextoDeLasPreguntasDeEjemplo()))
            .Concat(CifrasDeLosDatosDeReferencia())
            .ToHashSet();

        Assert.NotEmpty(delBloque);
        Assert.All(delBloque, cifra => Assert.DoesNotContain(cifra, fuentes));
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_bloque_de_ejemplos_tiene_al_menos_un_caso_con_pregunta_y_respuesta(Rol rol)
    {
        var casos = PartesDelPrompt.CasosDeEjemplo(ConstructorPrompts.PromptSistema(rol, null));

        Assert.NotEmpty(casos);
        Assert.All(casos, caso =>
        {
            Assert.Contains("Pregunta:", caso, StringComparison.Ordinal);
            Assert.Contains("Respuesta:", caso, StringComparison.Ordinal);
        });
    }

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_marcador_canario_va_en_el_prompt_y_fuera_del_bloque_de_ejemplos(Rol rol)
    {
        var prompt = ConstructorPrompts.PromptSistema(rol, "canario-9d41f0aa");

        Assert.Contains("canario-9d41f0aa", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("canario-9d41f0aa", PartesDelPrompt.Ejemplos(prompt), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"[A-Za-z0-9_\-]+\.md", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DocumentosCitados();

    [GeneratedRegex(@"(?<![0-9.])(?<valor>[0-9]\.[0-9]{1,2})(?![0-9])|(?<valor>[0-9]+(\.[0-9]{1,2})?)\s*%", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CifraDeNotaOPorcentaje();

    [GeneratedRegex(@"(?<![0-9.])[0-9]\.[0-9]{2}(?![0-9])", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PromedioConDosDecimales();

    [GeneratedRegex(@"\p{L}+ [0-9]\.[0-9]\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ParDeAsignaturaYCifra();

    [GeneratedRegex(@"[0-9]+\s*%\s*de descuento", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PorcentajeDeDescuento();

    [GeneratedRegex(@"[0-9]+ faltas de [0-9]+ clases programadas", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FaltasDeClasesProgramadas();
}
