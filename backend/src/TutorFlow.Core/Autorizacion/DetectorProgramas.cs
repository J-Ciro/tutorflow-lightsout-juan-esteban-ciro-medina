namespace TutorFlow.Core.Autorizacion;

public static class DetectorProgramas
{
    public static bool MencionaProgramaAjeno(string textoNormalizado, IReadOnlyCollection<string> programasPropios, DirectorioAcademico directorio)
    {
        var tokens = TokenizadorTexto.Tokenizar(textoNormalizado);
        return directorio.Programas
            .Where(programa => !programasPropios.Contains(programa.Id))
            .Any(programa => Menciona(tokens, programa.Id) || Menciona(tokens, programa.Nombre));
    }

    private static bool Menciona(IReadOnlyList<TokenTexto> tokens, string texto) =>
        TokenizadorTexto.BuscarSecuencia(tokens, TokenizadorTexto.Valores(texto)) >= 0;
}
