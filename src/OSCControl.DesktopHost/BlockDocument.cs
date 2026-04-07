using System.ComponentModel;

namespace OSCControl.DesktopHost;

internal sealed class BlockDocument
{
    public BindingList<BlockEndpoint> Endpoints { get; } = [];

    public BindingList<BlockRule> Rules { get; } = [];

    public static BlockDocument CreateDefault()
    {
        var document = new BlockDocument();
        document.Endpoints.Add(new BlockEndpoint
        {
            Name = "oscIn",
            Transport = BlockEndpointTransport.OscUdp,
            Mode = BlockEndpointMode.Input,
            Host = "127.0.0.1",
            Port = 9000,
            Codec = "osc"
        });

        var startupRule = new BlockRule
        {
            Trigger = BlockTriggerKind.Startup,
            WhenExpression = string.Empty
        };
        startupRule.Steps.Add(new BlockStep
        {
            Kind = BlockStepKind.Log,
            Target = "info",
            Value = "ready"
        });

        document.Rules.Add(startupRule);
        return document;
    }
}

internal sealed class BlockEndpoint
{
    public string Name { get; set; } = "endpoint";

    public BlockEndpointTransport Transport { get; set; } = BlockEndpointTransport.OscUdp;

    public BlockEndpointMode Mode { get; set; } = BlockEndpointMode.Input;

    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 9000;

    public int InputPort { get; set; } = 9000;

    public string Path { get; set; } = string.Empty;

    public string Codec { get; set; } = "osc";

    public override string ToString() => Transport == BlockEndpointTransport.Vrchat
        ? $"VRChat ({Host}:{Port}/{InputPort})"
        : $"{Name} ({Transport}, {Mode})";
}

internal sealed class BlockRule
{
    public BlockTriggerKind Trigger { get; set; } = BlockTriggerKind.Startup;

    public string EndpointName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string WhenExpression { get; set; } = string.Empty;

    public BindingList<BlockStep> Steps { get; } = [];

    public override string ToString()
    {
        return Trigger switch
        {
            BlockTriggerKind.Startup => "On startup",
            BlockTriggerKind.Receive => $"On receive {FormatName(EndpointName)} {FormatName(Address)}".Trim(),
            BlockTriggerKind.VrchatAvatarChange => "On VRChat avatar change",
            BlockTriggerKind.VrchatParameter => $"On VRChat param {FormatName(EndpointName)}".Trim(),
            _ => "Rule"
        };
    }

    private static string FormatName(string value) => string.IsNullOrWhiteSpace(value) ? "(unset)" : value.Trim();
}

internal sealed class BlockStep
{
    public BlockStepKind Kind { get; set; } = BlockStepKind.Log;

    public string Target { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public BlockPayloadMode PayloadMode { get; set; } = BlockPayloadMode.None;

    public string Extra { get; set; } = string.Empty;

    public BindingList<BlockStep> Children { get; } = [];

    public bool IsContainer => Kind == BlockStepKind.While;

    public override string ToString() => Kind.ToString();
}

internal enum BlockEndpointTransport
{
    OscUdp,
    WsClient,
    WsServer,
    Vrchat
}

internal enum BlockEndpointMode
{
    Input,
    Output
}

internal enum BlockTriggerKind
{
    Startup,
    Receive,
    VrchatAvatarChange,
    VrchatParameter
}

internal enum BlockStepKind
{
    Log,
    Store,
    Send,
    Stop,
    While,
    Break,
    Continue,
    VrchatParam,
    VrchatInput,
    VrchatChat,
    VrchatTyping
}

internal enum BlockPayloadMode
{
    None,
    Args,
    Body
}
