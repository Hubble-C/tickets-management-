using QRCoder;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class QrCodeService : IQrCodeService
{
    private readonly QRCodeGenerator _generator = new();
    private const int PixelsPerModule = 10;

    public string ToPngDataUri(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("QR content is required.", nameof(content));

        using var data = _generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(PixelsPerModule);
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }
}
