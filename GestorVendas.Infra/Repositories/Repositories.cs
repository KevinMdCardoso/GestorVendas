using GestorVendas.Domain.Entities;
using GestorVendas.Domain.Enums;
using GestorVendas.Domain.Interfaces;
using GestorVendas.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorVendas.Infra.Repositories;

// ── BASE ──────────────────────────────────────────────────────
public class Repositorio<T> : IRepositorio<T> where T : EntidadeBase
{
    protected readonly AppDbContext _ctx;
    protected readonly DbSet<T> _set;

    public Repositorio(AppDbContext ctx) { _ctx = ctx; _set = ctx.Set<T>(); }

    public async Task<T?> ObterPorIdAsync(Guid id) =>
        await _set.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IEnumerable<T>> ObterTodosAsync() =>
        await _set.ToListAsync();

    public async Task<T> AdicionarAsync(T entidade) { await _set.AddAsync(entidade); return entidade; }
    public Task AtualizarAsync(T entidade) { _set.Update(entidade); return Task.CompletedTask; }

    public async Task RemoverAsync(Guid id)
    {
        var e = await ObterPorIdAsync(id);
        if (e != null) { e.Ativo = false; e.AtualizadoEm = DateTime.UtcNow; }
    }
}

// ── EMPRESA ───────────────────────────────────────────────────
public class EmpresaRepositorio : Repositorio<Empresa>, IEmpresaRepositorio
{
    public EmpresaRepositorio(AppDbContext ctx) : base(ctx) { }

    public async Task<Empresa?> ObterPorCnpjAsync(string cnpj) =>
        await _ctx.Empresas.FirstOrDefaultAsync(e => e.Cnpj == cnpj.Replace(".", "").Replace("/", "").Replace("-", ""));

    public async Task<IEnumerable<Empresa>> ObterAtivasAsync() =>
        await _ctx.Empresas.Where(e => e.Ativo).OrderBy(e => e.NomeFantasia).ToListAsync();
}

// ── USUÁRIO ───────────────────────────────────────────────────
public class UsuarioRepositorio : Repositorio<Usuario>, IUsuarioRepositorio
{
    public UsuarioRepositorio(AppDbContext ctx) : base(ctx) { }

    public async Task<Usuario?> ObterPorLoginAsync(string login) =>
        await _ctx.Usuarios
            .Include(u => u.Empresa)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Login == login);

    public async Task<IEnumerable<Usuario>> ObterPorEmpresaAsync(Guid empresaId) =>
        await _ctx.Usuarios
            .Where(u => u.EmpresaId == empresaId && u.Ativo)
            .OrderBy(u => u.Nome).ToListAsync();

    public async Task<bool> LoginExisteAsync(string login, Guid? ignorarId = null) =>
        await _ctx.Usuarios.IgnoreQueryFilters()
            .AnyAsync(u => u.Login == login && (ignorarId == null || u.Id != ignorarId));
}

// ── PRODUTO ───────────────────────────────────────────────────
public class ProdutoRepositorio : Repositorio<Produto>, IProdutoRepositorio
{
    public ProdutoRepositorio(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<Produto>> ObterPorEmpresaAsync(Guid empresaId, string? busca = null)
    {
        var query = _ctx.Produtos.Where(p => p.EmpresaId == empresaId && p.Ativo);
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Nome.ToLower().Contains(busca.ToLower()));
        return await query.OrderBy(p => p.Nome).ToListAsync();
    }

    public async Task<IEnumerable<Produto>> ObterComEstoqueBaixoAsync(Guid empresaId) =>
        await _ctx.Produtos
            .Where(p => p.EmpresaId == empresaId && p.Ativo && p.EstoqueAtual <= p.EstoqueMinimo)
            .OrderBy(p => p.EstoqueAtual).ToListAsync();
}

// ── VENDA ─────────────────────────────────────────────────────
public class VendaRepositorio : Repositorio<Venda>, IVendaRepositorio
{
    public VendaRepositorio(AppDbContext ctx) : base(ctx) { }

