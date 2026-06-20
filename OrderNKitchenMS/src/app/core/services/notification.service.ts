import { Injectable } from "@angular/core";
import { Subject } from "rxjs";

//Types
export type ToastType = "success" | "error" | "info" | "warning";

//Model for toast notifications
export interface ToastModel {
    id: number;
    message: string;
    type: ToastType;
}

//Main Component
@Injectable({
    providedIn: "root"
})
export class NotificationService {

    private _toast = new Subject<ToastModel>();
    public toast$ = this._toast.asObservable();
    private _toastId = 0;

    success(message: string) {this._toast.next({ id: this._toastId++, message, type: "success" });}
    error(message: string) {this._toast.next({ id: this._toastId++, message, type: "error" });}
    info(message: string) {this._toast.next({ id: this._toastId++, message, type: "info" });}
    warning(message: string) {this._toast.next({ id: this._toastId++, message, type: "warning" });}
}