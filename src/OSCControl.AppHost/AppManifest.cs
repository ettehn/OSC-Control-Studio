namespace OSCControl.AppHost;

internal sealed class AppManifest
{
    public string Name { get; set; } = "OSCControl App";

    public string Script { get; set; } = "app.osccontrol";

    public string? Plan { get; set; } = "app.plan.json";

    public string Logs { get; set; } = "logs";
}