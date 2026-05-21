using QRCoder;
using tickets_management.Services.Interfaces;

namespace tickets_management.Services;

public class QrCodeService : IQrCodeService
{
    // QRCodeGenerator is thread-safe for CreateQrCode, so a single instance is
    // fine for the singleton lifetime this service is registered with.
    private readonly QRCodeGenerator _generator = new();

    public string ToSvg(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("QR content is required.", nameof(content));

        using var data = _generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data);
        // 4 px per module keeps the SVG compact while staying scannable.
        return svg.GetGraphic(4);
    }
}
