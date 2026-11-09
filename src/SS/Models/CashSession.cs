using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SS.Models;

public partial class CashSession : ObservableObject
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningAmount { get; set; }
    public decimal? ClosingAmount { get; set; }
    public decimal CashSales { get; set; }
    public decimal TotalSales { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? Difference { get; set; }

    public bool IsOpen => ClosedAt == null;
    public string StatusLabel => IsOpen ? "ABIERTA" : "CERRADA";
    public string OpenedAtLabel => OpenedAt.ToString("dd/MM/yyyy HH:mm");
    public string ClosedAtLabel => ClosedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—";
    public string OpeningLabel => $"${OpeningAmount:F2}";
    public string CashSalesLabel => $"${CashSales:F2}";
    public string TotalSalesLabel => $"${TotalSales:F2}";
    public string ExpectedCashLabel => $"${ExpectedCash:F2}";
    public string ClosingLabel => ClosingAmount.HasValue ? $"${ClosingAmount.Value:F2}" : "—";
    public string DifferenceLabel => Difference.HasValue ? $"${Difference.Value:F2}" : "—";
    public string DifferenceColor => Difference.HasValue && Difference.Value < 0 ? "#EF5350" : "#4CAF50";
}
