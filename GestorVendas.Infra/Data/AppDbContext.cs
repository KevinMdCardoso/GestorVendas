using GestorVendas.Domain.Entities;
using GestorVendas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestorVendas.Infra.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<ConfiguracaoEmpresa> ConfiguracoesEmpresa => Set<ConfiguracaoEmpresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Empresa ──────────────────────────────────────────
        mb.Entity<Empresa>(e => {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Cnpj).IsUnique();
            e.Property(x => x.RazaoSocial).HasMaxLength(200).IsRequired();
            e.Property(x => x.NomeFantasia).HasMaxLength(200).IsRequired();
            e.Property(x => x.Cnpj).HasMaxLength(20).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasQueryFilter(x => x.Ativo);
        });

        // ── ConfiguracaoEmpresa ───────────────────────────────
        mb.Entity<ConfiguracaoEmpresa>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.DescontoMaximoPermitido).HasPrecision(5, 2);
            e.HasOne(x => x.Empresa).WithMany(emp => emp.Configuracoes)
                .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Usuário ───────────────────────────────────────────
        mb.Entity<Usuario>(e => {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Login).IsUnique();
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Login).HasMaxLength(100).IsRequired();
            e.Property(x => x.SenhaHash).IsRequired();
            e.HasOne(x => x.Empresa).WithMany(emp => emp.Usuarios)
                .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.Ativo);
        });

        // ── Produto ───────────────────────────────────────────
        mb.Entity<Produto>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.PrecoVenda).HasPrecision(18, 2);
            e.HasOne(x => x.Empresa).WithMany(emp => emp.Produtos)
                .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.Ativo);
        });

        // ── MovimentacaoEstoque ───────────────────────────────
        mb.Entity<MovimentacaoEstoque>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Produto).WithMany(p => p.Movimentacoes)
                .HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany()
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Venda ─────────────────────────────────────────────
        mb.Entity<Venda>(e => {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmpresaId, x.Numero }).IsUnique();
            e.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.PercentualDesconto).HasPrecision(5, 2);
            e.Property(x => x.ValorDesconto).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.ValorRecebido).HasPrecision(18, 2);
            e.Property(x => x.Troco).HasPrecision(18, 2);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasOne(x => x.Empresa).WithMany(emp => emp.Vendas)
                .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany(u => u.Vendas)
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── ItemVenda ─────────────────────────────────────────
        mb.Entity<ItemVenda>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProdutoNome).HasMaxLength(200).IsRequired();
            e.Property(x => x.PrecoUnitario).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasOne(x => x.Venda).WithMany(v => v.Itens)
                .HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany(p => p.ItensVenda)
                .HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Seed: Admin do sistema ────────────────────────────
        // Senha: 99441986 (hash gerado com PBKDF2)
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        mb.Entity<Usuario>().HasData(new
        {
            Id = adminId,
            Nome = "Administrador",
            Login = "admin",
            // hash de "99441986" — gerado uma vez e fixado aqui
            SenhaHash = "zqycMHtjjh70OptBcCnOzw==:dzDnlFmaMH+oE9yUC0MeUN9kGJbCt8PruB8/Xjcpfao=",
            Perfil = PerfilUsuario.Admin,
            EmpresaId = (Guid?)null,
            Ativo = true,
            CriadoEm = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            AtualizadoEm = (DateTime?)null
        });
    }
}
