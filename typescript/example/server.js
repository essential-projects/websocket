const Express = require('express');
const http = require('http');
const WebSocket = require('ws');
const SocketClient = require('./../dist/commonjs/index').SocketClient;
const EndpointSocketScope = require('./../dist/commonjs/index').EndpointSocketScope;

const app = Express();
const httpServer = http.Server(app);
const socketServer = new WebSocket.Server({
    server: httpServer,
});

const endpointSocketScope = new EndpointSocketScope('my_namespace', socketServer);

class ExampleMessage {
    constructor(testMessage) {
        this.testMessage = testMessage;
    }
}

endpointSocketScope.onConnect((socketClient) => {

    socketClient.emit('my_test', new ExampleMessage('sent directly to the specific client'));

    endpointSocketScope.emit('my_test', new ExampleMessage('sent to all connected clients'));
});

socketServer.addListener("connection", (socket) => {
    const socketClient = new SocketClient(socket);
    socketClient.emit('my_test', new ExampleMessage('sent using a socket directly'))
});

httpServer.listen(8001, 'localhost', () => {
    console.log('server started');
});
