using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace SS.Services;

public sealed class DatabaseSecrets
{
    public string AppUser { get; set; } = "safesale_app";
    public string AppPassword { get; set; } = "";
    public string RootPassword { get; set; } = "";
}

/// <summary>
/// Guarda las credenciales internas del motor local. El usuario nunca las ve
/// ni las escribe: se generan solas en el primer arranque.
/// </summary>
public static class CredentialStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static DatabaseSecrets LoadOrCreate(string appUser)
    {
        AppPaths.EnsureCreated();

        if (File.Exists(AppPaths.SecretsFile))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<DatabaseSecrets>(File.ReadAllText(AppPaths.SecretsFile));
                if (existing != null && !string.IsNullOrWhiteSpace(existing.AppPassword) &&
                    !string.IsNullOrWhiteSpace(existing.RootPassword))
                {
                    existing.AppUser = appUser;
                    return existing;
                }
            }
            catch
            {
                // Archivo corrupto: se regenera.
            }
        }

        var secrets = new DatabaseSecrets
        {
            AppUser = appUser,
            AppPassword = GeneratePassword(),
            RootPassword = GeneratePassword()
        };

        Save(secrets);
        return secrets;
    }

    private static void Save(DatabaseSecrets secrets)
    {
        var json = JsonSerializer.Serialize(secrets, JsonOptions);
        File.WriteAllText(AppPaths.SecretsFile, json);
        RestrictPermissions(AppPaths.SecretsFile);
    }

    /// <summary>Contraseña hexadecimal (solo [0-9A-F]), segura para SQL y connection strings.</summary>
    private static string GeneratePassword() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static void RestrictPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            // %LOCALAPPDATA% ya hereda ACLs del perfil del usuario.
            try { File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden); } catch { }
            return;
        }

        try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch { }
    }
}
