using System;
using System.Threading.Tasks;

namespace SS.Services;

public interface ICameraService : IDisposable
{
    bool IsCapturing { get; }
    Task StartCaptureAsync(int cameraIndex = 0);
    void StopCapture();
    event Action<byte[]>? FrameAvailable;
}
