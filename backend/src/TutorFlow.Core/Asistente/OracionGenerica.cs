using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class OracionGenerica
{
    private const string Hay = "hay";
    private const int PalabrasDePorInasistencia = 2;

    private static readonly HashSet<string> Marcadores = new(StringComparer.Ordinal)
    {
        "tu", "tus", "te", "ti", "tienes", "tengo", "mi", "mis", "me", "llevas", "usted", "perdiste", "perdi", "reprobaste", "dos", "tres", "cuatro"
    };

    private static readonly HashSet<string> AntecesoresDeHay = new(StringComparer.Ordinal) { "si", "que" };

    private readonly BloqueDeTexto _bloque;
    private readonly IReadOnlyList<TokenTexto> _tokens;
    private readonly bool _esEstudiante;
    private readonly IReadOnlyList<IReadOnlyList<string>> _identificadoresDelUsuario;

    public OracionGenerica(BloqueDeTexto bloque, IReadOnlyList<TokenTexto> tokens, Usuario usuario, DirectorioAcademico directorio)
    {
        _bloque = bloque;
        _tokens = tokens;
        _esEstudiante = usuario.Rol == Rol.Estudiante;
        _identificadoresDelUsuario = IdentificadoresDe(usuario, directorio);
    }

    public bool Es(FormaDePerdida forma) =>
        _esEstudiante && !TieneMarcadorPersonal() && TienePalabrasAjenas(forma);

    private static List<IReadOnlyList<string>> IdentificadoresDe(Usuario usuario, DirectorioAcademico directorio)
    {
        var nombre = directorio.Estudiantes.FirstOrDefault(estudiante => estudiante.Id == usuario.Id)?.Nombre;
        var identificadores = new List<IReadOnlyList<string>> { TokenizadorTexto.Valores(usuario.Id) };
        identificadores.AddRange(nombre is null ? [] : [TokenizadorTexto.Valores(nombre)]);
        return identificadores;
    }

    private bool TieneMarcadorPersonal() =>
        Enumerable.Range(0, _tokens.Count).Any(EsMarcadorEn)
        || _identificadoresDelUsuario.Any(secuencia => TokenizadorTexto.BuscarSecuencia(_tokens, secuencia) >= 0);

    private bool EsMarcadorEn(int indice) =>
        Marcadores.Contains(_tokens[indice].Valor)
        || (_tokens[indice].Valor == Hay && (indice == 0 || !AntecesoresDeHay.Contains(_tokens[indice - 1].Valor)));

    private bool TienePalabrasAjenas(FormaDePerdida forma)
    {
        var finDeLaForma = forma.Fin + (forma.PorInasistencia ? PalabrasDePorInasistencia : 0);
        return Enumerable.Range(0, _tokens.Count)
            .Any(indice => _tokens[indice].Inicio >= _bloque.InicioDeLaOracion && (indice < forma.Inicio || indice > finDeLaForma));
    }
}
