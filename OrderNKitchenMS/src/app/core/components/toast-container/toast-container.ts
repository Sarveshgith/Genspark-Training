import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.css'
})
export class ToastContainerComponent {
  public toastService = inject(ToastService);

  public getToastClasses(toast: Toast): string {
    switch (toast.type) {
      case 'success':
        return 'bg-emerald-50 border-emerald-200 text-emerald-800 shadow-emerald-100';
      case 'error':
        return 'bg-red-50 border-red-200 text-red-800 shadow-red-100';
      case 'info':
      default:
        return 'bg-[#FFF9EB] border-[#FFE8B3] text-[#B37822] shadow-[#FFF9EB]';
    }
  }

  public getIcon(type: 'success' | 'error' | 'info'): string {
    switch (type) {
      case 'success':
        return '✓';
      case 'error':
        return '⚠️';
      case 'info':
      default:
        return 'ℹ️';
    }
  }

  public dismiss(id: number): void {
    this.toastService.remove(id);
  }
}
