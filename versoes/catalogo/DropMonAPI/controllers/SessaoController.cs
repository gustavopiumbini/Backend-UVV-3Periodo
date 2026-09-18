using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DropMonAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropMonAPI.Controllers;

public sealed record LoginRequest([param: Required, EmailAddress] string Email,
    [param: Required, MinLength(8), MaxLength(200)] string Senha);

[ApiController]
[Route("api/sessao")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class SessaoController(SupabaseAuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status() => Ok(new {
        authenticationRequired = auth.Required,
        configured = !auth.Required || auth.Configured,
        authenticated = !auth.Required || User.Identity?.IsAuthenticated == true,
        email = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.Email) : null
    });

    [AllowAnonymous]
    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar(LoginRequest request, CancellationToken ct)
    {
        if (!auth.Required) return NoContent();
        var outcome = await auth.LoginAsync(request.Email.Trim(), request.Senha, ct);
        if (outcome.Result == LoginResult.NotConfigured)
            return Problem(statusCode: 503, detail: "A autenticação administrativa ainda não foi configurada.");
        if (outcome.Result == LoginResult.Unavailable)
            return Problem(statusCode: 503, detail: "O serviço de autenticação está indisponível. Tente novamente.");
        if (outcome.Result != LoginResult.Success)
            return Problem(statusCode: 401, detail: "E-mail ou senha inválidos.");

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, outcome.UserId!),
            new Claim(ClaimTypes.Email, outcome.Email ?? request.Email.Trim())
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(outcome.ExpiresIn)
            });
        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        if (auth.Required) await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
