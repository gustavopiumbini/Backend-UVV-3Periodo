using DropMonAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// LINHA NOVA 1: Avisando a API que vamos usar a arquitetura de Controllers
builder.Services.AddControllers();

// (Essa parte do banco de dados você já tinha feito)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=dropmon.db"));

var app = builder.Build();

// LINHA NOVA 2: Mapeando as URLs para os nossos Controllers (como o /api/produtos)
app.MapControllers();

app.Run();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
