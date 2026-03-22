using GestorVendas.Domain.Entities;

namespace GestorVendas.Domain.Interfaces;

public interface IRepositorio<T> where T : EntidadeBase
{
    Task<T?> ObterPorIdAsync(Guid id);
    Task<IEnumerable<T>> ObterTodosAsync();
    Task<T> AdicionarAsync(T entidade);
    Task AtualizarAsync(T entidade);
    Task RemoverAsync(Guid id);
}

public interface IEmpresaRepositorio : IRepositorio<Empresa>
{
    Task<Empresa?> ObterPorCnpjAsync(string cnpj);
    Task<IEnumerable<Empresa>> ObterAtivasAsync();
}

public interface IUsuarioRepositorio : IRepositorio<Usuario>
{
    Task<Usuario?> ObterPorLoginAsync(string login);
    Task<IEnumerable<Usuario>> ObterPorEmpresaAsync(Guid empresaId);
    Task<bool> LoginExisteAsync(string login, Guid? ignorarId = null);
}

public interface IProdutoRepositorio : IRepositorio<Produto>
{
    Task<IEnumerable<Produto>> ObterPorEmpresaAsync(Guid empresaId, string? busca = null);
    Task<IEnumerable<Produto>> ObterComEstoqueBaixoAsync(Guid empresaId);
}

public interface IVendaRepositorio : IRepositorio<Venda>
{
    Task<Venda?> ObterComItensAsync(Guid id);
    Task<IEnumerable<Venda>> ObterPorEmpresaEPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim);
    Task<string> GerarNumeroAsync(Guid empresaId);
    Task<decimal> TotalVendasAsync(Guid empresaId, DateTime inicio, DateTime fim);
    Task<int> QuantidadeVendasAsync(Guid empresaId, DateTime inicio, DateTime fim);
    Task<IEnumerable<(string Nome, int Quantidade, decimal Total)>> ProdutosMaisVendidosAsync(Guid empresaId, DateTime inicio, DateTime fim, int top = 10);
}

public interface IMovimentacaoRepositorio : IRepositorio<MovimentacaoEstoque>
{
    Task<IEnumerable<MovimentacaoEstoque>> ObterPorProdutoAsync(Guid produtoId);
    Task<IEnumerable<MovimentacaoEstoque>> ObterPorEmpresaAsync(Guid empresaId, DateTime? inicio = null, DateTime? fim = null);
}

public interface IConfiguracaoRepositorio
{
    Task<ConfiguracaoEmpresa?> ObterPorEmpresaAsync(Guid empresaId);
    Task SalvarAsync(ConfiguracaoEmpresa config);
}

public interface IUnitOfWork : IDisposable
{
    IEmpresaRepositorio Empresas { get; }
    IUsuarioRepositorio Usuarios { get; }
    IProdutoRepositorio Produtos { get; }
    IVendaRepositorio Vendas { get; }
    IMovimentacaoRepositorio Movimentacoes { get; }
    IConfiguracaoRepositorio Configuracoes { get; }
    Task<int> SalvarAsync();
}
