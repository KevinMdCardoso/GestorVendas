using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        var senha = "99441986";
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(senha, salt, 100_000, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(32);
            var resultado = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
            Console.WriteLine($"Hash para senha '{senha}':");
            Console.WriteLine(resultado);
        }
    }
}
