namespace AutonomusCRM.AI;

public class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public bool Enabled { get; set; }
}
