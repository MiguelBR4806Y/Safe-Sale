using MySqlConnector;

namespace SS.Data;

public static class DbErrors
{
    /// <summary>MySQL/MariaDB: 1062 = Duplicate entry for key.</summary>
    public static bool IsDuplicateKey(System.Exception ex) =>
        ex is MySqlException { Number: 1062 };
}
