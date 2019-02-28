const WebSocket = require('ws');
const SocketClient = require('./../dist/commonjs/index').SocketClient;


function start() {
	const ws = new WebSocket("http://localhost:8000");

	const regularSocketClient = new SocketClient(ws);

	regularSocketClient.on('my_test', (myMessage) => {
		console.log('received on regular socket client: ', myMessage);
	});

	const namespacedSocketClient = new SocketClient(ws, "my_namespace");

	namespacedSocketClient.on('my_test', (myMessage) => {
		console.log('received on namespaced socket client: ', myMessage);
	});

}

start();