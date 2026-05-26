namespace tickets_management.Services.Interfaces;

public interface IQrCodeService
{
    string ToPngDataUri(string content);
}
