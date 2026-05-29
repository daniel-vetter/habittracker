using HabitTracker.WebApp.Database;
using HabitTracker.WebApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.AddNpgsqlDbContext<Db>("db");

builder.Services.AddOpenApiDocument(x =>
{
    x.DefaultResponseReferenceTypeNullHandling =
        NJsonSchema.Generation.ReferenceTypeNullHandling.NotNull;
    x.Title = "HabitTracker API";
});

var app = builder.Build();

// Must come BEFORE app.Run() and BEFORE any DB migration: when invoked with
// --generateTypeScriptClient the app emits the Angular client and exits.
if (await app.Services.CreateTypeScriptClient(args)) return;

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<Db>().Database.MigrateAsync();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();
app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
