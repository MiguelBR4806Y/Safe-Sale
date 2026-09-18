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
            -- Categories table
            CREATE TABLE IF NOT EXISTS categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                description TEXT
            );

            -- Products table
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

            -- Users table (admin & cashier)
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL CHECK(role IN ('admin','cashier')),
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            -- Sales/transactions table
            CREATE TABLE IF NOT EXISTS sales (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER,
                total DECIMAL(10,2) NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            );

            -- Sale items table
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

        // Migration: add payment_method column if missing
        using var migrateCmd = conn.CreateCommand();
        migrateCmd.CommandText = "PRAGMA table_info(sales)";
        using var mReader = migrateCmd.ExecuteReader();
        bool hasPaymentMethod = false;
        while (mReader.Read())
        {
            if (mReader.GetString(1) == "payment_method")
            {
                hasPaymentMethod = true;
                break;
            }
        }
        if (!hasPaymentMethod)
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE sales ADD COLUMN payment_method TEXT DEFAULT ''";
            alterCmd.ExecuteNonQuery();
        }

        SeedDefaultAdmin(conn);
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