    public async Task<Venda?> ObterComItensAsync(Guid id) =>
        await _ctx.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<Venda>> ObterPorEmpresaEPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim) =>
        await _ctx.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Usuario)
            .Where(v => v.EmpresaId == empresaId && v.DataVenda >= inicio && v.DataVenda <= fim)
            .OrderByDescending(v => v.DataVenda).ToListAsync();

    public async Task<string> GerarNumeroAsync(Guid empresaId)
    {
        var prefixo = DateTime.UtcNow.ToString("yyyyMMdd");
        var ultima = await _ctx.Vendas
            .Where(v => v.EmpresaId == empresaId && v.Numero.StartsWith(prefixo))
            .OrderByDescending(v => v.Numero).FirstOrDefaultAsync();
        int seq = 1;
        if (ultima != null && ultima.Numero.Length > 8 && int.TryParse(ultima.Numero[8..], out var n))
            seq = n + 1;
        return $"{prefixo}{seq:D4}";
    }

    public async Task<decimal> TotalVendasAsync(Guid empresaId, DateTime inicio, DateTime fim) =>
        await _ctx.Vendas
            .Where(v => v.EmpresaId == empresaId && v.Status == StatusVenda.Finalizada
                && v.DataVenda >= inicio && v.DataVenda <= fim)
            .SumAsync(v => (decimal?)v.Total) ?? 0;

    public async Task<int> QuantidadeVendasAsync(Guid empresaId, DateTime inicio, DateTime fim) =>
        await _ctx.Vendas
            .CountAsync(v => v.EmpresaId == empresaId && v.Status == StatusVenda.Finalizada
                && v.DataVenda >= inicio && v.DataVenda <= fim);

    public async Task<IEnumerable<(string Nome, int Quantidade, decimal Total)>> ProdutosMaisVendidosAsync(
        Guid empresaId, DateTime inicio, DateTime fim, int top = 10)
    {
        var result = await _ctx.ItensVenda
            .Where(i => i.Venda.EmpresaId == empresaId
                && i.Venda.Status == StatusVenda.Finalizada
                && i.Venda.DataVenda >= inicio && i.Venda.DataVenda <= fim)
            .GroupBy(i => i.ProdutoNome)
            .Select(g => new { Nome = g.Key, Quantidade = g.Sum(i => i.Quantidade), Total = g.Sum(i => i.Total) })
            .OrderByDescending(x => x.Quantidade)
            .Take(top)
            .ToListAsync();
        return result.Select(x => (x.Nome, x.Quantidade, x.Total));
    }
}

// ── MOVIMENTAÇÃO ──────────────────────────────────────────────
public class MovimentacaoRepositorio : Repositorio<MovimentacaoEstoque>, IMovimentacaoRepositorio
{
    public MovimentacaoRepositorio(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<MovimentacaoEstoque>> ObterPorProdutoAsync(Guid produtoId) =>
        await _ctx.MovimentacoesEstoque
            .Include(m => m.Produto).Include(m => m.Usuario)
            .Where(m => m.ProdutoId == produtoId)
            .OrderByDescending(m => m.CriadoEm).ToListAsync();

    public async Task<IEnumerable<MovimentacaoEstoque>> ObterPorEmpresaAsync(Guid empresaId, DateTime? inicio = null, DateTime? fim = null)
    {
        var query = _ctx.MovimentacoesEstoque
            .Include(m => m.Produto).Include(m => m.Usuario)
            .Where(m => m.EmpresaId == empresaId);
        if (inicio.HasValue) query = query.Where(m => m.CriadoEm >= inicio.Value);
        if (fim.HasValue) query = query.Where(m => m.CriadoEm <= fim.Value);
        return await query.OrderByDescending(m => m.CriadoEm).Take(200).ToListAsync();
    }
}

// ── CONFIGURAÇÃO ──────────────────────────────────────────────
public class ConfiguracaoRepositorio : IConfiguracaoRepositorio
{
    private readonly AppDbContext _ctx;
    public ConfiguracaoRepositorio(AppDbContext ctx) => _ctx = ctx;

    public async Task<ConfiguracaoEmpresa?> ObterPorEmpresaAsync(Guid empresaId) =>
        await _ctx.ConfiguracoesEmpresa.FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

    public async Task SalvarAsync(ConfiguracaoEmpresa config)
    {
        var existe = await _ctx.ConfiguracoesEmpresa.AnyAsync(c => c.Id == config.Id);
        if (existe) _ctx.ConfiguracoesEmpresa.Update(config);
        else await _ctx.ConfiguracoesEmpresa.AddAsync(config);
        await _ctx.SaveChangesAsync();
    }
}

// ── UNIT OF WORK ──────────────────────────────────────────────
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;

    public IEmpresaRepositorio Empresas { get; }
    public IUsuarioRepositorio Usuarios { get; }
    public IProdutoRepositorio Produtos { get; }
    public IVendaRepositorio Vendas { get; }
    public IMovimentacaoRepositorio Movimentacoes { get; }
    public IConfiguracaoRepositorio Configuracoes { get; }

    public UnitOfWork(AppDbContext ctx)
    {
        _ctx = ctx;
        Empresas = new EmpresaRepositorio(ctx);
        Usuarios = new UsuarioRepositorio(ctx);
        Produtos = new ProdutoRepositorio(ctx);
        Vendas = new VendaRepositorio(ctx);
        Movimentacoes = new MovimentacaoRepositorio(ctx);
        Configuracoes = new ConfiguracaoRepositorio(ctx);
    }

    public async Task<int> SalvarAsync() => await _ctx.SaveChangesAsync();
    public void Dispose() => _ctx.Dispose();
}
