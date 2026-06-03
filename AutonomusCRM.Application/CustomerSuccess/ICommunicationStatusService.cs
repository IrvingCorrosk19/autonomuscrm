namespace AutonomusCRM.Application.CustomerSuccess;

public record CommunicationStatusDto(
    string EmailMode,
    bool EmailIsLive,
    string WhatsAppMode,
    bool WhatsAppIsLive,
    string WarningMessage);

public interface ICommunicationStatusService
{
    CommunicationStatusDto GetStatus();
}
