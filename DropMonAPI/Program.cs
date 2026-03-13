using DropMonAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. AVISANDO A API: Vamos usar a arquitetura de Controllers
builder.Services.AddControllers();

// 2. CONFIGURAÇÃO DO CORS: O "Passe VIP" para o Front-end
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTudo", policy =>
    {
        policy.AllowAnyOrigin()   // Permite que qualquer site acesse a API
              .AllowAnyMethod()   // Permite GET, POST, etc
              .AllowAnyHeader();  // Permite qualquer cabeçalho
    });
});

// 3. BANCO DE DADOS: Conectando o Entity Framework ao nosso SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=dropmon.db"));

var app = builder.Build();

// 4. ATIVANDO O CORS: (MUITO IMPORTANTE: Tem que vir ANTES do MapControllers)
app.UseCors("PermitirTudo");

// 5. ROTAS: Mapeando os caminhos para o nosso garçom (ProdutosController)
app.MapControllers();

// 6. LIGANDO O MOTOR: Só pode existir UM app.Run() e ele fica no final!
app.Run();
