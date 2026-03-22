using GestorVendas.Application.DTOs;

namespace GestorVendas.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    string HashSenha(string senha);
    bool VerificarSenha(string senha, string hash);
}

public interface IEmpresaService
{
    Task<IEnumerable<EmpresaDto>> ListarAsync();
    Task<EmpresaDto> ObterAsync(Guid id);
    Task<EmpresaDto> CriarAsync(CriarEmpresaRequest request);
    Task<EmpresaDto> AtualizarAsync(Guid id, AtualizarEmpresaRequest request);
    Task DesativarAsync(Guid id);
}

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioDto>> ListarPorEmpresaAsync(Guid empresaId);
    Task<UsuarioDto> ObterAsync(Guid id);
    Task<UsuarioDto> CriarAsync(CriarUsuarioRequest request, Guid empresaCriadorId);
    Task<UsuarioDto> AtualizarAsync(Guid id, AtualizarUsuarioRequest request);
    Task DesativarAsync(Guid id);
}

public interface IProdutoService
{
    Task<IEnumerable<ProdutoDto>> ListarAsync(Guid empresaId, string? busca = null);
    Task<ProdutoDto> ObterAsync(Guid id, Guid empresaId);
    Task<ProdutoDto> CriarAsync(CriarProdutoRequest request, Guid empresaId, Guid usuarioId);
    Task<ProdutoDto> AtualizarAsync(Guid id, AtualizarProdutoRequest request, Guid empresaId);
    Task DesativarAsync(Guid id, Guid empresaId);
    Task EntradaEstoqueAsync(EntradaEstoqueRequest request, Guid empresaId, Guid usuarioId);
    Task AjusteEstoqueAsync(AjusteEstoqueRequest request, Guid empresaId, Guid usuarioId);
    Task<IEnumerable<MovimentacaoDto>> ListarMovimentacoesAsync(Guid empresaId, Guid? produtoId = null);
    Task<IEnumerable<ProdutoEstoqueBaixoDto>> ListarEstoqueBaixoAsync(Guid empresaId);
}

public interface IVendaService
{
    Task<VendaDto> IniciarAsync(Guid empresaId, Guid usuarioId);
    Task<VendaDto> AdicionarItemAsync(Guid vendaId, AdicionarItemRequest request, Guid empresaId);
    Task<VendaDto> RemoverItemAsync(Guid vendaId, Guid itemId, Guid empresaId);
    Task<VendaDto> FinalizarAsync(Guid vendaId, FinalizarVendaRequest request, Guid empresaId);
    Task<VendaDto> CancelarAsync(Guid vendaId, string motivo, Guid empresaId);
    Task<VendaDto> ObterAsync(Guid id, Guid empresaId);
    Task<IEnumerable<VendaResumoDto>> ListarAsync(Guid empresaId, DateTime? inicio = null, DateTime? fim = null);
}

public interface IDashboardService
{
    Task<DashboardDto> ObterAsync(Guid empresaId);
}

public interface IRelatorioService
{
    Task<RelatorioVendasDto> VendasPorPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim);
}

public interface IConfiguracaoService
{
    Task<ConfiguracaoDto> ObterAsync(Guid empresaId);
    Task<ConfiguracaoDto> AtualizarAsync(Guid empresaId, AtualizarConfiguracaoRequest request);
}
