using System;
using QRCoder;

namespace OrderNKitchenMS_API.Utils;

public static class QRCodeHelper
{
    public static byte[] GenerateQRCodeBytes(string text, int pixelsPerModule = 20)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(pixelsPerModule);
    }
}
