using System.Reflection;
using Microsoft.Extensions.Configuration;
using Xunit.Sdk;

namespace TutorFlow.Api.Tests.Operacion;

internal static class ValidadorPendiente
{
    private const string NombreDelTipo = "TutorFlow.Api.ValidadorDeConfiguracion";
    private const string NombreDelMetodo = "Validar";

    public static IReadOnlyList<string> Validar(IConfiguration configuracion)
    {
        var metodo = typeof(Program).Assembly.GetType(NombreDelTipo)?.GetMethod(
            NombreDelMetodo,
            BindingFlags.Public | BindingFlags.Static,
            [typeof(IConfiguration)])
            ?? throw new XunitException(
                $"falta la clase estática pública {NombreDelTipo} con 'public static IReadOnlyList<string> {NombreDelMetodo}(IConfiguration configuracion)' (spec 001 §11.4, ESC-181 af)");
        return (IReadOnlyList<string>)metodo.Invoke(null, [configuracion])!;
    }
}
