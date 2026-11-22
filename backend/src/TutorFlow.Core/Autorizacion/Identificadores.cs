using System.Text.RegularExpressions;

namespace TutorFlow.Core.Autorizacion;

public static partial class Identificadores
{
    public static bool EsIdEstudiante(string? id) => id is not null && IdEstudiante().IsMatch(id);

    public static bool EsIdCoordinador(string? id) => id is not null && IdCoordinador().IsMatch(id);

    public static bool EsIdPrograma(string? id) => id is not null && IdPrograma().IsMatch(id);

    [GeneratedRegex(@"^EST-[0-9]{3}\z")]
    private static partial Regex IdEstudiante();

    [GeneratedRegex(@"^COORD-[0-9]{2}\z")]
    private static partial Regex IdCoordinador();

    [GeneratedRegex(@"^[A-Z]{2,5}(-[A-Z]{2,5})?\z")]
    private static partial Regex IdPrograma();
}
