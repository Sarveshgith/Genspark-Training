import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'app-order-timer',
  standalone: true,
  template: `
    <div class="flex flex-col items-end select-none">
      <span [class]="timerColorClass() + ' text-2xl font-bold font-mono-dm leading-none tracking-tight'">
        {{ formattedTime() }}
      </span>
      <span class="text-[8px] font-extrabold text-slate-500 tracking-widest uppercase mt-1">
        ELAPSED
      </span>
    </div>
  `
})
export class OrderTimer {
  public elapsedSeconds = input.required<number>();

  public formattedTime = computed(() => {
    const totalSecs = this.elapsedSeconds();
    const hours = Math.floor(totalSecs / 3600);
    const minutes = Math.floor((totalSecs % 3600) / 60);
    const seconds = totalSecs % 60;

    const pad = (num: number) => String(num).padStart(2, '0');

    if (hours > 0) {
      return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
    }
    return `${pad(minutes)}:${pad(seconds)}`;
  });

  public timerColorClass = computed(() => {
    const secs = this.elapsedSeconds();
    if (secs >= 600) {
      return 'text-[#DE350B] animate-pulse'; // overdue red
    }
    if (secs >= 300) {
      return 'text-[#F28500]'; // warning orange
    }
    return 'text-slate-300'; // normal slate
  });
}
