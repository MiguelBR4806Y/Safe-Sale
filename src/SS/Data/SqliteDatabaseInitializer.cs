using Microsoft.Data.Sqlite;
using SS.Services;

namespace SS.Data;

public static class SqliteDatabaseInitializer
{
    public static void Initialize(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                icon TEXT DEFAULT '',
                color TEXT DEFAULT '#7B1FA2'
            );

            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                barcode TEXT UNIQUE,
                price DECIMAL(10,2) NOT NULL,
                stock INTEGER DEFAULT 0,
                category_id INTEGER,
                min_stock INTEGER DEFAULT 5,
                FOREIGN KEY (category_id) REFERENCES categories(id)
            );

            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL CHECK(role IN ('admin','cashier')),
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS sales (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER,
                total DECIMAL(10,2) NOT NULL,
                payment_method TEXT DEFAULT '',
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            );

            CREATE TABLE IF NOT EXISTS sale_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sale_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                price_sold DECIMAL(10,2) NOT NULL,
                FOREIGN KEY (sale_id) REFERENCES sales(id),
                FOREIGN KEY (product_id) REFERENCES products(id)
            )";

        cmd.ExecuteNonQuery();

        // Migration: add icon/color columns to categories if missing
        MigrateCategories(conn);

        SeedDefaultAdmin(conn);
        SeedCategories(conn);
    }

    private static void MigrateCategories(SqliteConnection conn)
    {
        using var infoCmd = conn.CreateCommand();
        infoCmd.CommandText = "PRAGMA table_info(categories)";
        using var reader = infoCmd.ExecuteReader();

        var columns = new System.Collections.Generic.HashSet<string>();
        while (reader.Read())
            columns.Add(reader.GetString(1));

        if (!columns.Contains("icon"))
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE categories ADD COLUMN icon TEXT DEFAULT ''";
            alter.ExecuteNonQuery();
        }
        if (!columns.Contains("color"))
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = "ALTER TABLE categories ADD COLUMN color TEXT DEFAULT '#7B1FA2'";
            alter.ExecuteNonQuery();
        }
    }

    private static void SeedCategories(SqliteConnection conn)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM categories";
        long count = (long)checkCmd.ExecuteScalar()!;
        if (count > 0) return;

        var categories = new (string name, string icon, string color)[]
        {
            ("Abarrotes y Despensa",     "\U0001F33E", "#8D6E63"),
            ("Lacteos",                  "\U0001F95B", "#42A5F5"),
            ("Panaderia y Reposteria",   "\U0001F35E", "#FFB74D"),
            ("Carnes y Embutidos",       "\U0001F969", "#EF5350"),
            ("Frutas y Verduras",        "\U0001F966", "#66BB6A"),
            ("Bebidas",                  "\U0001F964", "#AB47BC"),
            ("Snacks y Dulces",          "\U0001F36A", "#FF7043"),
            ("Cuidado Personal",         "\U0001F9F4", "#26C6DA"),
            ("Limpieza y Hogar",         "\U0001F9F9", "#78909C"),
        };

        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO categories (name, icon, color) VALUES (@name, @icon, @color)";

        var nameParam = insertCmd.Parameters.Add("@name", SqliteType.Text);
        var iconParam = insertCmd.Parameters.Add("@icon", SqliteType.Text);
        var colorParam = insertCmd.Parameters.Add("@color", SqliteType.Text);

        foreach (var (name, icon, color) in categories)
        {
            nameParam.Value = name;
            iconParam.Value = icon;
            colorParam.Value = color;
            insertCmd.ExecuteNonQuery();
        }
    }

    private static void SeedDefaultAdmin(SqliteConnection conn)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM users";
        long count = (long)checkCmd.ExecuteScalar()!;

        if (count > 0) return;

        string passwordHash = PasswordHasher.HashPassword("admin123");

        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO users (username, password_hash, role)
            VALUES (@username, @password_hash, @role)";
        insertCmd.Parameters.AddWithValue("@username", "admin");
        insertCmd.Parameters.AddWithValue("@password_hash", passwordHash);
        insertCmd.Parameters.AddWithValue("@role", "admin");

        insertCmd.ExecuteNonQuery();
    }
}
