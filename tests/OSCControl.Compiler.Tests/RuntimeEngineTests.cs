using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Runtime;
using Xunit;

namespace OSCControl.Compiler.Tests;

public sealed class RuntimeEngineTests
{
    [Fact]
    public async Task ReceiveAsync_ExecutesRuleAndPersistsState()
    {
        const string source = """
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: 9000
    codec: osc
}

endpoint wsOut: ws.client {
    mode: output
    host: "127.0.0.1"
    port: 8080
    codec: json
}

state lastNote = 0

on receive oscIn when msg.address == "/note/on" [
    let note = arg(0)
    store lastNote = note
    send wsOut {
        address: "/forward"
        args: [[note, state("lastNote"), count(msg.args)]]
        body: {
            note: note,
            velocity: arg(1),
            source: source()
        }
    }
    log info concat("note:", string(note))
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var clock = new FakeRuntimeClock(new DateTimeOffset(2026, 4, 6, 12, 0, 0, TimeSpan.Zero));
        var transport = new RecordingTransportDispatcher(clock);
        var logs = new RecordingRuntimeLogSink();
        var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            Clock = clock,
            TransportDispatcher = transport,
            LogSink = logs
        });

        var result = await engine.ReceiveAsync("oscIn", new RuntimeEventMessage("oscIn", "/note/on", [60d, 127d]));

        Assert.Equal(1, result.MatchedRules);
        Assert.False(result.StopRequested);
        Assert.Equal(60d, engine.State.Load("lastNote"));

        var send = Assert.Single(transport.Records).Message;
        Assert.Equal("wsOut", send.TargetEndpoint);
        Assert.Equal("/forward", send.Address);
        Assert.Equal([60d, 60d, 2], send.Args);

        var body = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(send.Body);
        Assert.Equal(60d, body["note"]);
        Assert.Equal(127d, body["velocity"]);
        Assert.Equal("oscIn", body["source"]);

        var log = Assert.Single(logs.Entries);
        Assert.Equal("info", log.Level);
        Assert.Equal("note:60", log.Value);
    }

    [Fact]
    public async Task StopStep_PreventsLaterMatchingRules()
    {
        const string source = """
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: 9000
    codec: osc
}

endpoint wsOut: ws.client {
    mode: output
    host: "127.0.0.1"
    port: 8080
    codec: json
}

on address "/halt" [
    stop
]

on receive oscIn when msg.address == "/halt" [
    send wsOut {
        address: "/should-not-run"
    }
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var transport = new RecordingTransportDispatcher();
        var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            TransportDispatcher = transport
        });

        var result = await engine.ReceiveAsync("oscIn", new RuntimeEventMessage("oscIn", "/halt"));

        Assert.Equal(1, result.MatchedRules);
        Assert.True(result.StopRequested);
        Assert.Empty(transport.Records);
    }

    [Fact]
    public async Task SetMessageMutation_PersistsAcrossMatchingRules()
    {
        const string source = """
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: 9000
    codec: osc
}

endpoint wsOut: ws.client {
    mode: output
    host: "127.0.0.1"
    port: 8080
    codec: json
}

on receive oscIn when msg.address == "/mutate" [
    set msg.body.note = arg(0)
]

on address "/mutate" when msg.body.note == arg(0) [
    send wsOut {
        address: "/mutated"
    }
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var transport = new RecordingTransportDispatcher();
        var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            TransportDispatcher = transport
        });

        var message = new RuntimeEventMessage("oscIn", "/mutate", [42d], new Dictionary<string, object?>());
        var result = await engine.ReceiveAsync("oscIn", message);

        Assert.Equal(2, result.MatchedRules);
        var send = Assert.Single(transport.Records).Message;
        Assert.Equal("/mutated", send.Address);
    }

    [Fact]
    public async Task ForEach_IteratesRange_AndRestoresOuterLocal()
    {
        const string source = """
on startup [
    let item = "outer"
    for item in range(0, 3) [
        log info item
    ]
    log info item
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var logs = new RecordingRuntimeLogSink();
        var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            LogSink = logs
        });

        var result = await engine.StartAsync();

        Assert.Equal(1, result.MatchedRules);
        Assert.False(result.StopRequested);
        Assert.Collection(
            logs.Entries,
            entry => Assert.Equal(0d, entry.Value),
            entry => Assert.Equal(1d, entry.Value),
            entry => Assert.Equal(2d, entry.Value),
            entry => Assert.Equal("outer", entry.Value));
    }
    [Fact]
    public async Task WhileBreakContinue_ExecutesWithLoopControl()
    {
        const string source = """
state count = 0

on startup [
    while state("count") < 5 [
        if state("count") == 1 [
            store count = state("count") + 1
            continue
        ]

        if state("count") == 3 [
            break
        ]

        log info state("count")
        store count = state("count") + 1
    ]
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var logs = new RecordingRuntimeLogSink();
        var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            LogSink = logs
        });

        var result = await engine.StartAsync();

        Assert.Equal(1, result.MatchedRules);
        Assert.False(result.StopRequested);
        Assert.Equal(3d, engine.State.Load("count"));
        Assert.Collection(
            logs.Entries,
            entry => Assert.Equal(0d, entry.Value),
            entry => Assert.Equal(2d, entry.Value));
    }

    [Fact]
    public async Task EnvironmentFunctions_ReturnRuntimeValues()
    {
        const string source = """
on startup [
    log info env("time.utc")
    log info env("process.id")
    log info env("tcp.listening", 0)
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var clock = new FakeRuntimeClock(new DateTimeOffset(2026, 4, 6, 12, 0, 0, TimeSpan.Zero));
        var logs = new RecordingRuntimeLogSink();
        var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            Clock = clock,
            LogSink = logs
        });

        var result = await engine.StartAsync();

        Assert.Equal(1, result.MatchedRules);
        Assert.Collection(
            logs.Entries,
            entry => Assert.Equal("2026-04-06T12:00:00.0000000+00:00", entry.Value),
            entry => Assert.True(RuntimeValueHelpers.ToNumber(entry.Value) > 0),
            entry => Assert.False(Assert.IsType<bool>(entry.Value)));
    }

    private sealed class FakeRuntimeClock : IRuntimeClock
    {
        public FakeRuntimeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}

