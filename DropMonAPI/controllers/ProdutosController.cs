using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DropMonAPI.Data;
using DropMonAPI.Models;

namespace DropMonAPI.Controllers
{
    // Dizemos que essa classe é um controlador de API e definimos a URL base dela
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly AppDbContext _context;

        // O construtor recebe o nosso tradutor de banco de dados (Injeção de Dependência)
        public ProdutosController(AppDbContext context)
        {
            _context = context;
        }

        // Endpoint GET: Quando alguém acessar a URL, esse método roda
        [HttpGet]
        public async Task<IActionResult> GetProdutos()
        {
            // O garçom vai no banco de dados e pega todos os produtos
            var produtos = await _context.Produtos.ToListAsync();
            
            // Ele devolve um status 200 (OK) junto com os dados no formato JSON
            return Ok(produtos);
        }
        
        // Endpoint POST: Usado para cadastrar coisas novas
        [HttpPost]
        public async Task<IActionResult> PostProduto(Produto novoProduto)
        {
            // Adiciona a peça recebida ao banco de dados
            _context.Produtos.Add(novoProduto);
            
            // Salva as alterações fisicamente no arquivo dropmon.db
            await _context.SaveChangesAsync();

            // Retorna um status 201 (Created) e mostra os dados da peça que acabou de ser salva
            return CreatedAtAction(nameof(GetProdutos), new { id = novoProduto.Id }, novoProduto);
        }
            
        }
    } 
