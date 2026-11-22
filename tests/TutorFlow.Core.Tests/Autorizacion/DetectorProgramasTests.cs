using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Core.Tests.Autorizacion;

public sealed class DetectorProgramasTests
{
    private static readonly DirectorioAcademico Directorio = DatosReferencia.Cargar().Directorio;

    private static bool Ajeno(string pregunta, params string[] propios) =>
        DetectorProgramas.MencionaProgramaAjeno(NormalizadorTexto.Normalizar(pregunta), propios, Directorio);

    [Theory]
    [InlineData("¿Qué estudiantes de ADM perdieron materias?", "ESC-060")]
    [InlineData("¿Qué estudiantes de Administración de Empresas perdieron materias?", "ESC-060")]
    [InlineData("¿Mi programa tiene mejor promedio que ADM?", "ESC-133")]
    [InlineData("compara ING-SIS con adm", "SEG-04b")]
    [InlineData("estudiantes de administracion de empresas", "SEG-04b")]
    [Trait("CA", "CA-04")]
    [Trait("SEG", "SEG-04b")]
    public void Coordinador_de_ing_sis_que_nombra_adm_menciona_programa_ajeno(string pregunta, string referencia)
    {
        Assert.True(Ajeno(pregunta, "ING-SIS"), referencia);
    }

    [Theory]
    [InlineData("¿Qué trámite administrativo se hace para cancelar una asignatura?", "ESC-125")]
    [InlineData("¿Qué estudiantes de ING-SIS perdieron materias?", "SEG-04b")]
    [InlineData("¿Qué estudiantes de Ingeniería de Sistemas perdieron materias?", "SEG-04b")]
    [InlineData("¿Cuántos estudiantes de mi programa están en prueba académica?", "SEG-04b")]
    [InlineData("¿cuándo es la admisión?", "SEG-04b")]
    [Trait("CA", "CA-12")]
    [Trait("SEG", "SEG-04b")]
    public void Coordinador_de_ing_sis_no_menciona_programa_ajeno_con_palabras_parecidas_o_propias(string pregunta, string referencia)
    {
        Assert.False(Ajeno(pregunta, "ING-SIS"), referencia);
    }

    [Theory]
    [InlineData("¿Cuántos estudiantes de Administración de Empresas están en prueba académica?", false)]
    [InlineData("¿Qué estudiantes de Ingeniería de Sistemas perdieron?", true)]
    [InlineData("estudiantes de ING-SIS", true)]
    [Trait("ESC", "ESC-124")]
    [Trait("SEG", "SEG-04b")]
    public void Coordinador_de_adm_distingue_su_programa_del_ajeno(string pregunta, bool esperado)
    {
        Assert.Equal(esperado, Ajeno(pregunta, "ADM"));
    }

    [Fact]
    [Trait("ESC", "ESC-136")]
    [Trait("SEG", "SEG-04b")]
    public void Coordinador_con_dos_programas_no_tiene_programa_ajeno_entre_ellos()
    {
        Assert.False(Ajeno("comparar ADM con Ingeniería de Sistemas", "ING-SIS", "ADM"));
    }

    [Fact]
    [Trait("ESC", "ESC-135")]
    [Trait("SEG", "SEG-04b")]
    public void Coordinador_sin_programas_menciona_programa_ajeno_con_cualquiera()
    {
        Assert.True(Ajeno("estudiantes de ADM"));
    }
}
