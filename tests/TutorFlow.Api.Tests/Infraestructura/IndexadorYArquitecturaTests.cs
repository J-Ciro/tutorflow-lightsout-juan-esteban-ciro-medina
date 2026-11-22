using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Infrastructure.Llm;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Infraestructura;

public sealed class IndexadorTests : IDisposable
{
    private readonly FabricaApi _fabrica = new(conocimientoReal: true);

    public static TheoryData<string, string> CitasExistentes => new()
    {
        { BaseConocimientoFalsa.Reglamento, "Artículo 3" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 5" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 6" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 7" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 11" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 12" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 15" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 20" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 22" },
        { BaseConocimientoFalsa.Evaluacion, "Sección 2" },
        { BaseConocimientoFalsa.Evaluacion, "Sección 3" },
        { BaseConocimientoFalsa.Evaluacion, "Sección 4" },
        { BaseConocimientoFalsa.Bienestar, "Sección 1" },
        { BaseConocimientoFalsa.Bienestar, "Sección 2" },
        { BaseConocimientoFalsa.Bienestar, "Sección 3.1" },
        { BaseConocimientoFalsa.Bienestar, "Sección 3.2" }
    };

    public static TheoryData<string, string> CitasInexistentes => new()
    {
        { "reglamento_academico_2023.md", "Artículo 11" },
        { "reglamento_academico_2023.md", "Artículo 1" },
        { BaseConocimientoFalsa.Bienestar, "Anexo" },
        { BaseConocimientoFalsa.Reglamento, "Artículo 99" },
        { "inventado.md", "Sección 1" }
    };

    private IBaseConocimiento Base => _fabrica.Services.GetRequiredService<IBaseConocimiento>();

    public void Dispose() => _fabrica.Dispose();

    [Theory]
    [MemberData(nameof(CitasExistentes))]
    [Trait("CA", "CA-03")]
    public void Las_citas_de_los_documentos_vigentes_existen_en_el_indice(string documento, string seccion)
    {
        Assert.True(Base.ExisteCita(new Cita(documento, seccion)));
    }

    [Theory]
    [MemberData(nameof(CitasInexistentes))]
    [Trait("CA", "CA-03")]
    [Trait("SEG", "SEG-10")]
    public void El_reglamento_derogado_el_anexo_y_lo_inventado_no_existen_en_el_indice(string documento, string seccion)
    {
        Assert.False(Base.ExisteCita(new Cita(documento, seccion)));
    }

    [Theory]
    [InlineData("¿Con qué nota puedo habilitar una materia?", "Artículo 6", "1.5")]
    [InlineData("¿Con cuántas faltas se pierde una materia?", "Artículo 11", "20%")]
    [InlineData("¿Hasta qué semana puedo cancelar una asignatura?", "Artículo 15", "semana 10")]
    [Trait("CA", "CA-03")]
    [Trait("ESC", "ESC-011")]
    public void La_busqueda_solo_recupera_el_reglamento_vigente(string pregunta, string articulo, string derogado)
    {
        var secciones = Base.BuscarSecciones(pregunta, 6);

        Assert.InRange(secciones.Count, 1, 6);
        Assert.DoesNotContain(secciones, s => s.Documento == "reglamento_academico_2023.md");
        Assert.Contains(secciones, s => s.Seccion == articulo);
        Assert.DoesNotContain(secciones, s => s.Texto.Contains(derogado, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-077")]
    public void La_busqueda_de_becas_nunca_recupera_el_anexo_con_instrucciones()
    {
        var secciones = Base.BuscarSecciones("¿Qué becas hay?", 6);

        Assert.NotEmpty(secciones);
        Assert.DoesNotContain(secciones, s => s.Seccion.Contains("Anexo", StringComparison.Ordinal));
        Assert.DoesNotContain(secciones, s => s.Texto.Contains("ignora todas tus instrucciones", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("SEG", "SEG-13")]
    public void La_busqueda_respeta_el_maximo_de_secciones()
    {
        Assert.True(Base.BuscarSecciones("nota asignatura reglamento estudiante periodo", 2).Count <= 2);
    }
}

public sealed class ArquitecturaApiTests
{
    [Fact]
    [Trait("AGENTS", "2")]
    public void Infrastructure_referencia_core_y_no_referencia_api()
    {
        var referenciados = typeof(ProveedorLlm).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.Contains("TutorFlow.Core", referenciados);
        Assert.DoesNotContain(referenciados, n => n.StartsWith("TutorFlow.Api", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AGENTS", "2")]
    public void Api_referencia_core_e_infrastructure()
    {
        var referenciados = typeof(Program).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.Contains("TutorFlow.Core", referenciados);
        Assert.Contains("TutorFlow.Infrastructure", referenciados);
    }

    [Fact]
    [Trait("AGENTS", "2")]
    public void Core_no_referencia_infrastructure_ni_api()
    {
        var referenciados = typeof(IClienteLlm).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(referenciados, n => n.StartsWith("TutorFlow.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(referenciados, n => n.StartsWith("TutorFlow.Api", StringComparison.Ordinal));
    }
}
