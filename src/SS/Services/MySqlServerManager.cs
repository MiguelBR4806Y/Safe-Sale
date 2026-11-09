using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using MySqlConnector;

namespace SS.Services;

public sealed class MySqlStartupException : Exception
{
    public MySqlStartupException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Motor MySQL local, incluido con la app. El usuario final no configura nada:
/// la app inicializa el datadir, elige un puerto loopback, genera credenciales
/// internas, crea la base/usuario y arranca/apaga el servidor.
/// </summary>
public sealed class MySqlServerManager : IDisposable
{
    public static MySqlServerManager Instance { get; } = new();

    private const string AppUser = "safesale_app";
    private const string RootUser = "root";

    private readonly object _gate = new();
    private Process? _process;
    private FileStream? _lockHandle;
    private bool _ownsLock;
    private bool _startedByUs;
    private bool _adoptedRunning;
    private string _appPassword = "";
    private string _rootPassword = "";
    private int _port;
    private bool _disposed;

    public string? ConnectionString { get; private set; }
    public int Port => _port;
    public bool IsRunning => ConnectionString != null;

    public string? RootConnectionString =>
        ConnectionString == null
            ? null
            : BuildConnectionString(_port, RootUser, _rootPassword, false);

    public void Start()
    {
        lock (_gate)
        {
            if (ConnectionString != null)
                return;

            AppPaths.EnsureCreated();
            _ownsLock = AcquireLock();

            var secrets = CredentialStore.LoadOrCreate(AppUser);
            _appPassword = secrets.AppPassword;
            _rootPassword = secrets.RootPassword;

            var state = LoadState();
            _port = state.Port > 0 ? state.Port : FindFreePort();

            // Si ya hay un servidor de esta app corriendo, se reutiliza.
            if (CanConnectAsApp())
            {
                _adoptedRunning = true;
                ConnectionString = BuildConnectionString(_port, AppUser, _appPassword, false);
                Log($"Motor MySQL local ya estaba corriendo en 127.0.0.1:{_port}. Se reutiliza.");
                return;
            }

            if (!_ownsLock)
                throw new MySqlStartupException(
                    "Otra instancia de Safe-Sale está iniciando el motor local. Esperá unos segundos y reintentá.");

            KillOrphanIfAny();

            if (!IsPortFree(_port))
            {
                _port = FindFreePort();
            }

            SaveState(new ServerState { Port = _port });

            var server = ResolveMysqld();
            bool firstRun = !IsDataDirectoryInitialized();
            if (firstRun)
            {
                PrepareEmptyDataDirectory();
                Log("Inicializando datadir de MySQL local (primer arranque)...");
                try
                {
                    InitializeDataDirectory(server.Executable, server.BaseDirectory);
                }
                catch
                {
                    TryClearDataDirectory();
                    throw;
                }
            }

            Log($"Arrancando MySQL local en 127.0.0.1:{_port}...");
            StartServerProcess(server.Executable, server.BaseDirectory);

            try
            {
                WaitUntilReady(TimeSpan.FromSeconds(90));
                ProvisionDatabase();
            }
            catch
            {
                StopServerProcess();
                throw;
            }

            SaveState(new ServerState { Port = _port });
            ConnectionString = BuildConnectionString(_port, AppUser, _appPassword, false);
            Log("MySQL local listo.");
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_disposed) return;

            try
            {
                if (_ownsLock && (_startedByUs || _adoptedRunning))
                {
                    ShutdownServer();
                }
            }
            catch (Exception ex)
            {
                Log($"Error al apagar MySQL local: {ex.Message}");
                TryKillByPidFile();
            }
            finally
            {
                ReleaseLock();
                ConnectionString = null;
                _startedByUs = false;
                _adoptedRunning = false;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // ---------------------------------------------------------------- aprovisionamiento

    private void InitializeDataDirectory(string mysqld, string baseDir)
    {
        Directory.CreateDirectory(AppPaths.MySqlDataDirectory);

        var psi = new ProcessStartInfo
        {
            FileName = mysqld,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = baseDir
        };
        psi.ArgumentList.Add("--no-defaults");
        psi.ArgumentList.Add("--initialize-insecure");
        psi.ArgumentList.Add($"--basedir={baseDir}");
        psi.ArgumentList.Add($"--datadir={AppPaths.MySqlDataDirectory}");

        if (OperatingSystem.IsWindows())
            psi.ArgumentList.Add("--console");

        using var process = Process.Start(psi)
            ?? throw new MySqlStartupException("No se pudo ejecutar la inicialización de MySQL.");

        var errorTask = process.StandardError.ReadToEndAsync();
        var outputTask = process.StandardOutput.ReadToEndAsync();

        if (!process.WaitForExit(120_000))
        {
            try { process.Kill(true); } catch { }
            throw new MySqlStartupException("La inicialización de MySQL superó el tiempo máximo.");
        }

        var output = errorTask.GetAwaiter().GetResult() + outputTask.GetAwaiter().GetResult();
        var errTail = TailLastErrFile();
        var details = string.Join(Environment.NewLine, new[] { output, errTail }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        if (process.ExitCode != 0 || !IsDataDirectoryInitialized())
        {
            throw new MySqlStartupException(
                "No se pudo inicializar el datadir local de MySQL.\n\n" + Trim(details));
        }
    }

    private static string TailLastErrFile()
    {
        try
        {
            var dataDir = AppPaths.MySqlDataDirectory;
            if (!Directory.Exists(dataDir)) return string.Empty;

            var errFile = Directory.GetFiles(dataDir, "*.err")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            return errFile == null
                ? string.Empty
                : string.Join(Environment.NewLine, File.ReadAllLines(errFile).TakeLast(25));
        }
        catch
        {
            return string.Empty;
        }
    }

    private void StartServerProcess(string mysqld, string baseDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = mysqld,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = baseDir
        };

        psi.ArgumentList.Add("--no-defaults");
        psi.ArgumentList.Add($"--basedir={baseDir}");
        psi.ArgumentList.Add($"--datadir={AppPaths.MySqlDataDirectory}");
        psi.ArgumentList.Add("--bind-address=127.0.0.1");
        psi.ArgumentList.Add($"--port={_port}");
        psi.ArgumentList.Add($"--pid-file={Path.Combine(AppPaths.MySqlDirectory, "mysqld.pid")}");
        psi.ArgumentList.Add("--mysqlx=0");

        if (OperatingSystem.IsWindows())
        {
            // Fuerza el log de errores a la consola (redirigida a mysql.log).
            psi.ArgumentList.Add("--console");
        }
        else
        {
            psi.ArgumentList.Add($"--socket={Path.Combine(AppPaths.MySqlDirectory, "mysql.sock")}");
        }

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => AppendMysqlLog(e.Data);
        _process.ErrorDataReceived += (_, e) => AppendMysqlLog(e.Data);
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        _startedByUs = true;
    }

    private void WaitUntilReady(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            if (_process is { HasExited: true })
            {
                throw new MySqlStartupException(
                    "El proceso de MySQL local terminó inesperadamente.\n\n" + TailLog());
            }

            using (var conn = TryOpenRoot(_rootPassword))
            {
                if (conn != null) return;
            }

            using (var conn = TryOpenRoot(""))
            {
                if (conn != null) return;
            }

            try
            {
                using var probe = new MySqlConnection(
                    BuildConnectionString(_port, RootUser, _rootPassword, true));
                probe.Open();
                return;
            }
            catch (Exception ex) { lastError = ex; }

            Thread.Sleep(400);
        }

        throw new MySqlStartupException(
            "MySQL local no respondió a tiempo.\n\n" +
            (lastError?.Message ?? "Sin detalles") + "\n\n" + TailLog());
    }

    private MySqlConnection? TryOpenRoot(string password)
    {
        try
        {
            var conn = new MySqlConnection(
                BuildConnectionString(_port, RootUser, password, true));
            conn.Open();
            return conn;
        }
        catch
        {
            return null;
        }
    }

    private MySqlConnection OpenRootConnection()
    {
        using (var conn = TryOpenRoot(_rootPassword))
        {
            if (conn != null)
                return new MySqlConnection(BuildConnectionString(_port, RootUser, _rootPassword, true));
        }

        using (var conn = TryOpenRoot(""))
        {
            if (conn != null)
                return new MySqlConnection(BuildConnectionString(_port, RootUser, "", true));
        }

        throw new MySqlStartupException(
            "No se pudo autenticar como root en el MySQL local.\n\n" + TailLog());
    }

    private void ProvisionDatabase()
    {
        using var conn = OpenRootConnection();
        conn.Open();

        var statements = new List<string>
        {
            "CREATE DATABASE IF NOT EXISTS `safesale` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
            $"CREATE USER IF NOT EXISTS '{AppUser}'@'localhost' IDENTIFIED BY '{_appPassword}';",
            $"CREATE USER IF NOT EXISTS '{AppUser}'@'127.0.0.1' IDENTIFIED BY '{_appPassword}';",
            $"ALTER USER '{AppUser}'@'localhost' IDENTIFIED BY '{_appPassword}';",
            $"ALTER USER '{AppUser}'@'127.0.0.1' IDENTIFIED BY '{_appPassword}';",
            $"GRANT ALL PRIVILEGES ON `safesale`.* TO '{AppUser}'@'localhost';",
            $"GRANT ALL PRIVILEGES ON `safesale`.* TO '{AppUser}'@'127.0.0.1';",
            $"ALTER USER '{RootUser}'@'localhost' IDENTIFIED BY '{_rootPassword}';",
            $"CREATE USER IF NOT EXISTS '{RootUser}'@'127.0.0.1' IDENTIFIED BY '{_rootPassword}';",
            $"ALTER USER '{RootUser}'@'127.0.0.1' IDENTIFIED BY '{_rootPassword}';",
            $"GRANT ALL PRIVILEGES ON *.* TO '{RootUser}'@'127.0.0.1' WITH GRANT OPTION;",
            "FLUSH PRIVILEGES;"
        };

        foreach (var sql in statements)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    private void ShutdownServer()
    {
        try
        {
            using var conn = new MySqlConnection(
                BuildConnectionString(_port, RootUser, _rootPassword, true));
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SHUTDOWN;";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Si no responde, se intenta matar por PID.
            TryKillByPidFile();
        }

        if (_process != null)
        {
            try
            {
                if (!_process.WaitForExit(15_000))
                    _process.Kill(true);
            }
            catch { }
            finally
            {
                try { _process.Dispose(); } catch { }
                _process = null;
            }
        }
    }

    private void StopServerProcess()
    {
        if (_process == null) return;
        try
        {
            if (!_process.HasExited)
                _process.Kill(true);
            _process.WaitForExit(10_000);
        }
        catch { }
        finally
        {
            try { _process.Dispose(); } catch { }
            _process = null;
        }
    }

    private void KillOrphanIfAny()
    {
        var pidFile = Path.Combine(AppPaths.MySqlDirectory, "mysqld.pid");
        if (!File.Exists(pidFile)) return;

        try
        {
            var pid = int.Parse(File.ReadAllText(pidFile).Trim());
            using var proc = Process.GetProcessById(pid);
            if (proc.ProcessName.Contains("mysqld", StringComparison.OrdinalIgnoreCase))
            {
                proc.Kill(true);
                proc.WaitForExit(5_000);
                Log("Se terminó un proceso mysqld huérfano de un cierre anterior.");
            }
        }
        catch { }

        try { File.Delete(pidFile); } catch { }
    }

    private void TryKillByPidFile()
    {
        var pidFile = Path.Combine(AppPaths.MySqlDirectory, "mysqld.pid");
        try
        {
            if (!File.Exists(pidFile)) return;
            var pid = int.Parse(File.ReadAllText(pidFile).Trim());
            using var proc = Process.GetProcessById(pid);
            proc.Kill(true);
        }
        catch { }
    }

    // ---------------------------------------------------------------- detección / conexión

    private bool CanConnectAsApp()
    {
        try
        {
            using var conn = new MySqlConnection(
                BuildConnectionString(_port, AppUser, _appPassword, false));
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildConnectionString(int port, string user, string password, bool noDatabase)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = "127.0.0.1",
            Port = (uint)port,
            UserID = user,
            Password = password,
            SslMode = MySqlSslMode.None,
            AllowPublicKeyRetrieval = true,
            ConnectionTimeout = 5,
            Pooling = false
        };

        if (!noDatabase)
            builder.Database = "safesale";

        return builder.ConnectionString;
    }

    private static bool IsDataDirectoryInitialized()
    {
        var data = AppPaths.MySqlDataDirectory;
        return Directory.Exists(Path.Combine(data, "mysql"))
            && File.Exists(Path.Combine(data, "mysql.ibd"))
            && File.Exists(Path.Combine(data, "ibdata1"));
    }

    /// <summary>
    /// Si el datadir existe pero está incompleto (por ejemplo, un arranque anterior
    /// se cortó), se mueve a un backup y se crea uno nuevo. Nunca toca un datadir completo.
    /// </summary>
    private void PrepareEmptyDataDirectory()
    {
        var dataDir = AppPaths.MySqlDataDirectory;

        if (Directory.Exists(dataDir))
        {
            var entries = Directory.EnumerateFileSystemEntries(dataDir).ToList();
            if (entries.Count > 0)
            {
                var backup = Path.Combine(
                    AppPaths.MySqlDirectory,
                    $"data.incomplete-{DateTime.Now:yyyyMMdd-HHmmss}");

                try
                {
                    Directory.Move(dataDir, backup);
                    Log($"Datadir incompleto movido a {backup}.");
                }
                catch (Exception ex)
                {
                    throw new MySqlStartupException(
                        "El datadir local quedó bloqueado por un proceso de MySQL. " +
                        "Cerrá Safe-Sale por completo, esperá unos segundos y volvé a intentar.\n\n" +
                        ex.Message, ex);
                }
            }
        }

        Directory.CreateDirectory(dataDir);
    }

    private static void TryClearDataDirectory()
    {
        try
        {
            var dataDir = AppPaths.MySqlDataDirectory;
            if (!Directory.Exists(dataDir)) return;

            foreach (var entry in Directory.EnumerateFileSystemEntries(dataDir).ToList())
            {
                if (Directory.Exists(entry))
                    Directory.Delete(entry, recursive: true);
                else
                    File.Delete(entry);
            }
        }
        catch { }
    }

    private static bool IsPortFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    // ---------------------------------------------------------------- binario incluido

    private sealed record ServerBinary(string Executable, string BaseDirectory);

    private static ServerBinary ResolveMysqld()
    {
        var exeName = OperatingSystem.IsWindows() ? "mysqld.exe" : "mysqld";
        var candidates = new List<string>();

        var rid = GetRuntimeId();
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "vendor", "mysql", rid, "bin", exeName));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "vendor", "mysql", "bin", exeName));

