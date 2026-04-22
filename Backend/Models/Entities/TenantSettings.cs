namespace MuafaPlus.Models;

public enum PatientNamePolicy
{
    Hide         = 0,
    ShowOptional = 1,
    Require      = 2,
}

public class TenantSettings
{
    public Guid TenantId { get; set; }

    public PatientNamePolicy PatientNamePolicy    { get; set; } = PatientNamePolicy.ShowOptional;
    public string?           WhatsAppSenderId     { get; set; }   // null = use platform default
    public int               NotificationDelayHours { get; set; } = 2;
    public bool              WhatsAppEnabled      { get; set; } = false;
    public bool              ChatEnabled          { get; set; } = false;
    public int               PatientChatWindowDays { get; set; } = 7;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
