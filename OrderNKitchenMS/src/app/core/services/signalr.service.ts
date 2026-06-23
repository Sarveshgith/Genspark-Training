import { Injectable } from "@angular/core";
import * as signalR from "@microsoft/signalr";
import { signalrUrl } from "../enviroment";
import { Observable, Subject } from "rxjs";
import { OrderModel } from "../models/order.model";

@Injectable({
    providedIn: "root"
})
export class SignalRService {

    private hubConnection!: signalR.HubConnection;

    private newOrderSubject = new Subject<OrderModel>();
    public get newOrder$(): Observable<OrderModel> {
        return this.newOrderSubject.asObservable();
    }

    private orderUpdateSubject = new Subject<any>();
    public get orderUpdate$(): Observable<any> {
        return this.orderUpdateSubject.asObservable();
    }

    public async connect(token: string): Promise<void> {
        try {

            this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(signalrUrl, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

            this.registerSignalREvents();

            await this.hubConnection.start();
            console.log("SignalR Connected.");

        } catch (err) {
            console.error("Error connecting to SignalR:", err);
        }
    }

    private registerSignalREvents(): void {

        this.hubConnection.on("ReceiveNewOrder", (order) => {
            console.log("New order received:", order);
            this.newOrderSubject.next(order);
        });

        this.hubConnection.on("ReceiveOrderUpdate", (trackingInfo) => {
            console.log("Order update received:", trackingInfo);
            this.orderUpdateSubject.next(trackingInfo);
        });
    }

    public async disconnect(): Promise<void> {
        try {
            if (this.hubConnection) {
                await this.hubConnection.stop();
                console.log("SignalR Disconnected.");
            }
        } catch (err) {
            console.error("Error disconnecting from SignalR:", err);
        }
    }
}