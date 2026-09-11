using System;
using System.IO;

namespace SS.Data;

public static class AppDatabase
{
    public static string DbPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "supermarket.db");
}
