import { IEndpointSocketScope, MessageEnvelope, OnConnectCallback } from '@essential-projects/websocket_contracts';
import * as WebSocket from 'ws';
import { SocketClient } from './socket_client';

export class EndpointSocketScope implements IEndpointSocketScope {

  private _endpointName: string;
  private _socketServer: WebSocket.Server;

  private get endpointName(): string {
    return this._endpointName;
  }

  private get socketServer(): WebSocket.Server {
    return this._socketServer;
  }

  constructor(endpointName: string, socketServer: WebSocket.Server) {
    this._endpointName = endpointName;
    this._socketServer = socketServer;
  }

  public onConnect(callback: OnConnectCallback): void {
    this.socketServer.on('connection', (socket: WebSocket) => {
      const socketClient: SocketClient = new SocketClient(socket, this.endpointName);
      callback(socketClient);
    });
  }

  public emit<TMessage>(eventType: string, message: TMessage): void {
    for (const client of this.socketServer.clients) {
      if (client.readyState === WebSocket.OPEN) {
        const payload: MessageEnvelope<TMessage> = new MessageEnvelope(message, eventType, this.endpointName);
        client.send(JSON.stringify(payload));
      }
    }
  }
}
