import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
  duration: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  public toasts = signal<Toast[]>([]);
  private nextId = 1;

  public show(message: string, type: 'success' | 'error' | 'info' = 'info', duration: number = 10000): void {
    const id = this.nextId++;
    const newToast: Toast = { id, message, type, duration };
    
    this.toasts.update(current => [...current, newToast]);

    if (duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, duration);
    }
  }

  public success(message: string, duration: number = 10000): void {
    this.show(message, 'success', duration);
  }

  public error(message: string, duration: number = 10000): void {
    this.show(message, 'error', duration);
  }

  public info(message: string, duration: number = 10000): void {
    this.show(message, 'info', duration);
  }

  public remove(id: number): void {
    this.toasts.update(current => current.filter(t => t.id !== id));
  }
}
