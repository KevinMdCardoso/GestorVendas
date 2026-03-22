using Npgsql;

var connString = "Host=localhost;Port=5432;Database=gestorvendas_dev;Username=postgres;Password=99441986";

try
{
    using (var conn = new NpgsqlConnection(connString))
    {
        await conn.OpenAsync();
        Console.WriteLine("✅ Conexão com sucesso!");
        
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT ""Id"", ""Login"", ""SenhaHash"" 
                FROM ""Usuarios"" 
                WHERE ""Login"" = 'admin'";
            
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    Console.WriteLine($"ID: {reader["Id"]}");
                    Console.WriteLine($"Login: {reader["Login"]}");
                    Console.WriteLine($"SenhaHash: {reader["SenhaHash"]}");
                }
                else
                {
                    Console.WriteLine("❌ Usuário admin não encontrado!");
                }
            }
        }
        
        // Agora atualiza a senha
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                UPDATE ""Usuarios"" 
                SET ""SenhaHash"" = 'zqycMHtjjh70OptBcCnOzw==:dzDnlFmaMH+oE9yUC0MeUN9kGJbCt8PruB8/Xjcpfao=' 
                WHERE ""Id"" = '00000000-0000-0000-0000-000000000001'";
            
            var linhasAfetadas = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ {linhasAfetadas} linha(s) atualizada(s)!");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro: {ex.Message}");
}
