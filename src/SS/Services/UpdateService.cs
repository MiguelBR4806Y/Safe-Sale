using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace SS.Services;

public static class UpdateService
{
    private const string GitHubRepoUrl = "https://github.com/MiguelBR4806Y/Safe-Sale";

    private static UpdateManager? _updateManager;

    public static void Initialize()
    {
        _updateManager = new UpdateManager(new GithubSource(GitHubRepoUrl, null, false));
    }

    public static async Task<bool> CheckForUpdateAsync(Action<string>? onProgress = null)
    {
        if (_updateManager == null) Initialize();

        try
        {
            var newVersion = await _updateManager!.CheckForUpdatesAsync();
            if (newVersion == null) return false;

            onProgress?.Invoke($"Nueva versión disponible: {newVersion.TargetFullRelease.Version}");

            await _updateManager.DownloadUpdatesAsync(newVersion, (percent) =>
            {
                onProgress?.Invoke($"Descargando actualización... {percent}%");
            });

            _updateManager.ApplyUpdatesAndRestart(newVersion);
            return true;
        }
        catch (Exception ex)
        {
            onProgress?.Invoke($"Error al buscar actualización: {ex.Message}");
            return false;
        }
    }
}
