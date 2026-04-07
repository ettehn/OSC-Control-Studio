namespace OSCControl.Packaging;

public sealed class PackagedAppManifest
{
    public string Name { get; set; } = "OSCControl App";

    public string Script { get; set; } = "app.osccontrol";

    public string? Plan { get; set; } = "app.plan.json";

    public string Data { get; set; } = "../data";

    public string Logs { get; set; } = "../logs";

    public string? SourceScript { get; set; }
}
