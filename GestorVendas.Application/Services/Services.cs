using GestorVendas.Application.DTOs;
using GestorVendas.Application.Interfaces;
using GestorVendas.Domain.Entities;
using GestorVendas.Domain.Enums;
using GestorVendas.Domain.Interfaces;

namespace GestorVendas.Application.Services;

// ── PRODUTO SERVICE ───────────────────────────────────────────
public class ProdutoService : IProdutoService
{
    private readonly IUnitOfWork _uow;
    public ProdutoService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<ProdutoDto>> ListarAsync(Guid empresaId, string? busca = null)
    {
        var lista = await _uow.Produtos.ObterPorEmpresaAsync(empresaId, busca);
        return lista.Select(ToDto);
    }

    public async Task<ProdutoDto> ObterAsync(Guid id, Guid empresaId)
    {
        var p = await _uow.Produtos.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        if (p.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        return ToDto(p);
    }

    public async Task<ProdutoDto> CriarAsync(CriarProdutoRequest req, Guid empresaId, Guid usuarioId)
    {
        var produto = new Produto
        {
            EmpresaId = empresaId,
            Nome = req.Nome,
            Descricao = req.Descricao,
            PrecoVenda = req.PrecoVenda,
            EstoqueAtual = req.EstoqueInicial,
            EstoqueMinimo = req.EstoqueMinimo
        };
        await _uow.Produtos.AdicionarAsync(produto);

        if (req.EstoqueInicial > 0)
        {
            await _uow.Movimentacoes.AdicionarAsync(new MovimentacaoEstoque
            {
                ProdutoId = produto.Id,
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                Quantidade = req.EstoqueInicial,
                Tipo = "Entrada",
                Observacao = "Estoque inicial",
                EstoqueAnterior = 0,
                EstoquePosterior = req.EstoqueInicial
            });
        }

        await _uow.SalvarAsync();
        return ToDto(produto);
    }

    public async Task<ProdutoDto> AtualizarAsync(Guid id, AtualizarProdutoRequest req, Guid empresaId)
    {
        var p = await _uow.Produtos.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        if (p.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");

        p.Nome = req.Nome;
        p.Descricao = req.Descricao;
        p.PrecoVenda = req.PrecoVenda;
        p.EstoqueMinimo = req.EstoqueMinimo;
        p.AtualizadoEm = DateTime.UtcNow;

        await _uow.Produtos.AtualizarAsync(p);
        await _uow.SalvarAsync();
        return ToDto(p);
    }

    public async Task DesativarAsync(Guid id, Guid empresaId)
    {
        var p = await _uow.Produtos.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        if (p.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        p.Ativo = false;
        p.AtualizadoEm = DateTime.UtcNow;
        await _uow.Produtos.AtualizarAsync(p);
        await _uow.SalvarAsync();
    }

    public async Task EntradaEstoqueAsync(EntradaEstoqueRequest req, Guid empresaId, Guid usuarioId)
    {
        var p = await _uow.Produtos.ObterPorIdAsync(req.ProdutoId)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        if (p.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");

        var anterior = p.EstoqueAtual;
        p.EstoqueAtual += req.Quantidade;
        p.AtualizadoEm = DateTime.UtcNow;

        await _uow.Produtos.AtualizarAsync(p);
        await _uow.Movimentacoes.AdicionarAsync(new MovimentacaoEstoque
        {
            ProdutoId = p.Id,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Quantidade = req.Quantidade,
            Tipo = "Entrada",
            Observacao = req.Observacao,
            EstoqueAnterior = anterior,
            EstoquePosterior = p.EstoqueAtual
        });
        await _uow.SalvarAsync();
    }

    public async Task AjusteEstoqueAsync(AjusteEstoqueRequest req, Guid empresaId, Guid usuarioId)
    {
        var p = await _uow.Produtos.ObterPorIdAsync(req.ProdutoId)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        if (p.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");

        var anterior = p.EstoqueAtual;
        p.EstoqueAtual = req.NovaQuantidade;
        p.AtualizadoEm = DateTime.UtcNow;

        await _uow.Produtos.AtualizarAsync(p);
        await _uow.Movimentacoes.AdicionarAsync(new MovimentacaoEstoque
        {
            ProdutoId = p.Id,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Quantidade = Math.Abs(req.NovaQuantidade - anterior),
            Tipo = "Ajuste",
            Observacao = req.Observacao ?? "Ajuste manual",
            EstoqueAnterior = anterior,
            EstoquePosterior = req.NovaQuantidade
        });
        await _uow.SalvarAsync();
    }

    public async Task<IEnumerable<MovimentacaoDto>> ListarMovimentacoesAsync(Guid empresaId, Guid? produtoId = null)
    {
        var lista = produtoId.HasValue
            ? await _uow.Movimentacoes.ObterPorProdutoAsync(produtoId.Value)
            : await _uow.Movimentacoes.ObterPorEmpresaAsync(empresaId);
        return lista.Select(m => new MovimentacaoDto(
            m.Id, m.Produto?.Nome ?? "", m.Tipo, m.Quantidade,
            m.EstoqueAnterior, m.EstoquePosterior,
            m.Observacao, m.Usuario?.Nome, m.CriadoEm));
    }

    public async Task<IEnumerable<ProdutoEstoqueBaixoDto>> ListarEstoqueBaixoAsync(Guid empresaId)
    {
        var lista = await _uow.Produtos.ObterComEstoqueBaixoAsync(empresaId);
        return lista.Select(p => new ProdutoEstoqueBaixoDto(
            p.Id, p.Nome, p.EstoqueAtual, p.EstoqueMinimo, p.StatusEstoque.ToString()));
    }

    private static ProdutoDto ToDto(Produto p) => new(
        p.Id, p.Nome, p.Descricao, p.PrecoVenda,
        p.EstoqueAtual, p.EstoqueMinimo, p.StatusEstoque.ToString());
}

// ── VENDA SERVICE ─────────────────────────────────────────────
public class VendaService : IVendaService
{
    private readonly IUnitOfWork _uow;
    public VendaService(IUnitOfWork uow) => _uow = uow;

    public async Task<VendaDto> IniciarAsync(Guid empresaId, Guid usuarioId)
    {
        var numero = await _uow.Vendas.GerarNumeroAsync(empresaId);
        var venda = new Venda
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Numero = numero,
            Status = StatusVenda.Aberta
        };
        await _uow.Vendas.AdicionarAsync(venda);
        await _uow.SalvarAsync();
        return ToDto(venda);
    }

    public async Task<VendaDto> AdicionarItemAsync(Guid vendaId, AdicionarItemRequest req, Guid empresaId)
    {
        var venda = await _uow.Vendas.ObterComItensAsync(vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");
        if (venda.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        if (venda.Status != StatusVenda.Aberta) throw new InvalidOperationException("Venda não está aberta.");

        var produto = await _uow.Produtos.ObterPorIdAsync(req.ProdutoId)
            ?? throw new KeyNotFoundException("Produto não encontrado.");
        if (produto.EmpresaId != empresaId) throw new UnauthorizedAccessException("Produto não pertence a esta empresa.");
        if (produto.EstoqueAtual < req.Quantidade) throw new InvalidOperationException($"Estoque insuficiente. Disponível: {produto.EstoqueAtual}");

        var itemExistente = venda.Itens.FirstOrDefault(i => i.ProdutoId == req.ProdutoId);
        if (itemExistente != null)
        {
            var novaQtd = itemExistente.Quantidade + req.Quantidade;
            if (produto.EstoqueAtual < novaQtd)
                throw new InvalidOperationException($"Estoque insuficiente. Disponível: {produto.EstoqueAtual}");
            itemExistente.Total = itemExistente.Quantidade * itemExistente.PrecoUnitario;
        }
        else
        {
            var novoItem = new ItemVenda
            {
                VendaId = vendaId,
                ProdutoId = req.ProdutoId,
                ProdutoNome = produto.Nome,
                PrecoUnitario = produto.PrecoVenda,
                Quantidade = req.Quantidade,
                Total = req.Quantidade * produto.PrecoVenda
            };
            await _uow.ItensVenda.AdicionarAsync(novoItem);
            venda.Itens.Add(novoItem);
        }

        RecalcularTotais(venda);
        await _uow.Vendas.AtualizarAsync(venda);
        await _uow.SalvarAsync();
        return ToDto(venda);
    }

    public async Task<VendaDto> RemoverItemAsync(Guid vendaId, Guid itemId, Guid empresaId)
    {
        var venda = await _uow.Vendas.ObterComItensAsync(vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");
        if (venda.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        if (venda.Status != StatusVenda.Aberta) throw new InvalidOperationException("Venda não está aberta.");

        var item = venda.Itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Item não encontrado.");
        venda.Itens.Remove(item);

        RecalcularTotais(venda);
        await _uow.Vendas.AtualizarAsync(venda);
        await _uow.SalvarAsync();
        return ToDto(venda);
    }

    public async Task<VendaDto> FinalizarAsync(Guid vendaId, FinalizarVendaRequest req, Guid empresaId)
    {
        var venda = await _uow.Vendas.ObterComItensAsync(vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");
        if (venda.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        if (venda.Status != StatusVenda.Aberta) throw new InvalidOperationException("Venda não está aberta.");
        if (!venda.Itens.Any()) throw new InvalidOperationException("Adicione ao menos um item.");

        // Valida desconto máximo
        if (req.PercentualDesconto > 0)
        {
            var config = await _uow.Configuracoes.ObterPorEmpresaAsync(empresaId);
            var maxDesconto = config?.DescontoMaximoPermitido ?? 0;
            if (req.PercentualDesconto > maxDesconto)
                throw new InvalidOperationException($"Desconto máximo permitido: {maxDesconto}%");
        }

        venda.PercentualDesconto = req.PercentualDesconto;
        venda.FormaPagamento = req.FormaPagamento;
        venda.ValorRecebido = req.ValorRecebido;
        venda.Observacao = req.Observacao;
        venda.Status = StatusVenda.Finalizada;
        venda.DataVenda = DateTime.UtcNow;
        RecalcularTotais(venda);
        venda.Troco = req.FormaPagamento == FormaPagamento.Dinheiro && req.ValorRecebido.HasValue
            ? req.ValorRecebido.Value - venda.Total
            : null;

        // Baixa estoque
        foreach (var item in venda.Itens)
        {
            var produto = await _uow.Produtos.ObterPorIdAsync(item.ProdutoId);
            if (produto != null)
            {
                var anterior = produto.EstoqueAtual;
                produto.EstoqueAtual -= item.Quantidade;
                if (produto.EstoqueAtual < 0) produto.EstoqueAtual = 0;
                produto.AtualizadoEm = DateTime.UtcNow;
                await _uow.Produtos.AtualizarAsync(produto);

                await _uow.Movimentacoes.AdicionarAsync(new MovimentacaoEstoque
                {
                    ProdutoId = produto.Id,
                    EmpresaId = empresaId,
                    UsuarioId = venda.UsuarioId,
                    Quantidade = item.Quantidade,
                    Tipo = "Saida",
                    Observacao = $"Venda {venda.Numero}",
                    EstoqueAnterior = anterior,
                    EstoquePosterior = produto.EstoqueAtual
                });
            }
        }

        await _uow.Vendas.AtualizarAsync(venda);
        await _uow.SalvarAsync();
        return ToDto(venda);
    }

    public async Task<VendaDto> CancelarAsync(Guid vendaId, string motivo, Guid empresaId)
    {
        var venda = await _uow.Vendas.ObterComItensAsync(vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");
        if (venda.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        if (venda.Status == StatusVenda.Cancelada) throw new InvalidOperationException("Venda já cancelada.");

        // Devolve estoque se já estava finalizada
        if (venda.Status == StatusVenda.Finalizada)
        {
            foreach (var item in venda.Itens)
            {
                var produto = await _uow.Produtos.ObterPorIdAsync(item.ProdutoId);
                if (produto != null)
                {
                    var anterior = produto.EstoqueAtual;
                    produto.EstoqueAtual += item.Quantidade;
                    produto.AtualizadoEm = DateTime.UtcNow;
                    await _uow.Produtos.AtualizarAsync(produto);
                    await _uow.Movimentacoes.AdicionarAsync(new MovimentacaoEstoque
                    {
                        ProdutoId = produto.Id,
                        EmpresaId = empresaId,
                        UsuarioId = venda.UsuarioId,
                        Quantidade = item.Quantidade,
                        Tipo = "Entrada",
                        Observacao = $"Cancelamento venda {venda.Numero}: {motivo}",
                        EstoqueAnterior = anterior,
                        EstoquePosterior = produto.EstoqueAtual
                    });
                }
            }
        }

        venda.Status = StatusVenda.Cancelada;
        venda.Observacao = $"CANCELADA: {motivo}";
        venda.AtualizadoEm = DateTime.UtcNow;
        await _uow.Vendas.AtualizarAsync(venda);
        await _uow.SalvarAsync();
        return ToDto(venda);
    }

    public async Task<VendaDto> ObterAsync(Guid id, Guid empresaId)
    {
        var venda = await _uow.Vendas.ObterComItensAsync(id)
            ?? throw new KeyNotFoundException("Venda não encontrada.");
        if (venda.EmpresaId != empresaId) throw new UnauthorizedAccessException("Acesso negado.");
        return ToDto(venda);
    }

    public async Task<IEnumerable<VendaResumoDto>> ListarAsync(Guid empresaId, DateTime? inicio = null, DateTime? fim = null)
    {
        var ini = DateTime.SpecifyKind(inicio ?? DateTime.UtcNow.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(fim ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        var lista = await _uow.Vendas.ObterPorEmpresaEPeriodoAsync(empresaId, ini, end);
        return lista.Select(v => new VendaResumoDto(
            v.Id, v.Numero, v.DataVenda, v.Total,
            v.FormaPagamento.ToString(), v.Status.ToString(),
            v.Itens.Count, v.Usuario?.Nome));
    }

    private static void RecalcularTotais(Venda v)
    {
        v.SubTotal = v.Itens.Sum(i => i.Total);
        v.ValorDesconto = Math.Round(v.SubTotal * (v.PercentualDesconto / 100), 2);
        v.Total = v.SubTotal - v.ValorDesconto;
    }

    private static VendaDto ToDto(Venda v) => new(
        v.Id, v.Numero, v.DataVenda, v.SubTotal, v.PercentualDesconto,
        v.ValorDesconto, v.Total, v.FormaPagamento.ToString(),
        v.ValorRecebido, v.Troco, v.Status.ToString(), v.Usuario?.Nome,
        v.Itens.Select(i => new ItemVendaDto(
            i.Id, i.ProdutoId, i.ProdutoNome, i.PrecoUnitario, i.Quantidade, i.Total))
        .ToList());
}

// ── DASHBOARD SERVICE ─────────────────────────────────────────
public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    public DashboardService(IUnitOfWork uow) => _uow = uow;

    public async Task<DashboardDto> ObterAsync(Guid empresaId)
    {
        var agora = DateTime.UtcNow;
        var hoje   = DateTime.SpecifyKind(agora.Date, DateTimeKind.Utc);
        var semana = hoje.AddDays(-7);
        var mes    = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var vhoje = await _uow.Vendas.TotalVendasAsync(empresaId, hoje, agora);
        var phoje = await _uow.Vendas.QuantidadeVendasAsync(empresaId, hoje, agora);
        var vsemana = await _uow.Vendas.TotalVendasAsync(empresaId, semana, agora);
        var psemana = await _uow.Vendas.QuantidadeVendasAsync(empresaId, semana, agora);
        var vmes = await _uow.Vendas.TotalVendasAsync(empresaId, mes, agora);
        var pmes = await _uow.Vendas.QuantidadeVendasAsync(empresaId, mes, agora);

        var ticketHoje = phoje > 0 ? Math.Round(vhoje / phoje, 2) : 0;

        var estoqueBaixo = await _uow.Produtos.ObterComEstoqueBaixoAsync(empresaId);
        var ultimasVendas = await _uow.Vendas.ObterPorEmpresaEPeriodoAsync(empresaId, hoje, agora);
        var maisVendidos = await _uow.Vendas.ProdutosMaisVendidosAsync(empresaId, mes, agora, 5);

        return new DashboardDto(
            vhoje, phoje, vsemana, psemana, vmes, pmes, ticketHoje,
            estoqueBaixo.Count(),
            ultimasVendas.OrderByDescending(v => v.DataVenda).Take(8)
                .Select(v => new VendaResumoDto(v.Id, v.Numero, v.DataVenda, v.Total,
                    v.FormaPagamento.ToString(), v.Status.ToString(), v.Itens.Count, v.Usuario?.Nome))
                .ToList(),
            estoqueBaixo.Select(p => new ProdutoEstoqueBaixoDto(
                p.Id, p.Nome, p.EstoqueAtual, p.EstoqueMinimo, p.StatusEstoque.ToString()))
                .ToList(),
            maisVendidos.Select(x => new ProdutoMaisVendidoDto(x.Nome, x.Quantidade, x.Total))
                .ToList());
    }
}

// ── RELATÓRIO SERVICE ─────────────────────────────────────────
public class RelatorioService : IRelatorioService
{
    private readonly IUnitOfWork _uow;
    public RelatorioService(IUnitOfWork uow) => _uow = uow;

    public async Task<RelatorioVendasDto> VendasPorPeriodoAsync(Guid empresaId, DateTime inicio, DateTime fim)
    {
        inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc);
        fim    = DateTime.SpecifyKind(fim,    DateTimeKind.Utc);
        var vendas = (await _uow.Vendas.ObterPorEmpresaEPeriodoAsync(empresaId, inicio, fim))
            .Where(v => v.Status == StatusVenda.Finalizada).ToList();

        var totalBruto = vendas.Sum(v => v.SubTotal);
        var totalDesc = vendas.Sum(v => v.ValorDesconto);
        var totalLiq = vendas.Sum(v => v.Total);
        var qtd = vendas.Count;
        var ticket = qtd > 0 ? Math.Round(totalLiq / qtd, 2) : 0;

        var porDia = vendas.GroupBy(v => v.DataVenda.Date)
            .Select(g => new VendaPorDiaDto(g.Key, g.Sum(v => v.Total), g.Count()))
            .OrderBy(x => x.Data).ToList();

        var maisVendidos = (await _uow.Vendas.ProdutosMaisVendidosAsync(empresaId, inicio, fim, 10))
            .Select(x => new ProdutoMaisVendidoDto(x.Nome, x.Quantidade, x.Total)).ToList();

        return new RelatorioVendasDto(inicio, fim, totalBruto, totalDesc, totalLiq, qtd, ticket, porDia, maisVendidos);
    }
}

// ── CONFIGURAÇÃO SERVICE ──────────────────────────────────────
public class ConfiguracaoService : IConfiguracaoService
{
    private readonly IUnitOfWork _uow;
    public ConfiguracaoService(IUnitOfWork uow) => _uow = uow;

    public async Task<ConfiguracaoDto> ObterAsync(Guid empresaId)
    {
        var c = await _uow.Configuracoes.ObterPorEmpresaAsync(empresaId)
            ?? new Domain.Entities.ConfiguracaoEmpresa { EmpresaId = empresaId };
        return new ConfiguracaoDto(c.EmpresaId, c.DescontoMaximoPermitido, c.EstoqueAlertaQuantidade);
    }

    public async Task<ConfiguracaoDto> AtualizarAsync(Guid empresaId, AtualizarConfiguracaoRequest req)
    {
        var c = await _uow.Configuracoes.ObterPorEmpresaAsync(empresaId)
            ?? new Domain.Entities.ConfiguracaoEmpresa { EmpresaId = empresaId };
        c.DescontoMaximoPermitido = req.DescontoMaximoPermitido;
        c.EstoqueAlertaQuantidade = req.EstoqueAlertaQuantidade;
        await _uow.Configuracoes.SalvarAsync(c);
        return new ConfiguracaoDto(c.EmpresaId, c.DescontoMaximoPermitido, c.EstoqueAlertaQuantidade);
    }
}
