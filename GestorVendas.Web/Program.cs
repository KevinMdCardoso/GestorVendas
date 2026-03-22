using System.Text;
using GestorVendas.Infra;
using GestorVendas.Web.Endpoints;
using GestorVendas.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ──────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
// Registra um HttpClient com a BaseAddress obtida da configuração
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var appUrl = config["applicationUrl"] ?? "https://localhost:57871";
    var baseUrl = appUrl.Split(';').FirstOrDefault() ?? "https://localhost:57871";

    var handler = new HttpClientHandler();

    // ✅ CRÍTICO: Habilitar suporte a cookies
    handler.UseCookies = true;
    handler.CookieContainer = new System.Net.CookieContainer();
    handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, error) => true;

    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl)
    };
    return client;
});

// ── Auth Cookie ────────────────────────────────────────────────
// DEVE vir ANTES de AddInfrastructure
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/login";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(10);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "GestorVendas.Auth";
    });

builder.Services.AddAuthorizationCore();

// ── Configuração de cookies para Blazor Server ─────────────────
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

// ── Infraestrutura ─────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, SessaoAuthStateProvider>();
builder.Services.AddScoped<SessaoService>();
builder.Services.AddScoped<BlazorAuthService>();

var app = builder.Build();

// ── Pipeline ───────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ── Banco de dados (EnsureCreated + seed) ──────────────────────
await app.Services.AplicarMigracoesAsync();

app.Run();
