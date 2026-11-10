using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SS.Services;

public class CameraService : ICameraService
{
    private VideoCapture? _capture;
    private CancellationTokenSource? _cts;
    private ManualResetEventSlim? _loopExited;
    private bool _isCapturing;
    private readonly object _lock = new();

    public bool IsCapturing => _isCapturing;

    public event Action<byte[]>? FrameAvailable;

    private static readonly Lazy<IReadOnlyList<string>> SystemCameraNames =
        new(LoadSystemCameraNames);

    public IReadOnlyList<string> GetAvailableCameras()
    {
        var indices = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var test = CreateCapture(i);
                if (!test.IsOpened())
                    break;

                indices.Add(i);
            }
            catch
            {
                break;
            }
        }

        var names = SystemCameraNames.Value;
        var cameras = new List<string>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            var name = i < names.Count ? names[i] : null;
            cameras.Add(string.IsNullOrWhiteSpace(name) ? $"Cámara {indices[i]}" : name!);
        }

        return Deduplicate(cameras);
    }

    private static IReadOnlyList<string> Deduplicate(List<string> cameras)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < cameras.Count; i++)
        {
            if (seen.TryGetValue(cameras[i], out var count))
            {
                seen[cameras[i]] = count + 1;
                cameras[i] = $"{cameras[i]} ({count + 1})";
            }
            else
            {
                seen[cameras[i]] = 1;
            }
        }
        return cameras;
    }

    private static IReadOnlyList<string> LoadSystemCameraNames()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                return LoadMacCameraNames();
            if (OperatingSystem.IsWindows())
                return LoadWindowsCameraNames();
            if (OperatingSystem.IsLinux())
                return LoadLinuxCameraNames();
        }
        catch
        {
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> LoadMacCameraNames()
    {
        var avFoundationNames = TryAvFoundationCameraNames();
        if (avFoundationNames.Count > 0)
            return avFoundationNames;

        var json = RunProcess("/usr/sbin/system_profiler", "-detailLevel", "mini", "-json", "SPCameraDataType");
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("SPCameraDataType", out var cameras) ||
            cameras.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var names = new List<string>();
        foreach (var camera in cameras.EnumerateArray())
        {
            if (camera.ValueKind == JsonValueKind.Object &&
                camera.TryGetProperty("_name", out var nameElement) &&
                nameElement.GetString() is { Length: > 0 } name)
            {
                names.Add(name.Trim());
            }
        }

        return names;
    }

    private const string ObjCLibrary = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCLibrary, EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjCGetClass(string name);

    [DllImport(ObjCLibrary, EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nuint MsgSendUInt(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendPtrArg(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendUIntArg(IntPtr receiver, IntPtr selector, nuint arg);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSendStringArg(IntPtr receiver, IntPtr selector,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string arg);

    private static IReadOnlyList<string> TryAvFoundationCameraNames()
    {
        try
        {
            NativeLibrary.Load("/System/Library/Frameworks/Foundation.framework/Foundation");
            NativeLibrary.Load("/System/Library/Frameworks/AVFoundation.framework/AVFoundation");

            var deviceClass = ObjCGetClass("AVCaptureDevice");
            var stringClass = ObjCGetClass("NSString");
            if (deviceClass == IntPtr.Zero || stringClass == IntPtr.Zero)
                return Array.Empty<string>();

            var devices = new List<(string Id, string Name)>();
            devices.AddRange(ReadAvFoundationDevices(deviceClass, stringClass, "vide"));
            devices.AddRange(ReadAvFoundationDevices(deviceClass, stringClass, "muxd"));

            devices.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return devices.Select(device => device.Name).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<(string Id, string Name)> ReadAvFoundationDevices(
        IntPtr deviceClass, IntPtr stringClass, string mediaTypeName)
    {
        var mediaType = MsgSendStringArg(stringClass, SelRegisterName("stringWithUTF8String:"), mediaTypeName);
        var devices = MsgSendPtrArg(deviceClass, SelRegisterName("devicesWithMediaType:"), mediaType);
        if (devices == IntPtr.Zero)
            yield break;

        var count = MsgSendUInt(devices, SelRegisterName("count"));
        var objectAtIndex = SelRegisterName("objectAtIndex:");
        var uniqueIdSelector = SelRegisterName("uniqueID");
        var localizedName = SelRegisterName("localizedName");
        var utf8String = SelRegisterName("UTF8String");

        for (nuint i = 0; i < count; i++)
        {
            var device = MsgSendUIntArg(devices, objectAtIndex, i);
            if (device == IntPtr.Zero)
                continue;

            var name = ReadUtf8String(MsgSendPtr(device, localizedName), utf8String);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var id = ReadUtf8String(MsgSendPtr(device, uniqueIdSelector), utf8String) ?? name;
            yield return (id, name);
        }
    }

    private static string? ReadUtf8String(IntPtr nsString, IntPtr utf8StringSelector)
    {
        if (nsString == IntPtr.Zero)
            return null;

        var utf8 = MsgSendPtr(nsString, utf8StringSelector);
        return utf8 == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8)?.Trim();
    }

    private static IReadOnlyList<string> LoadWindowsCameraNames()
    {
        var cameras = RunPowerShell(
            "Get-CimInstance Win32_PnPEntity | Where-Object { $_.PNPClass -eq 'Camera' } | Select-Object -ExpandProperty Name");
        if (cameras.Count > 0)
            return cameras;

        return RunPowerShell(
            "Get-CimInstance Win32_PnPEntity | Where-Object { $_.PNPClass -eq 'Image' } | Select-Object -ExpandProperty Name");
    }

    private static List<string> RunPowerShell(string script)
    {
        var output = RunProcess("powershell.exe", "-NoProfile", "-NonInteractive", "-Command", script);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static IReadOnlyList<string> LoadLinuxCameraNames()
    {
        var names = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var namePath = $"/sys/class/video4linux/video{i}/name";
            if (!File.Exists(namePath))
                break;

            try
            {
                var name = File.ReadAllText(namePath).Trim();
                names.Add(string.IsNullOrEmpty(name) ? $"Cámara {i}" : name);
            }
            catch
            {
                names.Add($"Cámara {i}");
            }
        }
        return names;
    }

    private static string RunProcess(string fileName, params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process == null)
                return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(true); } catch { }
                return string.Empty;
            }

            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static VideoCapture CreateCapture(int index)
    {
        if (OperatingSystem.IsMacOS())
            return new VideoCapture(index, VideoCaptureAPIs.AVFOUNDATION);

        return new VideoCapture(index);
    }

    public Task StartCaptureAsync(int cameraIndex = 0)
    {
        if (_isCapturing) return Task.CompletedTask;

        lock (_lock)
        {
            if (_isCapturing) return Task.CompletedTask;

            try
            {
                _capture = CreateCapture(cameraIndex);
                if (!_capture.IsOpened())
                {
                    _capture.Dispose();
                    _capture = null;
                    return Task.CompletedTask;
                }

                _capture.Set(VideoCaptureProperties.FrameWidth, 1280);
                _capture.Set(VideoCaptureProperties.FrameHeight, 720);
                _capture.Set(VideoCaptureProperties.Fps, 20);

                _isCapturing = true;
                _cts = new CancellationTokenSource();
                _loopExited = new ManualResetEventSlim(false);

                Task.Run(() => CaptureLoop(_cts.Token));
            }
            catch
            {
                _capture?.Dispose();
                _capture = null;
                _isCapturing = false;
            }
        }

        return Task.CompletedTask;
    }

    public void StopCapture()
    {
        CancellationTokenSource? cts;
        ManualResetEventSlim? loopExited;
        VideoCapture? capture;

        lock (_lock)
        {
            _isCapturing = false;
            cts = _cts;
            loopExited = _loopExited;
            capture = _capture;

            _cts = null;
            _loopExited = null;
            _capture = null;
        }

        cts?.Cancel();
        loopExited?.Wait(2000);

        cts?.Dispose();
        loopExited?.Dispose();
        capture?.Release();
        capture?.Dispose();
    }

    private void CaptureLoop(CancellationToken token)
    {
        try
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested && _isCapturing)
            {
                VideoCapture? capture;
                lock (_lock)
                {
                    capture = _capture;
                }

                if (capture == null) break;

                try
                {
                    if (capture.Read(frame) && !frame.Empty())
                    {
                        var jpegBytes = frame.ImEncode(".jpg");
                        FrameAvailable?.Invoke(jpegBytes);
                    }
                }
                catch
                {
                    break;
                }

                Thread.Sleep(50);
            }
        }
        finally
        {
            _loopExited?.Set();
        }
    }

    public void Dispose()
    {
        StopCapture();
    }
}
