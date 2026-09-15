using System;
using System.Security.Cryptography;
using System.Text;

namespace SS.Services;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        string salt = Convert.ToBase64String(saltBytes);
        string saltedPassword = salt + password;

        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        string hash = Convert.ToBase64String(hashBytes);

        return $"{salt}:{hash}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        string[] parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        string salt = parts[0];
        string expectedHash = parts[1];

        string saltedPassword = salt + password;

        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        string actualHash = Convert.ToBase64String(hashBytes);

        return actualHash == expectedHash;
    }
}
