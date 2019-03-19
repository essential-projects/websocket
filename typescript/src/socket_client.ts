import {IIdentity} from '@essential-projects/iam_contracts';
import {ISocketClient, MessageEnvelope, OnConnectCallback} from '@essential-projects/websocket_contracts';
import * as WebSocket from 'ws';

type EventListenersCollection = {
  [eventType: string]: Array<Function>;
};

export class SocketClient implements ISocketClient {

  private _socket: WebSocket;
  private _endpointName: string;
  private _eventListeners: EventListenersCollection = {};

  constructor(socket: WebSocket, endpointName?: string) {
    this._socket = socket;
    this._endpointName = endpointName;
    this._startListening();
  }

  private get socket(): WebSocket {
    return this._socket;
  }

  private get endpointName(): string {
    return this._endpointName;
  }

  private _startListening(): void {
    this.socket.on('message', this._handleMessage.bind(this));
  }

  private _handleMessage(data: WebSocket.Data): void {

    let messageEnvelope: MessageEnvelope<any>;

    try {
      messageEnvelope = JSON.parse(data as string);
    } catch (error) {
      return;
    }

    const isNotMatchingEndpoint: boolean = !!this.endpointName && this.endpointName !== messageEnvelope.channelName;

    if (isNotMatchingEndpoint) {
      return;
    }

    const eventListeners: Array<Function> = this._eventListeners[messageEnvelope.eventType];

    if (!eventListeners) {
      return;
    }

    for (const eventListener of eventListeners) {
      eventListener(messageEnvelope.message);
    }
  }

  public dispose(): void {
    this.socket.removeAllListeners();
    this.socket.close();
  }

  public onConnect(callback: OnConnectCallback): void {
    this.socket.on('connection', (socket: WebSocket) => {
      // property 'upgradeReq' is missing in typings
      const identity: IIdentity = (socket as any).upgradeReq.identity;
      const socketClient: SocketClient = new SocketClient(socket, this.endpointName);
      callback(socketClient, identity);
    });
  }

  public onDisconnect(callback: Function): void {
      this.socket.on('disconnect', () => {
          callback();
      });
  }

  public on(eventType: string, callback: Function): void {
    let eventListeners: Array<Function> = this._eventListeners[eventType];

    if (!eventListeners) {
      eventListeners = this._eventListeners[eventType] = [];
    }

    eventListeners.push(callback);
  }

  public once(eventType: string, callback: Function): void {

    const innerCallback: Function = (data: WebSocket.Data): void => {
      this.off(eventType, innerCallback);
      callback(data);
    };

    this.on(eventType, innerCallback);
  }

  public off(eventType: string, callback: Function): void {
    const eventListeners: Array<Function> = this._eventListeners[eventType];

    if (!eventListeners) {
      return;
    }

    const listenerIndex: number = eventListeners.indexOf(callback);

    if (listenerIndex < 0) {
      return;
    }

    eventListeners.splice(listenerIndex, 1);
  }

  public emit<TMessage>(eventType: string, message: TMessage): void {
    if (this.socket.readyState === WebSocket.OPEN) {
      const payload: MessageEnvelope<TMessage> = new MessageEnvelope(message, eventType, this.endpointName);
      this.socket.send(JSON.stringify(payload));
    }
  }
}
