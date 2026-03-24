using System.Security.Claims;
using GestorVendas.Application.DTOs;
using GestorVendas.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace GestorVendas.Web.Services;

// ── SESSÃO ────────────────────────────────────────────────────
public class SessaoService
{
    private readonly NavigationManager _nav;
    private const string TokenKey = "gestor_vendas_token";

    public LoginResponse? Usuario { get; private set; }
    public bool Autenticado => Usuario != null;

    public SessaoService(NavigationManager nav)
    {
        _nav = nav;
    }

    public Guid UsuarioId => Usuario?.UsuarioId ?? Guid.Empty;

    public Guid? EmpresaId => Usuario?.EmpresaId;
    public PerfilUsuario Perfil => Usuario?.Perfil ?? PerfilUsuario.Operador;
    public string NomeExibicao => Usuario?.NomeEmpresaOuSistema ?? "Gestor de Vendas";
    public string NomeUsuario => Usuario?.Nome ?? "";

    public bool IsAdmin => Perfil == PerfilUsuario.Admin;
    public bool IsGerente => Perfil == PerfilUsuario.Gerente;
    public bool IsOperador => Perfil == PerfilUsuario.Operador;
    public bool IsGerenteOuAdmin => IsAdmin || IsGerente;

    private ClaimsPrincipal? _principal;

    public event Action? OnChange;

    public void Entrar(LoginResponse response)
    {
        Usuario = response;
        _principal = CriarPrincipal(response);
        OnChange?.Invoke();
    }

    public void Sair()
    {
        Usuario = null;
        _principal = null;
        _nav.NavigateTo("/login", forceLoad: true);
        OnChange?.Invoke();
    }

    public void SincronizarComClaims(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var nome = principal.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var perfilStr = principal.FindFirst("perfil")?.Value ?? "2";
            var nomeExibicao = principal.FindFirst("nomeExibicao")?.Value ?? "Gestor de Vendas";
            var empresaIdStr = principal.FindFirst("empresaId")?.Value;
            var usuarioIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(perfilStr, out var perfilInt))
            {
                Usuario = new LoginResponse(
                    Token: "",
                    Nome: nome,
                    Perfil: (PerfilUsuario)perfilInt,
                    EmpresaId: Guid.TryParse(empresaIdStr, out var eid) ? eid : null,
                    NomeEmpresaOuSistema: nomeExibicao,
                    Expiracao: DateTime.UtcNow.AddHours(10),
                    UsuarioId: Guid.TryParse(usuarioIdStr, out var uid) ? uid : Guid.Empty
                );
                _principal = principal;
                OnChange?.Invoke();
            }
        }
        else
        {
            Usuario = null;
            _principal = null;
        }
    }

    public ClaimsPrincipal ObterPrincipal() =>
        _principal ?? new ClaimsPrincipal(new ClaimsIdentity());

    private static ClaimsPrincipal CriarPrincipal(LoginResponse r)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, r.Nome),
            new(ClaimTypes.Role, r.Perfil.ToString()),
            new("perfil", ((int)r.Perfil).ToString()),
            new("nomeExibicao", r.NomeEmpresaOuSistema)
        };
        if (r.EmpresaId.HasValue)
            claims.Add(new("empresaId", r.EmpresaId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private string? GetClaim(string tipo) =>
        _principal?.FindFirst(tipo)?.Value;
}

// ── AUTH STATE PROVIDER ───────────────────────────────────────
public class SessaoAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SessaoService _sessao;

    public SessaoAuthStateProvider(IHttpContextAccessor httpContextAccessor, SessaoService sessao)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessao = sessao;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = _httpContextAccessor?.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        _sessao.SincronizarComClaims(principal);
        return Task.FromResult(new AuthenticationState(principal));
    }
}
