using System;
using System.Collections.Generic;
using OpenCvSharp;
using ZXing;
using ZXing.Common;

namespace SS.Services;

public class BarcodeScannerService : IBarcodeScannerService
{
    private readonly BarcodeReaderGeneric _reader;

    public BarcodeScannerService()
    {
        var reader = new BarcodeReaderGeneric();
        reader.Options.TryHarder = true;
        reader.Options.TryInverted = true;
        reader.Options.PossibleFormats = new List<BarcodeFormat>
        {
            BarcodeFormat.EAN_13,
            BarcodeFormat.UPC_A,
            BarcodeFormat.CODE_128,
            BarcodeFormat.QR_CODE
        };
        reader.Options.ReturnCodabarStartEnd = false;
        _reader = reader;
    }

    public string? ScanFrame(byte[] imageData, int width, int height)
    {
        try
        {
            using var mat = Cv2.ImDecode(imageData, ImreadModes.Color);
            if (mat.Empty()) return null;

            int roiX = mat.Width / 4;
            int roiY = mat.Height / 3;
            int roiW = mat.Width / 2;
            int roiH = mat.Height / 3;

            using var roi = new Mat(mat, new Rect(roiX, roiY, roiW, roiH));

            var rgbBytes = ConvertToRGB(roi);

            var result = _reader.Decode(rgbBytes, roi.Width, roi.Height, RGBLuminanceSource.BitmapFormat.RGB24);

            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] ConvertToRGB(Mat mat)
    {
        using var rgb = new Mat();
        Cv2.CvtColor(mat, rgb, ColorConversionCodes.BGR2RGB);
        var bytes = new byte[rgb.Rows * rgb.Cols * 3];
        System.Runtime.InteropServices.Marshal.Copy(rgb.Data, bytes, 0, bytes.Length);
        return bytes;
    }
}
