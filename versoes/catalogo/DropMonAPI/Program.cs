using DropMonAPI.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DropMonAPI.Services.FotoStorage>();
builder.Services.AddSingleton<DropMonAPI.Services.SupabaseAuthService>();
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"] ?? "App_Data/keys";
if (!Path.IsPathRooted(dataProtectionPath))
    dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("DropMon");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "dropmon.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.SlidingExpiration = false;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Admin", policy => policy.RequireAssertion(context =>
        !builder.Configuration.GetValue<bool>("Admin:RequireAuthentication") ||
        context.User.Identity?.IsAuthenticated == true)));
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");
    if (builder.Configuration["Database:Provider"]?.Equals("Postgres", StringComparison.OrdinalIgnoreCase) == true)
        options.UseNpgsql(connectionString, postgres => postgres.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
    else
    {
        // Resolve o SQLite em relação ao projeto, independentemente do diretório do terminal.
        var sqlite = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        if (sqlite.DataSource != ":memory:" && !Path.IsPathRooted(sqlite.DataSource))
            sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
        options.UseSqlite(sqlite.ConnectionString);
    }
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    var metodoSeguro = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ||
        HttpMethods.IsOptions(context.Request.Method);
    if (builder.Configuration.GetValue<bool>("Admin:RequireAuthentication") && !metodoSeguro &&
        context.Request.Path.StartsWithSegments("/api") && context.Request.Headers["X-DropMon-Request"] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails {
            Status = 400, Detail = "Cabeçalho de segurança ausente."
        });
        return;
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
if (builder.Configuration.GetValue<bool>("Demo:Seed"))
{
    // A carga de demonstração é independente das migrações, que podem ser aplicadas externamente em produção.
    using var scope = app.Services.CreateScope();
    await DropMonAPI.Data.DemoData.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        scope.ServiceProvider.GetRequiredService<DropMonAPI.Services.FotoStorage>(), app.Environment.ContentRootPath);
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
