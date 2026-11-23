using System.Text.Json;
using System.Text.RegularExpressions;
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

    [Theory]
    [MemberData(nameof(Roles))]
    [Trait("ESC", "ESC-173")]
    [Trait("SEG", "SEG-10d")]
    public void El_bloque_de_ejemplos_no_contiene_cifras_con_forma_de_nota_ni_de_porcentaje(Rol rol)
    {
        Assert.DoesNotMatch(CifraDeNotaOPorcentaje(), Ejemplos(rol));
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

    [GeneratedRegex(@"(?<![0-9.])[0-9]\.[0-9]{1,2}(?![0-9])|[0-9]+(\.[0-9]{1,2})?\s*%", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CifraDeNotaOPorcentaje();
}
