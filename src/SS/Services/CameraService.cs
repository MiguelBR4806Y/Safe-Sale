using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SS.Services;

public class CameraService : ICameraService
{
    private VideoCapture? _capture;
    private CancellationTokenSource? _cts;
    private bool _isCapturing;

    public bool IsCapturing => _isCapturing;

    public event Action<byte[]>? FrameAvailable;

    public Task StartCaptureAsync(int cameraIndex = 0)
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

            Task.Run(() => CaptureLoop(_cts.Token));
        }
        catch
        {
            _capture?.Dispose();
            _capture = null;
            _isCapturing = false;
        }

        return Task.CompletedTask;
    }

    public void StopCapture()
    {
        _isCapturing = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
    }

    private void CaptureLoop(CancellationToken token)
    {
        using var frame = new Mat();

        while (!token.IsCancellationRequested && _isCapturing && _capture != null)
        {
            try
            {
                if (_capture.Read(frame) && !frame.Empty())
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

    public void Dispose()
    {
        StopCapture();
    }
}
