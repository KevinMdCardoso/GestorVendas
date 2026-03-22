using GestorVendas.Domain.Enums;

namespace GestorVendas.Domain.Entities;

public abstract class EntidadeBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;
}

// ── EMPRESA ──────────────────────────────────────────────────
public class Empresa : EntidadeBase
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
    public ICollection<ConfiguracaoEmpresa> Configuracoes { get; set; } = new List<ConfiguracaoEmpresa>();
}

// ── CONFIGURAÇÃO DA EMPRESA ───────────────────────────────────
public class ConfiguracaoEmpresa : EntidadeBase
{
    public Guid EmpresaId { get; set; }
    public decimal DescontoMaximoPermitido { get; set; } = 0; // % máximo que gerente pode dar
    public int EstoqueAlertaQuantidade { get; set; } = 10;    // alerta quando <= X unidades

    public Empresa Empresa { get; set; } = null!;
}

// ── USUÁRIO ───────────────────────────────────────────────────
public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public Guid? EmpresaId { get; set; } // null = Admin do sistema

    public Empresa? Empresa { get; set; }
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}

// ── PRODUTO ───────────────────────────────────────────────────
public class Produto : EntidadeBase
{
    public Guid EmpresaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal PrecoVenda { get; set; }
    public int EstoqueAtual { get; set; }
    public int EstoqueMinimo { get; set; } = 5;

    public Empresa Empresa { get; set; } = null!;
    public ICollection<ItemVenda> ItensVenda { get; set; } = new List<ItemVenda>();
    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = new List<MovimentacaoEstoque>();

    public StatusEstoque StatusEstoque =>
        EstoqueAtual <= 0 ? StatusEstoque.Esgotado :
        EstoqueAtual <= EstoqueMinimo / 2 ? StatusEstoque.Critico :
        EstoqueAtual <= EstoqueMinimo ? StatusEstoque.Baixo :
        StatusEstoque.Normal;
}

// ── MOVIMENTAÇÃO DE ESTOQUE ───────────────────────────────────
public class MovimentacaoEstoque : EntidadeBase
{
    public Guid ProdutoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public int Quantidade { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Entrada" | "Saida" | "Ajuste"
    public string? Observacao { get; set; }
    public int EstoqueAnterior { get; set; }
    public int EstoquePosterior { get; set; }

    public Produto Produto { get; set; } = null!;
    public Usuario? Usuario { get; set; }
}

// ── VENDA ─────────────────────────────────────────────────────
public class Venda : EntidadeBase
{
    public Guid EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal Total { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public decimal? ValorRecebido { get; set; }
    public decimal? Troco { get; set; }
    public StatusVenda Status { get; set; } = StatusVenda.Aberta;
    public string? Observacao { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Usuario? Usuario { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();

    // Placeholder NF-e (módulo futuro)
    public string? NFeChaveAcesso { get; set; }
    public string? NFeStatus { get; set; }
    public DateTime? NFeEmitidaEm { get; set; }
}

// ── ITEM DE VENDA ─────────────────────────────────────────────
public class ItemVenda : EntidadeBase
{
    public Guid VendaId { get; set; }
    public Guid ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty; // snapshot
    public decimal PrecoUnitario { get; set; }              // snapshot
    public int Quantidade { get; set; }
    public decimal Total { get; set; }

    public Venda Venda { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
}
