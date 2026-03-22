namespace GestorVendas.Domain.Enums;

public enum PerfilUsuario
{
    Admin = 1,
    Gerente = 2,
    Operador = 3
}

public enum FormaPagamento
{
    Dinheiro = 1,
    CartaoDebito = 2,
    CartaoCredito = 3,
    Pix = 4
}

public enum StatusVenda
{
    Aberta = 1,
    Finalizada = 2,
    Cancelada = 3
}

public enum StatusEstoque
{
    Normal = 1,
    Baixo = 2,
    Critico = 3,
    Esgotado = 4
}
