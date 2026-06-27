import { Injectable, signal } from "@angular/core";
import * as signalR from "@microsoft/signalr";
import { signalrUrl } from "../enviroment";
import { Observable, Subject } from "rxjs";
import { OrderModel } from "../models/order.model";

@Injectable({
    providedIn: "root"
})
export class SignalRService {

    private hubConnection!: signalR.HubConnection;
    public isConnected = signal<boolean>(false);

    private newOrderSubject = new Subject<OrderModel>();
    public get newOrder$(): Observable<OrderModel> {
        return this.newOrderSubject.asObservable();
    }

    private orderUpdateSubject = new Subject<any>();
    public get orderUpdate$(): Observable<any> {
        return this.orderUpdateSubject.asObservable();
    }

    private tablesUpdatedSubject = new Subject<void>();
    public get tablesUpdated$(): Observable<void> {
        return this.tablesUpdatedSubject.asObservable();
    }

    private billGeneratedSubject = new Subject<any>();
    public get billGenerated$(): Observable<any> {
        return this.billGeneratedSubject.asObservable();
    }

    private billPaidSubject = new Subject<any>();
    public get billPaid$(): Observable<any> {
        return this.billPaidSubject.asObservable();
    }

    public async connect(token: string): Promise<void> {
        if (this.hubConnection && this.isConnected()) {
            console.log("SignalR connection already active. Skipping connect.");
            return;
        }
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
            this.isConnected.set(true);

            this.hubConnection.onclose(() => this.isConnected.set(false));
            this.hubConnection.onreconnecting(() => this.isConnected.set(false));
            this.hubConnection.onreconnected(() => this.isConnected.set(true));

        } catch (err) {
            console.error("Error connecting to SignalR:", err);
            this.isConnected.set(false);
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

        this.hubConnection.on("ReceiveTableStateUpdate", () => {
            console.log("Table state update signal received");
            this.tablesUpdatedSubject.next();
        });

        this.hubConnection.on("bill_generated", (bill) => {
            console.log("Bill generated received:", bill);
            this.billGeneratedSubject.next(bill);
        });

        this.hubConnection.on("bill_paid", (bill) => {
            console.log("Bill paid received:", bill);
            this.billPaidSubject.next(bill);
        });
    }

    public async disconnect(): Promise<void> {
        try {
            if (this.hubConnection) {
                await this.hubConnection.stop();
                console.log("SignalR Disconnected.");
            }
            this.isConnected.set(false);
        } catch (err) {
            console.error("Error disconnecting from SignalR:", err);
        }
    }
}