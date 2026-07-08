import { Injectable, inject, signal } from "@angular/core";
import * as signalR from "@microsoft/signalr";
import { signalrUrl } from "../enviroment";
import { Observable, Subject, firstValueFrom } from "rxjs";
import { OrderModel } from "../models/order.model";
import { AuthService } from "./auth.service";

@Injectable({
    providedIn: "root"
})
export class SignalRService {
    private authService = inject(AuthService);

    private hubConnection!: signalR.HubConnection;
    private connectingPromise: Promise<void> | null = null;
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

    private adminAlertSubject = new Subject<any>();
    public get adminAlert$(): Observable<any> {
        return this.adminAlertSubject.asObservable();
    }

    private kitchenMessageSubject = new Subject<any>();
    public get kitchenMessage$(): Observable<any> {
        return this.kitchenMessageSubject.asObservable();
    }

    private floorMessageSubject = new Subject<any>();
    public get floorMessage$(): Observable<any> {
        return this.floorMessageSubject.asObservable();
    }

    private guestSessionEndedSubject = new Subject<void>();
    public get guestSessionEnded$(): Observable<void> {
        return this.guestSessionEndedSubject.asObservable();
    }

    public async connect(token: string): Promise<void> {
        if (this.isConnected()) {
            console.log("SignalR connection already active. Skipping connect.");
            return;
        }
        if (this.connectingPromise) {
            console.log("SignalR connection in progress. Returning existing connection promise.");
            return this.connectingPromise;
        }
        // Auto-refresh token if expired before starting the SignalR connection
        let currentToken = this.authService.getToken() || token;
        if (currentToken && this.authService.isTokenExpired(currentToken)) {
            console.log("Token is expired. Attempting refresh before connecting to SignalR...");
            try {
                const res = await firstValueFrom(this.authService.refreshToken());
                if (res && res.token) {
                    token = res.token;
                }
            } catch (refreshErr) {
                console.error("Failed to refresh token before connecting to SignalR:", refreshErr);
            }
        }

        this.connectingPromise = (async () => {
            try {
                this.hubConnection = new signalR.HubConnectionBuilder()
                    .withUrl(signalrUrl, {
                        accessTokenFactory: () => this.authService.getToken() || token
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
                throw err;
            } finally {
                this.connectingPromise = null;
            }
        })();

        return this.connectingPromise;
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

        this.hubConnection.on("ReceiveAdminAlert", (alert) => {
            console.log("Admin alert received:", alert);
            this.adminAlertSubject.next(alert);
        });

        this.hubConnection.on("GuestSessionEnded", () => {
            console.log("Guest session ended received");
            this.guestSessionEndedSubject.next();
        });

        this.hubConnection.on("ReceiveKitchenMessage", (notification) => {
            console.log("Kitchen message received:", notification);
            this.kitchenMessageSubject.next(notification);
        });

        this.hubConnection.on("ReceiveFloorMessage", (notification) => {
            console.log("Floor message received:", notification);
            this.floorMessageSubject.next(notification);
        });
    }

    public async sendKitchenMessage(type: string, payload: any): Promise<void> {
        if (this.hubConnection && this.isConnected()) {
            try {
                await this.hubConnection.invoke("SendKitchenMessage", type, payload);
            } catch (err) {
                console.error("Error sending kitchen message:", err);
                throw err;
            }
        } else {
            throw new Error("SignalR connection not active.");
        }
    }

    public async sendFloorMessage(type: string, tableId: number, payload: any): Promise<void> {
        if (this.hubConnection && this.isConnected()) {
            try {
                await this.hubConnection.invoke("SendFloorMessage", type, tableId, payload);
            } catch (err) {
                console.error("Error sending floor message:", err);
                throw err;
            }
        } else {
            throw new Error("SignalR connection not active.");
        }
    }

    public async sendAdminAlert(type: string, payload: any): Promise<void> {
        if (this.hubConnection && this.isConnected()) {
            try {
                await this.hubConnection.invoke("SendAdminAlert", type, payload);
            } catch (err) {
                console.error("Error sending admin alert:", err);
                throw err;
            }
        } else {
            throw new Error("SignalR connection not active.");
        }
    }



    public async guestCallWaiter(type: string): Promise<void> {
        if (this.hubConnection && this.isConnected()) {
            try {
                await this.hubConnection.invoke("GuestCallWaiter", type);
            } catch (err) {
                console.error("Error invoking GuestCallWaiter:", err);
                throw err;
            }
        } else {
            throw new Error("SignalR connection not active.");
        }
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