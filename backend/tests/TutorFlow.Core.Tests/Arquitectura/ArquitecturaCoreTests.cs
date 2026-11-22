using System.Reflection;
using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Tests.Arquitectura;

public sealed class ArquitecturaCoreTests
{
    private static readonly Assembly Core = typeof(CalculosAcademicos).Assembly;

    [Fact]
    [Trait("AGENTS", "2")]
    public void Core_no_referencia_infrastructure_ni_api()
    {
        var referenciados = Core.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(referenciados, n => n.StartsWith("TutorFlow.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(referenciados, n => n.StartsWith("TutorFlow.Api", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AGENTS", "2")]
    public void Core_no_depende_de_frameworks_web_ni_de_http()
    {
        var referenciados = Core.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(referenciados, n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenciados, n => n.StartsWith("Microsoft.Extensions.Http", StringComparison.Ordinal));
        Assert.DoesNotContain(referenciados, n => n.StartsWith("System.Net.Http", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AGENTS", "2")]
    public void Core_no_expone_double_ni_float_en_los_calculos_academicos()
    {
        var tipos = typeof(CalculosAcademicos)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType))
            .Select(t => Nullable.GetUnderlyingType(t) ?? t);

        Assert.DoesNotContain(tipos, t => t == typeof(double) || t == typeof(float));
    }

    [Fact]
    [Trait("AGENTS", "2")]
    public void Modelos_academicos_no_usan_double_ni_float()
    {
        var tipos = Core.GetTypes()
            .Where(t => t.Namespace == typeof(NotasCorte).Namespace)
            .SelectMany(t => t.GetProperties())
            .Select(p => Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);

        Assert.DoesNotContain(tipos, t => t == typeof(double) || t == typeof(float));
    }
}