        var envBase = Environment.GetEnvironmentVariable("SAFESALE_MYSQL_BASEDIR");
        if (!string.IsNullOrWhiteSpace(envBase))
            candidates.Add(Path.Combine(envBase, "bin", exeName));

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return new ServerBinary(candidate, Directory.GetParent(Directory.GetParent(candidate)!.FullName)!.FullName);
        }

        // Fallback: mysqld en PATH (solo desarrollo).
        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in pathDirs)
        {
            var candidate = Path.Combine(dir.Trim('"'), exeName);
            if (File.Exists(candidate))
                return new ServerBinary(candidate, Directory.GetParent(Directory.GetParent(candidate)!.FullName)!.FullName);
        }

        throw new MySqlStartupException(
            "No se encontró el motor MySQL incluido en la aplicación.\n\n" +
            "Para desarrollo, ejecutá scripts/fetch-mysql-vendor.ps1 (Windows) o " +
            "scripts/fetch-mysql-vendor.sh (macOS/Linux) para descargarlo en vendor/mysql.\n" +
            "También podés definir SAFESALE_MYSQL_BASEDIR apuntando a una instalación local.");
    }

    public static string GetRuntimeId()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            _ => "x64"
        };

        if (OperatingSystem.IsWindows()) return $"win-{arch}";
        if (OperatingSystem.IsMacOS()) return $"osx-{arch}";
        return $"linux-{arch}";
    }

    // ---------------------------------------------------------------- estado / lock / logs

    private sealed class ServerState
    {
        public int Port { get; set; }
    }

    private static ServerState LoadState()
    {
        try
        {
            if (File.Exists(AppPaths.StateFile))
                return JsonSerializer.Deserialize<ServerState>(File.ReadAllText(AppPaths.StateFile)) ?? new ServerState();
        }
        catch { }
        return new ServerState();
    }

    private static void SaveState(ServerState state)
    {
        try
        {
            AppPaths.EnsureCreated();
            File.WriteAllText(AppPaths.StateFile, JsonSerializer.Serialize(state));
        }
        catch { }
    }

    private bool AcquireLock()
    {
        try
        {
            _lockHandle = new FileStream(
                AppPaths.LockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ReleaseLock()
    {
        try { _lockHandle?.Dispose(); } catch { }
        _lockHandle = null;
        _ownsLock = false;
    }

    private static void Log(string message)
    {
        try
        {
            AppPaths.EnsureCreated();
            File.AppendAllText(AppPaths.StartupLogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private static void AppendMysqlLog(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        try
        {
            File.AppendAllText(AppPaths.MysqlLogFile, line + Environment.NewLine);
        }
        catch { }
    }

    private static string TailLog()
    {
        try
        {
            if (!File.Exists(AppPaths.MysqlLogFile)) return "Sin log del motor.";
            var lines = File.ReadAllLines(AppPaths.MysqlLogFile);
            return string.Join(Environment.NewLine, lines.TakeLast(20));
        }
        catch
        {
            return "No se pudo leer el log del motor.";
        }
    }

    private static string Trim(string text) =>
        text.Length <= 2000 ? text : text[..2000];
}
