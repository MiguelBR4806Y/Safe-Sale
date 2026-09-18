using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SS.Services;

public interface ICameraService : IDisposable
{
    bool IsCapturing { get; }
    IReadOnlyList<string> GetAvailableCameras();
    Task StartCaptureAsync(int cameraIndex = 0);
    void StopCapture();
    event Action<byte[]>? FrameAvailable;
}
