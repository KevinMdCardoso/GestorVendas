using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestorVendas.Application.DTOs;
using GestorVendas.Application.Interfaces;
using GestorVendas.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GestorVendas.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public AuthService(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var usuario = await _uow.Usuarios.ObterPorLoginAsync(req.Login)
            ?? throw new UnauthorizedAccessException("Login ou senha inválidos.");

        if (!usuario.Ativo)
            throw new UnauthorizedAccessException("Usuário inativo.");

        if (!VerificarSenha(req.Senha, usuario.SenhaHash))
            throw new UnauthorizedAccessException("Login ou senha inválidos.");

        var nomeExibicao = usuario.EmpresaId == null
            ? "Gestor de Vendas"
            : usuario.Empresa?.NomeFantasia ?? usuario.Empresa?.RazaoSocial ?? "Empresa";

        var expiracao = DateTime.UtcNow.AddHours(10);
        var token = GerarToken(usuario.Id, usuario.Login, usuario.Nome, usuario.Perfil, usuario.EmpresaId, expiracao);

        return new LoginResponse(token, usuario.Nome, usuario.Perfil, usuario.EmpresaId, nomeExibicao, expiracao);
    }

    private string GerarToken(Guid id, string login, string nome, Domain.Enums.PerfilUsuario perfil, Guid? empresaId, DateTime expiracao)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada.")));
        var creds = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, login),
            new("nome", nome),
            new(ClaimTypes.Role, perfil.ToString()),
            new("perfil", ((int)perfil).ToString())
        };

        if (empresaId.HasValue)
            claims.Add(new("empresaId", empresaId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "GestorVendas",
            audience: _config["Jwt:Audience"] ?? "GestorVendas",
            claims: claims,
            expires: expiracao,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashSenha(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(senha), salt, 100_000,
            HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerificarSenha(string senha, string senhaHash)
    {
        var partes = senhaHash.Split(':');
        if (partes.Length != 2) return false;
        var salt = Convert.FromBase64String(partes[0]);
        var hashEsperado = Convert.FromBase64String(partes[1]);
        var hashInformado = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(senha), salt, 100_000,
            HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hashInformado, hashEsperado);
    }
}
