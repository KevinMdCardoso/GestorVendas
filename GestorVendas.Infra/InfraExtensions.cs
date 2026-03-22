using GestorVendas.Application.Interfaces;
using GestorVendas.Application.Services;
using GestorVendas.Domain.Interfaces;
using GestorVendas.Infra.Data;
using GestorVendas.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorVendas.Infra;

public static class InfraExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("Default") ?? throw new InvalidOperationException("String de conexão 'Default' não encontrada."),
                npgsql => npgsql.MigrationsAssembly("GestorVendas.Infra")
            )
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IProdutoService, ProdutoService>();
        services.AddScoped<IVendaService, VendaService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRelatorioService, RelatorioService>();
        services.AddScoped<IConfiguracaoService, ConfiguracaoService>();

        return services;
    }

    public static async Task AplicarMigracoesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Verificar e atualizar senha do admin se necessário
        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Login == "admin");
        if (admin != null && admin.SenhaHash != "zqycMHtjjh70OptBcCnOzw==:dzDnlFmaMH+oE9yUC0MeUN9kGJbCt8PruB8/Xjcpfao=")
        {
            admin.SenhaHash = "zqycMHtjjh70OptBcCnOzw==:dzDnlFmaMH+oE9yUC0MeUN9kGJbCt8PruB8/Xjcpfao=";
            db.Usuarios.Update(admin);
            await db.SaveChangesAsync();
        }
    }
}
