using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Veterinaria.Api.Data;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Veterinaria")
    ?? throw new InvalidOperationException("Configura ConnectionStrings__Veterinaria mediante secretos o variables de entorno.");
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "https://lab12-veterinaria-api.onrender.com";
if (!Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var origin) ||
    origin.Scheme != "https" || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query))
    throw new InvalidOperationException("Cors:AllowedOrigin debe ser un origen HTTPS concreto sin ruta.");
builder.Services.AddDbContext<VeterinariaContext>(options => options.UseNpgsql(connection));
builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, context, ct) =>
{
    document.Info.Title = "Veterinaria API";
    document.Info.Description = "Laboratorio 12: propietarios y mascotas con ASP.NET Core 10, Entity Framework Core y PostgreSQL en Neon.";
    return Task.CompletedTask;
}));
builder.Services.AddCors(options => options.AddPolicy("ClienteLaboratorio", policy =>
    policy.WithOrigins(allowedOrigin.TrimEnd('/')).AllowAnyHeader().AllowAnyMethod()));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (builder.Configuration["RENDER"] == "true")
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});
var app = builder.Build();
app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
        (pg.SqlState == PostgresErrorCodes.UniqueViolation || pg.SqlState == PostgresErrorCodes.ForeignKeyViolation))
    {
        var conflict = pg.SqlState == PostgresErrorCodes.UniqueViolation || context.Request.Method == "DELETE";
        context.Response.StatusCode = conflict ? 409 : 400;
        await context.Response.WriteAsJsonAsync(new { mensaje = conflict
            ? "La operación incumple una regla de unicidad o tiene registros dependientes."
            : "La clave foránea indicada no existe." });
    }
    catch (DbUpdateConcurrencyException)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { mensaje = "El registro ya no existe." });
    }
});
app.UseHttpsRedirection();
app.UseCors("ClienteLaboratorio");
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Veterinaria API v1"));
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", async (VeterinariaContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { estado = "ok", baseDeDatos = "PostgreSQL" })
        : Results.StatusCode(503)).ExcludeFromDescription();
app.Run();
