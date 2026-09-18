namespace SS.Services;

public interface IBarcodeScannerService
{
    string? ScanFrame(byte[] imageData, int width, int height);
}
