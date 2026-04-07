# WebSocket runtime transport

OSCControl supports WebSocket endpoints through `ws.client` and `ws.server`.

## Endpoint modes

```osccontrol
endpoint wsClient: ws.client {
    mode: duplex
    host: "127.0.0.1"
    port: 8080
    path: "/control"
    codec: json
}

endpoint wsServer: ws.server {
    mode: duplex
    host: "127.0.0.1"
    port: 8081
    path: "/control"
    codec: json
}
```

Modes:

```text
input   Receive WebSocket messages.
output  Send WebSocket messages.
duplex  Receive and send on the same endpoint.
```

`ws.client` connects to a remote WebSocket server. In `input` or `duplex` mode,
the runtime also listens for messages from the connected server. In `output` or
`duplex` mode, runtime sends reuse that client connection.

`ws.server` accepts WebSocket clients. In `input` or `duplex` mode, messages from
clients trigger receive rules. In `output` or `duplex` mode, runtime sends are
broadcast to currently connected clients.

## Codecs

`codec: json` sends and receives an envelope shape when address, args, body,
headers, or extras are present. `codec: text` sends text payloads. `codec: bytes`
sends binary payloads.

Example:

```osccontrol
on receive wsServer when msg.address == "/ping" [
    send wsServer {
        address: "/pong"
        body: "pong"
    }
]
```
