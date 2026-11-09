using System;
using System.Collections.ObjectModel;
using MySqlConnector;
using SS.Models;

namespace SS.Data;

public class CashSessionRepository
{
    private readonly string _connectionString;

    public CashSessionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private const string SelectColumns = @"
        SELECT cs.id, cs.user_id, u.username, cs.opened_at, cs.closed_at,
               cs.opening_amount, cs.closing_amount, cs.cash_sales,
               cs.total_sales, cs.expected_cash, cs.difference
        FROM cash_sessions cs
        INNER JOIN users u ON u.id = cs.user_id";

    public CashSession? GetOpenSession(int userId)
    {
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectColumns + @"
            WHERE cs.user_id = @user_id AND cs.closed_at IS NULL
            ORDER BY cs.id DESC
            LIMIT 1";
        cmd.Parameters.AddWithValue("@user_id", userId);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public CashSession? GetById(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectColumns + " WHERE cs.id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public ObservableCollection<CashSession> GetHistory(int userId, bool allUsers)
    {
        var result = new ObservableCollection<CashSession>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectColumns
            + (allUsers ? "" : " WHERE cs.user_id = @user_id")
            + " ORDER BY cs.opened_at DESC LIMIT 100";

        if (!allUsers)
            cmd.Parameters.AddWithValue("@user_id", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(Read(reader));

        return result;
    }

    public CashSession OpenSession(int userId, decimal openingAmount)
    {
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        using (var check = conn.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = @"
                SELECT id FROM cash_sessions
                WHERE user_id = @user_id AND closed_at IS NULL
                ORDER BY id DESC LIMIT 1
                FOR UPDATE";
            check.Parameters.AddWithValue("@user_id", userId);

            var existing = check.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
                throw new InvalidOperationException("Ya hay una caja abierta para este usuario.");
        }

        using (var insert = conn.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = @"
                INSERT INTO cash_sessions (user_id, opening_amount, expected_cash)
                VALUES (@user_id, @opening, @opening)";
            insert.Parameters.AddWithValue("@user_id", userId);
            insert.Parameters.AddWithValue("@opening", openingAmount);
            insert.ExecuteNonQuery();
        }

        tx.Commit();

        return GetOpenSession(userId)
            ?? throw new InvalidOperationException("No se pudo abrir la caja.");
    }

    public CashSession CloseSession(int sessionId, decimal closingAmount)
    {
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        decimal openingAmount;

        using (var select = conn.CreateCommand())
        {
            select.Transaction = tx;
            select.CommandText = @"
                SELECT opening_amount FROM cash_sessions
                WHERE id = @id AND closed_at IS NULL
                FOR UPDATE";
            select.Parameters.AddWithValue("@id", sessionId);

            var value = select.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                throw new InvalidOperationException("La caja ya está cerrada o no existe.");

            openingAmount = Convert.ToDecimal(value);
        }

        decimal totalSales;
        decimal cashSales;

        using (var sums = conn.CreateCommand())
        {
            sums.Transaction = tx;
            sums.CommandText = @"
                SELECT COALESCE(SUM(total), 0),
                       COALESCE(SUM(CASE WHEN payment_method = 'Efectivo' THEN total ELSE 0 END), 0)
                FROM sales
                WHERE cash_session_id = @id";
            sums.Parameters.AddWithValue("@id", sessionId);

            using var reader = sums.ExecuteReader();
            reader.Read();
            totalSales = reader.GetDecimal(0);
            cashSales = reader.GetDecimal(1);
        }

        var expectedCash = openingAmount + cashSales;
        var difference = closingAmount - expectedCash;

        using (var update = conn.CreateCommand())
        {
            update.Transaction = tx;
            update.CommandText = @"
                UPDATE cash_sessions
                SET closed_at = NOW(),
                    closing_amount = @closing,
                    cash_sales = @cash_sales,
                    total_sales = @total_sales,
                    expected_cash = @expected_cash,
                    difference = @difference
                WHERE id = @id";
            update.Parameters.AddWithValue("@closing", closingAmount);
            update.Parameters.AddWithValue("@cash_sales", cashSales);
            update.Parameters.AddWithValue("@total_sales", totalSales);
            update.Parameters.AddWithValue("@expected_cash", expectedCash);
            update.Parameters.AddWithValue("@difference", difference);
            update.Parameters.AddWithValue("@id", sessionId);
            update.ExecuteNonQuery();
        }

        tx.Commit();

        return GetById(sessionId)
            ?? throw new InvalidOperationException("No se pudo cerrar la caja.");
    }

    private static CashSession Read(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        UserId = reader.GetInt32(1),
        Username = reader.GetString(2),
        OpenedAt = reader.GetDateTime(3),
        ClosedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
        OpeningAmount = reader.GetDecimal(5),
        ClosingAmount = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
        CashSales = reader.GetDecimal(7),
        TotalSales = reader.GetDecimal(8),
        ExpectedCash = reader.GetDecimal(9),
        Difference = reader.IsDBNull(10) ? null : reader.GetDecimal(10)
    };
}
