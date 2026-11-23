using TutorFlow.Api;

const int LimiteDuroCuerpoBytes = 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(opciones =>
{
    opciones.AddServerHeader = false;
    opciones.Limits.MaxRequestBodySize = LimiteDuroCuerpoBytes;
});
builder.Services.AgregarTutorFlow(builder.Configuration);

var app = builder.Build();

app.UsarProxyConfiable(app.Configuration);
app.UsarCorrelacion();
app.UsarEncabezadosDeSeguridad();
app.UseExceptionHandler();

app.MapGet("/api/salud", () => Results.Ok(new EstadoSalud("ok")));

app.MapPost(
    "/api/asistente/preguntar",
    (HttpContext contexto, ProcesadorPregunta procesador, CancellationToken cancellationToken) =>
        procesador.ProcesarAsync(contexto, cancellationToken));

app.Run();

public partial class Program;
