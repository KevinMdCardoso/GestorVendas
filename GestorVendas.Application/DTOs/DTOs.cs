using GestorVendas.Domain.Enums;

namespace GestorVendas.Application.DTOs;

// ── AUTH ──────────────────────────────────────────────────────
public record LoginRequest(string Login, string Senha);

public record LoginResponse(
    string Token,
    string Nome,
    PerfilUsuario Perfil,
    Guid? EmpresaId,
    string NomeEmpresaOuSistema,
    DateTime Expiracao,
    Guid UsuarioId = default
);

// ── EMPRESA ───────────────────────────────────────────────────
public record EmpresaDto(
    Guid Id,
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string? Telefone,
    string? Email,
    bool Ativo,
    int TotalUsuarios,
    int TotalProdutos
);

public record CriarEmpresaRequest(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string? Telefone,
    string? Email,
    // Gerente inicial
    string GerenteNome,
    string GerenteLogin,
    string GerентеSenha
);

public record AtualizarEmpresaRequest(
    string RazaoSocial,
    string NomeFantasia,
    string? Telefone,
    string? Email
);

// ── USUÁRIO ───────────────────────────────────────────────────
public record UsuarioDto(
    Guid Id,
    string Nome,
    string Login,
    PerfilUsuario Perfil,
    Guid? EmpresaId,
    string? NomeEmpresa,
    bool Ativo
);

public record CriarUsuarioRequest(
    string Nome,
    string Login,
    string Senha,
    PerfilUsuario Perfil,
    Guid? EmpresaId
);

public record AtualizarUsuarioRequest(
    string Nome,
    string? NovaSenha
);

// ── PRODUTO ───────────────────────────────────────────────────
public record ProdutoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    int EstoqueAtual,
    int EstoqueMinimo,
    string StatusEstoque
);

public record CriarProdutoRequest(
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    int EstoqueInicial,
    int EstoqueMinimo
);

public record AtualizarProdutoRequest(
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    int EstoqueMinimo
);

public record EntradaEstoqueRequest(
    Guid ProdutoId,
    int Quantidade,
    string? Observacao
);

public record AjusteEstoqueRequest(
    Guid ProdutoId,
    int NovaQuantidade,
    string? Observacao
);

public record MovimentacaoDto(
    Guid Id,
    string ProdutoNome,
    string Tipo,
    int Quantidade,
    int EstoqueAnterior,
    int EstoquePosterior,
    string? Observacao,
    string? UsuarioNome,
    DateTime CriadoEm
);

// ── VENDA / PDV ───────────────────────────────────────────────
public record IniciarVendaRequest(Guid UsuarioId);

public record AdicionarItemRequest(
    Guid ProdutoId,
    int Quantidade
);

public record FinalizarVendaRequest(
    FormaPagamento FormaPagamento,
    decimal? ValorRecebido,
    decimal PercentualDesconto, // 0 se operador, preenchido pelo gerente
    string? Observacao
);

public record ItemVendaDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Total
);

public record VendaDto(
    Guid Id,
    string Numero,
    DateTime DataVenda,
    decimal SubTotal,
    decimal PercentualDesconto,
    decimal ValorDesconto,
    decimal Total,
    string FormaPagamento,
    decimal? ValorRecebido,
    decimal? Troco,
    string Status,
    string? UsuarioNome,
    List<ItemVendaDto> Itens
);

public record VendaResumoDto(
    Guid Id,
    string Numero,
    DateTime DataVenda,
    decimal Total,
    string FormaPagamento,
    string Status,
    int QtdItens,
    string? UsuarioNome
);

// ── DASHBOARD ─────────────────────────────────────────────────
public record DashboardDto(
    decimal VendasHoje,
    int PedidosHoje,
    decimal VendasSemana,
    int PedidosSemana,
    decimal VendasMes,
    int PedidosMes,
    decimal TicketMedioHoje,
    int AlertasEstoque,
    List<VendaResumoDto> UltimasVendas,
    List<ProdutoEstoqueBaixoDto> EstoqueBaixo,
    List<ProdutoMaisVendidoDto> MaisVendidos
);

public record ProdutoEstoqueBaixoDto(
    Guid Id,
    string Nome,
    int EstoqueAtual,
    int EstoqueMinimo,
    string Status
);

public record ProdutoMaisVendidoDto(
    string Nome,
    int QuantidadeVendida,
    decimal ReceitaTotal
);

// ── RELATÓRIO ─────────────────────────────────────────────────
public record RelatorioVendasDto(
    DateTime Inicio,
    DateTime Fim,
    decimal TotalBruto,
    decimal TotalDescontos,
    decimal TotalLiquido,
    int QuantidadeVendas,
    decimal TicketMedio,
    List<VendaPorDiaDto> VendasPorDia,
    List<ProdutoMaisVendidoDto> ProdutosMaisVendidos
);

public record VendaPorDiaDto(
    DateTime Data,
    decimal Total,
    int Quantidade
);

// ── CONFIGURAÇÃO ──────────────────────────────────────────────
public record ConfiguracaoDto(
    Guid EmpresaId,
    decimal DescontoMaximoPermitido,
    int EstoqueAlertaQuantidade
);

public record AtualizarConfiguracaoRequest(
    decimal DescontoMaximoPermitido,
    int EstoqueAlertaQuantidade
);
