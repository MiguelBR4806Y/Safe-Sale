using System;
using System.Collections.Generic;
using MySqlConnector;
using SS.Services;

namespace SS.Data;

public static class DatabaseInitializer
{
    public static void Initialize(string appConnectionString, string? serverAdminConnectionString = null)
    {
        using var conn = new MySqlConnection(appConnectionString);
        conn.Open();

        CreateSchema(conn);
        MigrateCategories(conn);
        MigrateSalesCashSession(conn);
        SeedDefaultAdmin(conn);
        SeedCategories(conn);
        SeedDefaultProducts(conn);

        // Triggers y procedimientos requieren privilegios elevados cuando el
        // servidor tiene binlog activo; se crean con la conexión administrativa.
        using var adminConn = new MySqlConnection(serverAdminConnectionString ?? appConnectionString);
        adminConn.Open();
        CreateProgrammabilityObjects(adminConn);
    }

    private static void Execute(MySqlConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void CreateSchema(MySqlConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS categories (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(120) NOT NULL UNIQUE,
                icon VARCHAR(32) DEFAULT '',
                color VARCHAR(16) DEFAULT '#7B1FA2'
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS products (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                barcode VARCHAR(64) NULL UNIQUE,
                price DECIMAL(10,2) NOT NULL,
                stock INT DEFAULT 0,
                category_id INT NULL,
                min_stock INT DEFAULT 5,
                CONSTRAINT fk_products_category FOREIGN KEY (category_id) REFERENCES categories(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                username VARCHAR(64) NOT NULL UNIQUE,
                password_hash VARCHAR(255) NOT NULL,
                role VARCHAR(16) NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT chk_users_role CHECK (role IN ('admin','cashier'))
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS cash_sessions (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id INT NOT NULL,
                opened_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                closed_at DATETIME NULL,
                opening_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
                closing_amount DECIMAL(10,2) NULL,
                cash_sales DECIMAL(10,2) NOT NULL DEFAULT 0,
                total_sales DECIMAL(10,2) NOT NULL DEFAULT 0,
                expected_cash DECIMAL(10,2) NOT NULL DEFAULT 0,
                difference DECIMAL(10,2) NULL,
                CONSTRAINT fk_cash_sessions_user FOREIGN KEY (user_id) REFERENCES users(id),
                INDEX idx_cash_user_open (user_id, closed_at)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS sales (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id INT NULL,
                cash_session_id INT NULL,
                total DECIMAL(10,2) NOT NULL,
                payment_method VARCHAR(32) DEFAULT '',
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT fk_sales_user FOREIGN KEY (user_id) REFERENCES users(id),
                CONSTRAINT fk_sales_cash_session FOREIGN KEY (cash_session_id) REFERENCES cash_sessions(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS sale_items (
                id INT AUTO_INCREMENT PRIMARY KEY,
                sale_id INT NOT NULL,
                product_id INT NOT NULL,
                quantity INT NOT NULL,
                price_sold DECIMAL(10,2) NOT NULL,
                CONSTRAINT fk_sale_items_sale FOREIGN KEY (sale_id) REFERENCES sales(id),
                CONSTRAINT fk_sale_items_product FOREIGN KEY (product_id) REFERENCES products(id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS product_audit (
                id INT AUTO_INCREMENT PRIMARY KEY,
                product_id INT NOT NULL,
                action VARCHAR(10) NOT NULL,
                old_price DECIMAL(10,2) NULL,
                new_price DECIMAL(10,2) NULL,
                old_stock INT NULL,
                new_stock INT NULL,
                changed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_audit_product (product_id),
                INDEX idx_audit_changed (changed_at)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

        cmd.ExecuteNonQuery();
    }

    private static void MigrateCategories(MySqlConnection conn)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var infoCmd = conn.CreateCommand())
        {
            infoCmd.CommandText = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'categories'";

            using var reader = infoCmd.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(0));
        }

        if (!columns.Contains("icon"))
            Execute(conn, "ALTER TABLE categories ADD COLUMN icon VARCHAR(32) DEFAULT ''");

        if (!columns.Contains("color"))
            Execute(conn, "ALTER TABLE categories ADD COLUMN color VARCHAR(16) DEFAULT '#7B1FA2'");
    }

    private static void MigrateSalesCashSession(MySqlConnection conn)
    {
        var hasColumn = false;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'sales'
                  AND COLUMN_NAME = 'cash_session_id'";
            hasColumn = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        if (!hasColumn)
            Execute(conn, "ALTER TABLE sales ADD COLUMN cash_session_id INT NULL");

        var hasConstraint = false;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'sales'
                  AND CONSTRAINT_NAME = 'fk_sales_cash_session'";
            hasConstraint = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        if (!hasConstraint)
        {
            Execute(conn, @"
                ALTER TABLE sales
                ADD CONSTRAINT fk_sales_cash_session
                FOREIGN KEY (cash_session_id) REFERENCES cash_sessions(id)");
        }
    }

    /// <summary>
    /// Objetos de base de datos: vista de stock bajo, triggers de auditoría y
    /// procedimiento de cobro transaccional (ACID con ROLLBACK).
    /// </summary>
    private static void CreateProgrammabilityObjects(MySqlConnection conn)
    {
        Execute(conn, @"
            CREATE OR REPLACE VIEW vw_stock_bajo AS
            SELECT p.id, p.name, p.barcode, p.price, p.stock, p.min_stock,
                   p.category_id, c.name AS category_name
            FROM products p
            LEFT JOIN categories c ON c.id = p.category_id
            WHERE p.stock <= p.min_stock");

        Execute(conn, "DROP TRIGGER IF EXISTS trg_products_audit_insert");
        Execute(conn, @"
            CREATE TRIGGER trg_products_audit_insert
            AFTER INSERT ON products
            FOR EACH ROW
            INSERT INTO product_audit
                (product_id, action, old_price, new_price, old_stock, new_stock, changed_at)
            VALUES
                (NEW.id, 'INSERT', NULL, NEW.price, NULL, NEW.stock, NOW())");

        Execute(conn, "DROP TRIGGER IF EXISTS trg_products_audit_update");
        Execute(conn, @"
            CREATE TRIGGER trg_products_audit_update
            AFTER UPDATE ON products
            FOR EACH ROW
            BEGIN
                IF OLD.price <> NEW.price OR OLD.stock <> NEW.stock THEN
                    INSERT INTO product_audit
                        (product_id, action, old_price, new_price, old_stock, new_stock, changed_at)
                    VALUES
                        (OLD.id, 'UPDATE', OLD.price, NEW.price, OLD.stock, NEW.stock, NOW());
                END IF;
            END");

        Execute(conn, "DROP TRIGGER IF EXISTS trg_products_audit_delete");
        Execute(conn, @"
            CREATE TRIGGER trg_products_audit_delete
            AFTER DELETE ON products
            FOR EACH ROW
            INSERT INTO product_audit
                (product_id, action, old_price, new_price, old_stock, new_stock, changed_at)
            VALUES
                (OLD.id, 'DELETE', OLD.price, NULL, OLD.stock, NULL, NOW())");

        Execute(conn, "DROP PROCEDURE IF EXISTS sp_registrar_venta");
        Execute(conn, @"
            CREATE PROCEDURE sp_registrar_venta(
                IN p_user_id INT,
                IN p_cash_session_id INT,
                IN p_payment_method VARCHAR(32),
                IN p_items JSON,
                OUT p_sale_id INT
            )
            BEGIN
                DECLARE v_total DECIMAL(10,2) DEFAULT 0;
                DECLARE v_sale_id INT DEFAULT 0;

                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    ROLLBACK;
                    RESIGNAL;
                END;

                START TRANSACTION;

                IF p_items IS NULL OR JSON_LENGTH(p_items) = 0 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La venta no tiene items.';
                END IF;

                IF p_cash_session_id IS NULL THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Debe abrir una caja antes de cobrar.';
                END IF;

                IF NOT EXISTS (
                    SELECT 1
                    FROM cash_sessions cs
                    WHERE cs.id = p_cash_session_id
                      AND cs.closed_at IS NULL
                      AND (p_user_id IS NULL OR cs.user_id = p_user_id)
                ) THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La caja esta cerrada, no existe o no pertenece al usuario.';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM JSON_TABLE(p_items, '$[*]' COLUMNS (
                        product_id INT PATH '$.product_id',
                        quantity INT PATH '$.quantity'
                    )) AS jt
                    LEFT JOIN products p ON p.id = jt.product_id
                    WHERE p.id IS NULL
                       OR jt.quantity IS NULL
                       OR jt.quantity <= 0
                       OR p.stock < jt.quantity
                ) THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Stock insuficiente o producto inexistente.';
                END IF;

                SELECT COALESCE(SUM(jt.quantity * jt.price), 0)
                  INTO v_total
                  FROM JSON_TABLE(p_items, '$[*]' COLUMNS (
                      quantity INT PATH '$.quantity',
                      price DECIMAL(10,2) PATH '$.price'
                  )) AS jt;

                IF v_total <= 0 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El total de la venta debe ser mayor a 0.';
                END IF;

                INSERT INTO sales (user_id, cash_session_id, total, payment_method, created_at)
                VALUES (p_user_id, p_cash_session_id, v_total, COALESCE(p_payment_method, ''), NOW());

                SET v_sale_id = LAST_INSERT_ID();

                INSERT INTO sale_items (sale_id, product_id, quantity, price_sold)
                SELECT v_sale_id, jt.product_id, jt.quantity, jt.price
                  FROM JSON_TABLE(p_items, '$[*]' COLUMNS (
                      product_id INT PATH '$.product_id',
                      quantity INT PATH '$.quantity',
                      price DECIMAL(10,2) PATH '$.price'
                  )) AS jt;

                UPDATE products p
                  JOIN JSON_TABLE(p_items, '$[*]' COLUMNS (
                      product_id INT PATH '$.product_id',
                      quantity INT PATH '$.quantity'
                  )) AS jt ON jt.product_id = p.id
                   SET p.stock = p.stock - jt.quantity;

                UPDATE cash_sessions
                   SET cash_sales = cash_sales + (CASE WHEN COALESCE(p_payment_method, '') = 'Efectivo' THEN v_total ELSE 0 END),
                       total_sales = total_sales + v_total
                 WHERE id = p_cash_session_id;

                UPDATE cash_sessions
                   SET expected_cash = opening_amount + cash_sales
                 WHERE id = p_cash_session_id;

                SET p_sale_id = v_sale_id;

                COMMIT;
            END");
    }

    private static void SeedDefaultAdmin(MySqlConnection conn)
    {
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = 'admin'";
            var count = Convert.ToInt64(checkCmd.ExecuteScalar());
            if (count > 0) return;
        }

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

    private static void SeedCategories(MySqlConnection conn)
    {
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM categories";
            var count = Convert.ToInt64(checkCmd.ExecuteScalar());
            if (count > 0) return;
        }

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

        var nameParam = insertCmd.Parameters.Add("@name", MySqlDbType.VarChar);
        var iconParam = insertCmd.Parameters.Add("@icon", MySqlDbType.VarChar);
        var colorParam = insertCmd.Parameters.Add("@color", MySqlDbType.VarChar);

        foreach (var (name, icon, color) in categories)
        {
            nameParam.Value = name;
            iconParam.Value = icon;
            colorParam.Value = color;
            insertCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Inventario de demostración: 2 productos por categoría. Solo se carga si
    /// la tabla de productos está vacía, para no duplicar en cada arranque.
    /// </summary>
    private static void SeedDefaultProducts(MySqlConnection conn)
    {
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM products";
            var count = Convert.ToInt64(checkCmd.ExecuteScalar());
            if (count > 0) return;
        }

        var categoryIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using (var catCmd = conn.CreateCommand())
        {
            catCmd.CommandText = "SELECT id, name FROM categories";
            using var reader = catCmd.ExecuteReader();
            while (reader.Read())
                categoryIds[reader.GetString(1)] = reader.GetInt32(0);
        }

        var products = new (string Category, string Name, string Barcode, decimal Price, int Stock, int MinStock)[]
        {
            ("Abarrotes y Despensa",   "Arroz 1kg",              "7500000000011", 18.50m, 40, 8),
            ("Abarrotes y Despensa",   "Frijol negro 1kg",       "7500000000028", 22.00m, 35, 8),
            ("Lacteos",                "Leche entera 1L",        "7500000000035", 24.90m, 30, 10),
            ("Lacteos",                "Queso fresco 250g",      "7500000000042", 45.00m, 15, 5),
            ("Panaderia y Reposteria", "Pan de molde",           "7500000000059", 32.00m, 20, 5),
            ("Panaderia y Reposteria", "Galletas de soda",       "7500000000066", 15.50m, 40, 10),
            ("Carnes y Embutidos",     "Pechuga de pollo 1kg",   "7500000000073", 89.90m, 12, 4),
            ("Carnes y Embutidos",     "Jamon 250g",             "7500000000080", 38.50m, 18, 5),
            ("Frutas y Verduras",      "Manzana 1kg",            "7500000000097", 28.00m, 25, 6),
            ("Frutas y Verduras",      "Tomate 1kg",             "7500000000103", 20.00m, 30, 6),
            ("Bebidas",                "Agua 1.5L",              "7500000000110", 12.00m, 50, 12),
            ("Bebidas",                "Refresco cola 2L",       "7500000000127", 35.00m, 24, 8),
            ("Snacks y Dulces",        "Papas fritas",           "7500000000134", 18.00m, 45, 10),
            ("Snacks y Dulces",        "Chocolate",              "7500000000141", 22.00m, 35, 8),
            ("Cuidado Personal",       "Jabon de bano",          "7500000000158", 16.50m, 28, 6),
            ("Cuidado Personal",       "Pasta dental",           "7500000000165", 29.90m, 22, 6),
            ("Limpieza y Hogar",       "Detergente 1kg",         "7500000000172", 42.00m, 20, 5),
            ("Limpieza y Hogar",       "Cloro 1L",               "7500000000189", 18.50m, 30, 8),
        };

        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = @"
            INSERT INTO products (name, barcode, price, stock, category_id, min_stock)
            VALUES (@name, @barcode, @price, @stock, @category_id, @min_stock)";

        var nameParam = insertCmd.Parameters.Add("@name", MySqlDbType.VarChar);
        var barcodeParam = insertCmd.Parameters.Add("@barcode", MySqlDbType.VarChar);
        var priceParam = insertCmd.Parameters.Add("@price", MySqlDbType.Decimal);
        var stockParam = insertCmd.Parameters.Add("@stock", MySqlDbType.Int32);
        var categoryParam = insertCmd.Parameters.Add("@category_id", MySqlDbType.Int32);
        var minStockParam = insertCmd.Parameters.Add("@min_stock", MySqlDbType.Int32);

        foreach (var p in products)
        {
            if (!categoryIds.TryGetValue(p.Category, out var categoryId))
                continue;

            nameParam.Value = p.Name;
            barcodeParam.Value = p.Barcode;
            priceParam.Value = p.Price;
            stockParam.Value = p.Stock;
            categoryParam.Value = categoryId;
            minStockParam.Value = p.MinStock;
            insertCmd.ExecuteNonQuery();
        }
    }
}
