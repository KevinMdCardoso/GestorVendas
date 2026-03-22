using GestorVendas.Application.DTOs;
using GestorVendas.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestorVendas.Web.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithName("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout");
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest req,
        IAuthService authService,
        HttpContext httpContext)
    {
        try
        {
            var log = $"🔐 [LOGIN] Tentativa de login: {req.Login}";
            System.Diagnostics.Debug.WriteLine(log);
            Console.WriteLine(log);

            // Valida credenciais
            var resp = await authService.LoginAsync(req);
            var logOk = $"✅ [LOGIN] Credenciais validadas. Usuário: {resp.Nome}, Perfil: {resp.Perfil}";
            System.Diagnostics.Debug.WriteLine(logOk);
            Console.WriteLine(logOk);

            // Cria claims para o cookie
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, req.Login),
                new(ClaimTypes.Name, resp.Nome),
                new(ClaimTypes.Role, resp.Perfil.ToString()),
                new("perfil", ((int)resp.Perfil).ToString()),
                new("nomeExibicao", resp.NomeEmpresaOuSistema)
            };

            if (resp.EmpresaId.HasValue)
                claims.Add(new("empresaId", resp.EmpresaId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var logCookie = $"🍪 [LOGIN] Chamando SignInAsync com {claims.Count} claims. Scheme: {CookieAuthenticationDefaults.AuthenticationScheme}";
            System.Diagnostics.Debug.WriteLine(logCookie);
            Console.WriteLine(logCookie);

            // Persiste autenticação via cookie
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties 
                { 
                    IsPersistent = true,
                    ExpiresUtc = resp.Expiracao
                });

            var cookieCount = httpContext.Response.Headers.Where(h => h.Key == "Set-Cookie").Count();
            var logDone = $"✅ [LOGIN] SignInAsync completo. Headers Set-Cookie: {cookieCount}";
            System.Diagnostics.Debug.WriteLine(logDone);
            Console.WriteLine(logDone);

            // Verifica se o cookie foi adicionado
            if (cookieCount == 0)
            {
                var logWarn = $"⚠️  [LOGIN] AVISO: Nenhum Set-Cookie header encontrado!";
                System.Diagnostics.Debug.WriteLine(logWarn);
                Console.WriteLine(logWarn);
            }

            return Results.Ok(resp);
        }
        catch (UnauthorizedAccessException ex)
        {
            var logUnauth = $"❌ [LOGIN] Credenciais inválidas: {ex.Message}";
            System.Diagnostics.Debug.WriteLine(logUnauth);
            Console.WriteLine(logUnauth);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            var logErr = $"❌ [LOGIN] Erro: {ex.Message} | {ex.StackTrace}";
            System.Diagnostics.Debug.WriteLine(logErr);
            Console.WriteLine(logErr);
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }
}
