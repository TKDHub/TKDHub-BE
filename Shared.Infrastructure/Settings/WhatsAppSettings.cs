namespace Shared.Infrastructure.Settings;

/// <summary>Meta WhatsApp Cloud API credentials — https://developers.facebook.com/docs/whatsapp/cloud-api.</summary>
public sealed class WhatsAppSettings
{
    public const string SectionName = "WhatsAppSettings";

    public string ApiUrl        { get; init; } = "https://graph.facebook.com/v20.0";
    public string PhoneNumberId { get; init; } = string.Empty;
    public string AccessToken   { get; init; } = string.Empty;
}
