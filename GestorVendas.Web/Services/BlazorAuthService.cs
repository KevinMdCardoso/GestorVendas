using GestorVendas.Application.DTOs;
using System.Net.Http.Json;

namespace GestorVendas.Web.Services;

public class BlazorAuthService
{
    private readonly HttpClient _httpClient;

    public BlazorAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", req);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Login ou senha inválidos.");
            throw new Exception("Erro ao conectar com o servidor.");
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>() 
            ?? throw new Exception("Erro ao processar resposta.");
    }

    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("api/auth/logout", null);
    }
}
