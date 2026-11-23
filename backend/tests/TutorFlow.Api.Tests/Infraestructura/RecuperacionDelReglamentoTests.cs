using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Infraestructura;

public sealed class RecuperacionDelReglamentoTests : IDisposable
{
    private const int MaximoDeSecciones = 6;
    private const int PrimerasPosiciones = 2;

    private readonly FabricaApi _fabrica = new(conocimientoReal: true);

    public static TheoryData<string, string, string> PrimerasDosSecciones => new()
    {
        { "¿Cómo se calcula la nota definitiva de una asignatura?", "Sección 2", "Sección 3" }
    };

    private IBaseConocimiento Base => _fabrica.Services.GetRequiredService<IBaseConocimiento>();

    public void Dispose() => _fabrica.Dispose();

    [Theory]
    [MemberData(nameof(PrimerasDosSecciones))]
    [Trait("CA", "CA-01")]
    public void Las_preguntas_de_evaluacion_entregan_sus_secciones_esperadas_en_las_dos_primeras_posiciones(string pregunta, string primera, string segunda)
    {
        var secciones = Base.BuscarSecciones(pregunta, MaximoDeSecciones);

        var primeras = secciones.Take(PrimerasPosiciones).Select(s => (s.Documento, s.Seccion)).ToList();
        Assert.Contains((BaseConocimientoFalsa.Evaluacion, primera), primeras);
        Assert.Contains((BaseConocimientoFalsa.Evaluacion, segunda), primeras);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("D", "D-66")]
    public void La_pregunta_del_promedio_ponderado_entrega_exactamente_la_seccion_4_de_evaluacion()
    {
        var secciones = Base.BuscarSecciones("¿Cuál es mi promedio ponderado del periodo?", MaximoDeSecciones);

        var primeras = secciones.Take(PrimerasPosiciones).Select(s => (s.Documento, s.Seccion)).ToList();
        Assert.Equal([(BaseConocimientoFalsa.Evaluacion, "Sección 4")], primeras);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    public void Una_pregunta_de_bienestar_sigue_entregando_primero_la_seccion_1_de_bienestar()
    {
        var secciones = Base.BuscarSecciones("¿Cuál es el horario de atención de Bienestar?", MaximoDeSecciones);

        Assert.NotEmpty(secciones);
        Assert.Equal((BaseConocimientoFalsa.Bienestar, "Sección 1"), (secciones[0].Documento, secciones[0].Seccion));
    }
}
