using Avalonia;
using System;
using System.IO;
using SS.Data;

namespace SS;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "supermarket.db");
        SqliteDatabaseInitializer.Initialize(dbPath);

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}