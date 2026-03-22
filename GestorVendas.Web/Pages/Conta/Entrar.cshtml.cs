using GestorVendas.Application.DTOs;
using GestorVendas.Application.Interfaces;
using GestorVendas.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GestorVendas.Web.Pages.Conta;

[IgnoreAntiforgeryToken]
public class EntrarModel : PageModel
{
    private readonly IAuthService _authService;

    public EntrarModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty] public string Login { get; set; } = "";
    [BindProperty] public string Senha { get; set; } = "";

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var resp = await _authService.LoginAsync(new LoginRequest(Login, Senha));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Login),
                new(ClaimTypes.Name, resp.Nome),
                new(ClaimTypes.Role, resp.Perfil.ToString()),
                new("perfil", ((int)resp.Perfil).ToString()),
                new("nomeExibicao", resp.NomeEmpresaOuSistema)
            };

            if (resp.EmpresaId.HasValue)
                claims.Add(new("empresaId", resp.EmpresaId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = resp.Expiracao
                });

            var destino = resp.Perfil switch
            {
                PerfilUsuario.Admin   => "/admin/dashboard",
                PerfilUsuario.Gerente => "/gerente/dashboard",
                _                     => "/operador/pdv"
            };

            return LocalRedirect(destino);
        }
        catch (UnauthorizedAccessException)
        {
            return LocalRedirect("/login?erro=credenciais");
        }
        catch
        {
            return LocalRedirect("/login?erro=servidor");
        }
    }
}
