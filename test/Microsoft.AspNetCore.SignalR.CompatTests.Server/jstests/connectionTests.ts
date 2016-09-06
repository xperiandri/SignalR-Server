describe('connection', () => {
    enum MessageType {
        JoinGroup = 0,
        LeaveGroup = 1,
        Broadcast = 2,
        SendToGroup = 3,
        Message = 4
    }

    interface TestConnectionMessage {
        Type: MessageType
        SourceOrDest: string,
        Value: string
    }

    interface TestConnectionClient {
        connection: SignalR.Connection,
        message: JQueryPromise<TestConnectionMessage>,
        start: () => JQueryPromise<any>,
        send: (Type: MessageType, SourceOrDest: string, Value: string) => void
    }

    function assertMessage(source: string, value: string, message: TestConnectionMessage) {
        expect(message.Type).toEqual(MessageType.Message);
        expect(message.SourceOrDest).toEqual(source);
        expect(message.Value).toEqual(value);
    }

    function createConnection(transport: SignalR.Transport): TestConnectionClient {
        let connection = $.connection(CONNECTION_URL);
        let deferred = $.Deferred();
        connection.logging = true;

        connection.received(data => {
            deferred.resolve(data as TestConnectionMessage);
        });

        return {
            connection: connection,
            message: deferred.promise(),
            start: () => connection.start({ transport: transport.name }),
            send: (type, sourceOrDest, value) => connection.send(JSON.stringify({
                type: type,
                sourceOrDest: sourceOrDest,
                value: value
            }))
        };
    }

    eachTransport(transport => {
        describe(`over the ${transport.name} transport`, () => {
            it('can connect to the server', done => {
                var client = createConnection(transport);
                return client.start().then(() => {
                    expect(client.connection.state).toEqual($.signalR.connectionState.connected);
                }).fail(fail).always(() => {
                    client.connection.stop();
                    done();
                });
            });

            it('can have two connections', done => {
                var client1 = createConnection(transport);
                var client2 = createConnection(transport);
                return $.when(client1.start(), client2.start()).then(() => {
                    expect(client1.connection.state).toEqual($.signalR.connectionState.connected);
                    expect(client2.connection.state).toEqual($.signalR.connectionState.connected);
                    expect(client1.connection.id).not.toEqual(client2.connection.id);
                }).fail(fail).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    done();
                });
            });

            it('can broadcast to all clients', done => {
                var client1 = createConnection(transport);
                var client2 = createConnection(transport);
                var client3 = createConnection(transport);
                return $.when(client1.start(), client2.start(), client3.start()).then(() => {
                    client1.send(MessageType.Broadcast, '', 'Hello, World!');
                    return $.when(client1.message, client2.message, client3.message)
                }).then((m1, m2, m3) => {
                    assertMessage(client1.connection.id, 'Hello, World!', m1);
                    assertMessage(client1.connection.id, 'Hello, World!', m2);
                    assertMessage(client1.connection.id, 'Hello, World!', m3);
                }).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    client3.connection.stop();
                    done();
                });
            });

            it('can broadcast to groups', done => {
                var client1 = createConnection(transport);
                var client2 = createConnection(transport);
                var client3 = createConnection(transport);
                var client4 = createConnection(transport);
                return $.when(client1.start(), client2.start(), client3.start(), client4.start()).then(() => {
                    client2.send(MessageType.JoinGroup, '', 'test');
                    client3.send(MessageType.JoinGroup, '', 'test');
                    client4.send(MessageType.JoinGroup, '', 'test');
                    client3.send(MessageType.LeaveGroup, '', 'test');
                    client1.send(MessageType.SendToGroup, 'test', 'Hello, World!');
                    return $.when(client2.message, client4.message);
                }).then((m2, m4) => {
                    assertMessage(client1.connection.id, 'Hello, World!', m2);
                    assertMessage(client1.connection.id, 'Hello, World!', m4);
                    expect(client1.message.state()).toEqual('pending');
                    expect(client3.message.state()).toEqual('pending');
                }).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    client3.connection.stop();
                    client4.connection.stop();
                    done();
                });
            });

            it('can broadcast to group joined on connection', done => {
                var client1 = createConnection(transport);
                var client2 = createConnection(transport);
                var client3 = createConnection(transport);
                return $.when(client1.start(), client2.start(), client3.start()).then(() => {
                    client1.send(MessageType.SendToGroup, 'allclients', 'Hello, Group!');
                    return $.when(client1.message, client2.message, client3.message)
                }).then((m1, m2, m3) => {
                    assertMessage(client1.connection.id, 'Hello, Group!', m1);
                    assertMessage(client1.connection.id, 'Hello, Group!', m2);
                    assertMessage(client1.connection.id, 'Hello, Group!', m3);
                }).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    client3.connection.stop();
                    done();
                });
            });

            it('can recover from reconnect', done => {
                var reconnected = $.Deferred();
                var client1 = createConnection(transport);
                var client2 = createConnection(transport);
                return $.when(client1.start(), client2.start()).then(() => {
                    client1.connection.reconnected(() => reconnected.resolve());
                    transport.lostConnection(client1.connection);

                    return client2.send(MessageType.Broadcast, '', 'Hello!');
                }).then(() => {
                    return $.when(reconnected.promise(), client1.message);
                }).then((_, m1) => {
                    assertMessage(client2.connection.id, 'Hello!', m1);
                }).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    done();
                });
            });
        });
    });
});