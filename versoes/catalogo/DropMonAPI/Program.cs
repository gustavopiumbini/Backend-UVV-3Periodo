using DropMonAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DropMonAPI.Services.FotoStorage>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");
    // Resolve o arquivo em relação ao projeto, independentemente do diretório do terminal.
    var sqlite = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    if (sqlite.DataSource != ":memory:" && !Path.IsPathRooted(sqlite.DataSource))
        sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
    options.UseSqlite(sqlite.ConnectionString);
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    // Em desenvolvimento e na demonstração do Render, prepara um banco vazio ao iniciar.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    if (builder.Configuration.GetValue<bool>("Demo:Seed"))
        await DropMonAPI.Data.DemoData.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            scope.ServiceProvider.GetRequiredService<DropMonAPI.Services.FotoStorage>(), app.Environment.WebRootPath);
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

// Permite iniciar a aplicação real nos testes de integração.
public partial class Program { }
