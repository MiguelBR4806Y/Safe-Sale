using System;
using System.IO;

namespace SS.Services;

/// <summary>
/// Rutas locales de la aplicación. Todo vive en el perfil del usuario,
/// nunca en un servidor externo ni en una carpeta compartida.
/// </summary>
public static class AppPaths
{
    public static string AppDataDirectory
    {
        get
        {
            var overrideDir = Environment.GetEnvironmentVariable("SAFESALE_APPDATA");
            if (!string.IsNullOrWhiteSpace(overrideDir))
                return overrideDir;

            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Safe-Sale");
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(home, "Library", "Application Support", "Safe-Sale");
            }

            var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var baseDir = string.IsNullOrWhiteSpace(xdgData)
                ? Path.Combine(home, ".local", "share")
                : xdgData;

            return Path.Combine(baseDir, "Safe-Sale");
        }
    }

    public static string MySqlDirectory => Path.Combine(AppDataDirectory, "mysql");
    public static string MySqlDataDirectory => Path.Combine(MySqlDirectory, "data");
    public static string LogDirectory => Path.Combine(AppDataDirectory, "logs");
    public static string SecretsFile => Path.Combine(MySqlDirectory, "secrets.json");
    public static string StateFile => Path.Combine(MySqlDirectory, "state.json");
    public static string LockFile => Path.Combine(MySqlDirectory, "instance.lock");
    public static string MysqlLogFile => Path.Combine(LogDirectory, "mysql.log");
    public static string StartupLogFile => Path.Combine(LogDirectory, "startup.log");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(MySqlDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
