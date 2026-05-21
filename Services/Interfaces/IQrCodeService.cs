namespace tickets_management.Services.Interfaces;

public interface IQrCodeService
{
    /// <summary>
    /// Renders <paramref name="content"/> (a TicketCode) as a self-contained
    /// inline SVG string, ready to drop into a Razor view with Html.Raw.
    /// </summary>
    string ToSvg(string content);
}
