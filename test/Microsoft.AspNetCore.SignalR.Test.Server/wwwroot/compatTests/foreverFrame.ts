describe('foreverFrame transport', () => {
    it('should fail to initialize connection', done => {
        let connection = $.connection(HUBS_URL);
        connection.start({ transport: 'foreverFrame' })
            .done(() => fail("expected the transport to fail to connect"))
            .fail(e => {
                expect(e.message).toEqual("No transport could be initialized successfully. Try specifying a different transport or none at all for auto initialization.");
            }).always(() => {
                done()
            });
    });

    it('should fail to initialize hubConnection', done => {
        let connection = $.hubConnection(HUBS_URL);
        connection.start({ transport: 'foreverFrame' })
            .done(() => fail("expected the transport to fail to connect"))
            .fail(e => {
                expect(e.message).toEqual("No transport could be initialized successfully. Try specifying a different transport or none at all for auto initialization.");
            }).always(() => {
                done()
            });
    });
});
