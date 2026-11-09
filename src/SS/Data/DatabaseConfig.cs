using System;
using SS.Services;

namespace SS.Data;

public static class DatabaseConfig
{
    public const string DatabaseName = "safesale";
    public const string AppUserName = "safesale_app";

    /// <summary>
    /// Cadena de conexión interna. La resuelve la app sola: el usuario no
    /// configura host, puerto, usuario ni contraseña.
    /// </summary>
    public static string ConnectionString
    {
        get
        {
            var devOverride = Environment.GetEnvironmentVariable("SAFESALE_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(devOverride))
                return devOverride;

            var managed = MySqlServerManager.Instance.ConnectionString;
            if (string.IsNullOrWhiteSpace(managed))
                throw new InvalidOperationException("El motor MySQL local no está iniciado.");

            return managed;
        }
    }

    /// <summary>
    /// Conexión administrativa para crear triggers/procedimientos. Si hay un
    /// override de desarrollo (MySQL externo), se usa la misma conexión de la app.
    /// </summary>
    public static string? ServerAdminConnectionString
    {
        get
        {
            var devOverride = Environment.GetEnvironmentVariable("SAFESALE_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(devOverride))
                return null;

            return MySqlServerManager.Instance.RootConnectionString;
        }
    }
}
