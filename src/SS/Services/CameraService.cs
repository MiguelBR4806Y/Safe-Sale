using System;
using System.Collections.Generic;
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

    public IReadOnlyList<string> GetAvailableCameras()
    {
        var cameras = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var test = new VideoCapture(i);
                if (!test.IsOpened())
                    break;

                var backend = test.GetBackendName();
                cameras.Add(string.IsNullOrEmpty(backend) ? $"Cámara {i}" : $"{backend} - Cámara {i}");
            }
            catch
            {
                break;
            }
        }
        return cameras;
    }

    public Task StartCaptureAsync(int cameraIndex = 0)
    {
        if (_isCapturing) return Task.CompletedTask;

        lock (_lock)
        {
            if (_isCapturing) return Task.CompletedTask;

            try
            {
                _capture = new VideoCapture(cameraIndex);
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
