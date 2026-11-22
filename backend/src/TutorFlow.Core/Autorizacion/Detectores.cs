namespace TutorFlow.Core.Autorizacion;

public static class DetectorTerceros
{
    public static bool MencionaTercero(Usuario usuario, string textoNormalizado, DirectorioAcademico directorio) => throw new NotImplementedException();

    public static bool TieneFormaDeIdPersona(string textoNormalizado, DirectorioAcademico directorio) => throw new NotImplementedException();

    public static IReadOnlyList<string> PersonasMencionadas(string textoNormalizado, DirectorioAcademico directorio) => throw new NotImplementedException();

    public static string EnmascararTerceros(Usuario usuario, string pregunta, DirectorioAcademico directorio) => throw new NotImplementedException();
}

public static class DetectorAgregados
{
    public static bool PideAgregados(string textoNormalizado) => throw new NotImplementedException();
}

public static class DetectorProgramas
{
    public static bool MencionaProgramaAjeno(string textoNormalizado, IReadOnlyCollection<string> programasPropios, DirectorioAcademico directorio) => throw new NotImplementedException();
}
