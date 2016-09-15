describe('hubConnection', () => {
    interface ChatMessage {
        from: string,
        message: string
    }

    interface ChatHubClient {
        connection: SignalR.Connection,
        proxy: SignalR.Hub.Proxy,
        message: JQueryPromise<ChatMessage>,
        start: () => JQueryPromise<any>
    }

    function createChatHubConnection(transport: SignalR.Transport): ChatHubClient {
        let connection = $.hubConnection(HUBS_URL);
        let proxy = connection.createHubProxy('chatHub');
        let deferred = $.Deferred();
        connection.logging = true;

        proxy.on('receiveMessage', (from, message) => {
            deferred.resolve({
                from: from,
                message: message
            });
        });

        return {
            connection: connection,
            proxy: proxy,
            message: deferred.promise(),
            start: () => connection.start({ transport: transport.name })
        };
    }

/*
    eachTransport(transport => {
        describe(`over the ${transport.name} transport`, () => {
            it('can connect to server', done => {
                var client = createChatHubConnection(transport);
                return client.start().then(() => {
                    expect(client.connection.state).toEqual($.signalR.connectionState.connected);
                }).fail(fail).always(() => {
                    client.connection.stop();
                    done();
                });
            });

            it('can have two connections', done => {
                var client1 = createChatHubConnection(transport);
                var client2 = createChatHubConnection(transport);
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

            it('can handle RPC', done => {
                var client = createChatHubConnection(transport);
                return client.start().then(() => {
                    return client.proxy.invoke('add', 40, 2);
                }).then(result => {
                    expect(result).toEqual(42);
                    return client.proxy.invoke('addAsync', 40, 2);
                }).then(result => {
                    expect(result).toEqual(42);
                }).fail(fail).always(() => {
                    client.connection.stop();
                    done();
                });
            });

            it('can broadcast to all clients', done => {
                var client1 = createChatHubConnection(transport);
                var client2 = createChatHubConnection(transport);
                var client3 = createChatHubConnection(transport);
                return $.when(client1.start(), client2.start(), client3.start()).then(() => {
                    client1.proxy.invoke('broadcast', 'client1', 'Hello, World!');

                    return $.when(client1.message, client2.message, client3.message);
                }).then((m1, m2, m3) => {
                    expect(m1).toEqual({ from: "client1", message: "Hello, World!" });
                    expect(m2).toEqual({ from: "client1", message: "Hello, World!" });
                    expect(m3).toEqual({ from: "client1", message: "Hello, World!" });
                }).fail(fail).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    client3.connection.stop();
                    done();
                });
            });

            it('can broadcast to groups', done => {
                var client1 = createChatHubConnection(transport);
                var client2 = createChatHubConnection(transport);
                var client3 = createChatHubConnection(transport);
                var client4 = createChatHubConnection(transport);
                return $.when(client1.start(), client2.start(), client3.start(), client4.start()).then(() => {
                    return $.when(client2.proxy.invoke('joinGroup', 'test'), client3.proxy.invoke('joinGroup', 'test'), client4.proxy.invoke('joinGroup', 'test'));
                }).then(() => {
                    return client3.proxy.invoke('leaveGroup', 'test');
                }).then(() => {
                    return client1.proxy.invoke('sendToGroup', 'client1', 'test', 'Hello, World!');
                }).then(() => {
                    return $.when(client2.message, client4.message);
                }).then((m2, m4) => {
                    expect(m2).toEqual({ from: "client1", message: "Hello, World!" });
                    expect(m4).toEqual({ from: "client1", message: "Hello, World!" });
                    expect(client1.message.state()).toEqual("pending");
                    expect(client3.message.state()).toEqual("pending");
                }).fail(fail).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    client3.connection.stop();
                    client4.connection.stop();
                    done();
                });
            });

            it('can broadcast to group joined on connection', done => {
                var client1 = createChatHubConnection(transport);
                var client2 = createChatHubConnection(transport);
                var client3 = createChatHubConnection(transport);
                return $.when(client1.start(), client2.start(), client3.start()).then(() => {
                    client1.proxy.invoke('sendToGroup', 'client1', 'onconnectedgroup', 'Hello, World!');

                    return $.when(client1.message, client2.message, client3.message);
                }).then((m1, m2, m3) => {
                    expect(m1).toEqual({ from: "client1", message: "Hello, World!" });
                    expect(m2).toEqual({ from: "client1", message: "Hello, World!" });
                    expect(m3).toEqual({ from: "client1", message: "Hello, World!" });
                }).fail(fail).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    client3.connection.stop();
                    done();
                });
            });

            it('can receive progress messages', done => {
                var client1 = createChatHubConnection(transport);
                var receivedProgress = [];
                return client1.start().then(() => {
                    return client1.proxy.invoke('withProgress')
                        .progress(i => receivedProgress.push(i));
                }).then(() => {
                    expect(receivedProgress).toEqual([0, 1, 2, 3, 4]);
                }).always(() => {
                    client1.connection.stop();
                    done();
                });
            });

            it('can recover from reconnect', done => {
                var reconnected = $.Deferred();
                var client1 = createChatHubConnection(transport);
                var client2 = createChatHubConnection(transport);
                return $.when(client1.start(), client2.start()).then(() => {
                    client1.connection.reconnected(() => reconnected.resolve());
                    transport.lostConnection(client1.connection);

                    return client2.proxy.invoke('broadcast', 'client2', 'Hello!');
                }).then(() => {
                    return $.when(reconnected.promise(), client1.message);
                }).then((_, m1) => {
                    expect(m1).toEqual({ from: 'client2', message: 'Hello!' });
                }).always(() => {
                    client1.connection.stop();
                    client2.connection.stop();
                    done();
                });
            });
        });
    });
    */
});