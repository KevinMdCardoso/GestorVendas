using GestorVendas.Application.DTOs;
using GestorVendas.Application.Interfaces;
using GestorVendas.Domain.Entities;
using GestorVendas.Domain.Enums;
using GestorVendas.Domain.Interfaces;

namespace GestorVendas.Application.Services;

// ── EMPRESA SERVICE ───────────────────────────────────────────
public class EmpresaService : IEmpresaService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthService _auth;

    public EmpresaService(IUnitOfWork uow, IAuthService auth)
    {
        _uow = uow;
        _auth = auth;
    }

    public async Task<IEnumerable<EmpresaDto>> ListarAsync()
    {
        var empresas = await _uow.Empresas.ObterAtivasAsync();
        var result = new List<EmpresaDto>();
        foreach (var e in empresas)
        {
            var usuarios = await _uow.Usuarios.ObterPorEmpresaAsync(e.Id);
            var produtos = await _uow.Produtos.ObterPorEmpresaAsync(e.Id);
            result.Add(ToDto(e, usuarios.Count(), produtos.Count()));
        }
        return result;
    }

    public async Task<EmpresaDto> ObterAsync(Guid id)
    {
        var e = await _uow.Empresas.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Empresa não encontrada.");
        var usuarios = await _uow.Usuarios.ObterPorEmpresaAsync(id);
        var produtos = await _uow.Produtos.ObterPorEmpresaAsync(id);
        return ToDto(e, usuarios.Count(), produtos.Count());
    }

    public async Task<EmpresaDto> CriarAsync(CriarEmpresaRequest req)
    {
        if (await _uow.Empresas.ObterPorCnpjAsync(req.Cnpj) != null)
            throw new InvalidOperationException("Já existe uma empresa com este CNPJ.");

        if (await _uow.Usuarios.LoginExisteAsync(req.GerenteLogin))
            throw new InvalidOperationException("Login do gerente já está em uso.");

        var empresa = new Empresa
        {
            RazaoSocial = req.RazaoSocial,
            NomeFantasia = req.NomeFantasia,
            Cnpj = req.Cnpj.Replace(".", "").Replace("/", "").Replace("-", ""),
            Telefone = req.Telefone,
            Email = req.Email
        };
        await _uow.Empresas.AdicionarAsync(empresa);

        var gerente = new Usuario
        {
            Nome = req.GerenteNome,
            Login = req.GerenteLogin,
            SenhaHash = _auth.HashSenha(req.GerентеSenha),
            Perfil = PerfilUsuario.Gerente,
            EmpresaId = empresa.Id
        };
        await _uow.Usuarios.AdicionarAsync(gerente);

        var config = new ConfiguracaoEmpresa
        {
            EmpresaId = empresa.Id,
            DescontoMaximoPermitido = 10,
            EstoqueAlertaQuantidade = 10
        };
        await _uow.Configuracoes.SalvarAsync(config);

        await _uow.SalvarAsync();
        return ToDto(empresa, 1, 0);
    }

    public async Task<EmpresaDto> AtualizarAsync(Guid id, AtualizarEmpresaRequest req)
    {
        var e = await _uow.Empresas.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Empresa não encontrada.");

        e.RazaoSocial = req.RazaoSocial;
        e.NomeFantasia = req.NomeFantasia;
        e.Telefone = req.Telefone;
        e.Email = req.Email;
        e.AtualizadoEm = DateTime.UtcNow;

        await _uow.Empresas.AtualizarAsync(e);
        await _uow.SalvarAsync();

        var usuarios = await _uow.Usuarios.ObterPorEmpresaAsync(id);
        var produtos = await _uow.Produtos.ObterPorEmpresaAsync(id);
        return ToDto(e, usuarios.Count(), produtos.Count());
    }

    public async Task DesativarAsync(Guid id)
    {
        var e = await _uow.Empresas.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Empresa não encontrada.");
        e.Ativo = false;
        e.AtualizadoEm = DateTime.UtcNow;
        await _uow.Empresas.AtualizarAsync(e);
        await _uow.SalvarAsync();
    }

    private static EmpresaDto ToDto(Empresa e, int totalUsuarios, int totalProdutos) => new(
        e.Id, e.RazaoSocial, e.NomeFantasia, e.Cnpj,
        e.Telefone, e.Email, e.Ativo, totalUsuarios, totalProdutos);
}

// ── USUARIO SERVICE ───────────────────────────────────────────
public class UsuarioService : IUsuarioService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthService _auth;

    public UsuarioService(IUnitOfWork uow, IAuthService auth)
    {
        _uow = uow;
        _auth = auth;
    }

    public async Task<IEnumerable<UsuarioDto>> ListarPorEmpresaAsync(Guid empresaId)
    {
        var lista = await _uow.Usuarios.ObterPorEmpresaAsync(empresaId);
        return lista.Select(ToDto);
    }

    public async Task<UsuarioDto> ObterAsync(Guid id)
    {
        var u = await _uow.Usuarios.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");
        return ToDto(u);
    }

    public async Task<UsuarioDto> CriarAsync(CriarUsuarioRequest req, Guid empresaCriadorId)
    {
        // Operador só pode ser criado dentro da mesma empresa
        if (req.Perfil == PerfilUsuario.Operador && req.EmpresaId != empresaCriadorId)
            throw new InvalidOperationException("Operador deve pertencer à mesma empresa.");

        if (req.Perfil == PerfilUsuario.Admin)
            throw new InvalidOperationException("Não é permitido criar usuários Admin por este fluxo.");

        if (await _uow.Usuarios.LoginExisteAsync(req.Login))
            throw new InvalidOperationException("Login já está em uso.");

        var usuario = new Usuario
        {
            Nome = req.Nome,
            Login = req.Login,
            SenhaHash = _auth.HashSenha(req.Senha),
            Perfil = req.Perfil,
            EmpresaId = req.EmpresaId ?? empresaCriadorId
        };

        await _uow.Usuarios.AdicionarAsync(usuario);
        await _uow.SalvarAsync();
        return ToDto(usuario);
    }

    public async Task<UsuarioDto> AtualizarAsync(Guid id, AtualizarUsuarioRequest req)
    {
        var u = await _uow.Usuarios.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        u.Nome = req.Nome;
        if (!string.IsNullOrEmpty(req.NovaSenha))
            u.SenhaHash = _auth.HashSenha(req.NovaSenha);
        u.AtualizadoEm = DateTime.UtcNow;

        await _uow.Usuarios.AtualizarAsync(u);
        await _uow.SalvarAsync();
        return ToDto(u);
    }

    public async Task DesativarAsync(Guid id)
    {
        var u = await _uow.Usuarios.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");
        u.Ativo = false;
        u.AtualizadoEm = DateTime.UtcNow;
        await _uow.Usuarios.AtualizarAsync(u);
        await _uow.SalvarAsync();
    }

    private static UsuarioDto ToDto(Usuario u) => new(
        u.Id, u.Nome, u.Login, u.Perfil,
        u.EmpresaId, u.Empresa?.NomeFantasia, u.Ativo);
}
