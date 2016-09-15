const HUBS_URL = '/test/hubs';
const CONNECTION_URL = '/test/raw';

// Some tests can take 15secs :(.
jasmine.DEFAULT_TIMEOUT_INTERVAL = 15000;

function eachTransport(f: (transport: SignalR.Transport) => void) {
    var transportNames = Object.keys($.signalR.transports)
        .filter(x => x != "foreverFrame" && x.length > 0 && x[0] != '_');
    transportNames.forEach(t => f($.signalR.transports[t] as SignalR.Transport));
}