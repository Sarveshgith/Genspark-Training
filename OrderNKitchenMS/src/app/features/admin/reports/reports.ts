// @feature Admin | Reports & Analytics | Dashboard component summarizing daily sales, category performance, kitchen SLAs, item preparation durations, and hourly transaction loads.
import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, switchMap } from 'rxjs';
import { OrderService } from '../../../core/services/order.service';
import { OrderModel } from '../../../core/models/order.model';
import { CategoryPerformanceModel, DailyRevenueModel, KitchenSlaModel, RangeRevenueModel, TopSellingItemModel } from '../../../core/models/reports.model';
import { ReportService } from '../../../core/services/report.service';
import { BillService } from '../../../core/services/bill.service';
import { BillDto } from '../../../core/models/bill.model';

interface ItemPrepTime {
  itemName: string;
  avgMinutes: number;
  completedCount: number;
}

interface HourlyChartBar {
  hourLabel: string;
  count: number;
  percentage: number;
}

interface TableTurnoverStat {
  tableNumber: number;
  count: number;
  totalRevenue: number;
  avgOrderValue: number;
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.html',
  styleUrl: './reports.css'
})
export class ReportsComponent implements OnInit {
  private reportService = inject(ReportService);
  private orderService = inject(OrderService);
  private billService = inject(BillService);
  private destroyRef = inject(DestroyRef);

  // Fetch trigger — switchMap ensures in-flight requests are cancelled on rapid re-trigger
  private readonly fetchTrigger$ = new Subject<void>();

  // Filter States (Interactive Date Range)
  public fromDate = signal<string>(new Date(Date.now() - 6 * 24 * 3600 * 1000).toISOString().substring(0, 10));
  public toDate = signal<string>(new Date().toISOString().substring(0, 10));
  public Math = Math;

  // Report States
  public dailyRevenue = signal<DailyRevenueModel | null>(null);
  public rangeRevenue = signal<RangeRevenueModel[]>([]);
  public topSellingItemsMaster = signal<TopSellingItemModel[]>([]);
  public categoryPerformance = signal<CategoryPerformanceModel[]>([]);
  public kitchenSla = signal<KitchenSlaModel | null>(null);
  public rawOrders = signal<OrderModel[]>([]);
  public rawBills = signal<BillDto[]>([]);
  public isLoading = signal<boolean>(false);
  public errorMessage = signal<string | null>(null);

  // ─── Computed Derivations ────────────────────────────────────────────────────

  // Filter raw orders by the selected date range
  public filteredOrders = computed<OrderModel[]>(() => {
    const list = this.rawOrders();
    const fromStr = this.fromDate();
    const toStr = this.toDate();
    if (!fromStr || !toStr) return list;

    const start = new Date(fromStr);
    start.setHours(0, 0, 0, 0);
    const end = new Date(toStr);
    end.setHours(23, 59, 59, 999);

    return list.filter(order => {
      if (!order.createdAt) return false;
      const time = new Date(order.createdAt).getTime();
      return time >= start.getTime() && time <= end.getTime();
    });
  });

  // Filter raw bills by the full selected date range
  public filteredBills = computed<BillDto[]>(() => {
    const list = this.rawBills();
    const fromStr = this.fromDate();
    const toStr = this.toDate();
    if (!fromStr || !toStr) return [];

    const start = new Date(fromStr);
    start.setHours(0, 0, 0, 0);
    const end = new Date(toStr);
    end.setHours(23, 59, 59, 999);

    return list.filter(bill => {
      if (!bill.createdAt) return false;
      const time = new Date(bill.createdAt).getTime();
      return time >= start.getTime() && time <= end.getTime();
    });
  });

  public billsGeneratedCount = computed(() => this.filteredBills().length);
  public billsPaidCount = computed(() =>
    this.filteredBills().filter(b => b.statusName?.toLowerCase() === 'paid').length
  );
  public billsFailedCount = computed(() =>
    this.filteredBills().filter(b => b.statusName?.toLowerCase() === 'failed').length
  );

  // 1. Average prep time by item (from filtered orders — order lifecycle approximation)
  public avgPrepTimesByItem = computed<ItemPrepTime[]>(() => {
    const list = this.filteredOrders();
    const map = new Map<string, { totalTime: number; count: number }>();

    list.forEach(order => {
      if ((order.status === 4 || order.status === 5) && order.completedAt && order.createdAt) {
        const start = new Date(order.createdAt).getTime();
        const end = new Date(order.completedAt).getTime();
        const durationMinutes = (end - start) / (1000 * 60);

        if (durationMinutes > 0 && durationMinutes < 480) { // Guard: cap at 8h to filter stale sessions
          order.orderItems.forEach(item => {
            const name = item.menuItemName;
            const current = map.get(name) || { totalTime: 0, count: 0 };
            map.set(name, {
              totalTime: current.totalTime + durationMinutes,
              count: current.count + 1
            });
          });
        }
      }
    });

    const result: ItemPrepTime[] = [];
    map.forEach((value, key) => {
      result.push({
        itemName: key,
        avgMinutes: Math.round((value.totalTime / value.count) * 10) / 10,
        completedCount: value.count
      });
    });

    return result.sort((a, b) => a.avgMinutes - b.avgMinutes);
  });

  // 2. Orders grouped by hour chart data (from filtered orders)
  public hourlyOrdersData = computed<HourlyChartBar[]>(() => {
    const list = this.filteredOrders();
    const hours = Array(24).fill(0);

    list.forEach(order => {
      if (order.createdAt) {
        const date = new Date(order.createdAt);
        const hour = date.getHours();
        if (hour >= 0 && hour < 24) {
          hours[hour]++;
        }
      }
    });

    const maxCount = Math.max(...hours, 1);
    const chartBars: HourlyChartBar[] = [];
    for (let h = 8; h <= 23; h++) {
      const label = `${h.toString().padStart(2, '0')}:00`;
      chartBars.push({
        hourLabel: label,
        count: hours[h],
        percentage: Math.round((hours[h] / maxCount) * 100)
      });
    }
    return chartBars;
  });

  // 3. Table Turnover Rates Analysis (from filtered orders)
  public tableTurnoverStats = computed<TableTurnoverStat[]>(() => {
    const list = this.filteredOrders();
    const map = new Map<number, { count: number; totalRevenue: number }>();

    list.forEach(order => {
      if (order.status === 4 || order.status === 5) {
        const tableNum = order.tableNumber;
        const current = map.get(tableNum) || { count: 0, totalRevenue: 0 };
        map.set(tableNum, {
          count: current.count + 1,
          totalRevenue: current.totalRevenue + order.totalAmount
        });
      }
    });

    const result: TableTurnoverStat[] = [];
    map.forEach((value, key) => {
      result.push({
        tableNumber: key,
        count: value.count,
        totalRevenue: value.totalRevenue,
        avgOrderValue: value.count > 0 ? value.totalRevenue / value.count : 0
      });
    });

    return result.sort((a, b) => b.count - a.count);
  });

  // 4. Dynamic Top Selling Items (from filtered orders — date-range-aware)
  public topSellingItems = computed<TopSellingItemModel[]>(() => {
    const list = this.filteredOrders();
    const map = new Map<string, { qty: number; revenue: number }>();

    list.forEach(order => {
      if (order.status === 4 || order.status === 5) {
        order.orderItems.forEach(item => {
          const name = item.menuItemName;
          const current = map.get(name) || { qty: 0, revenue: 0 };
          map.set(name, {
            qty: current.qty + item.quantity,
            revenue: current.revenue + (item.quantity * item.unitPrice)
          });
        });
      }
    });

    // Use master list for category lookup
    const catMap = new Map<string, string>();
    this.topSellingItemsMaster().forEach(item => {
      catMap.set(item.itemName, item.category);
    });

    const result: TopSellingItemModel[] = [];
    map.forEach((value, key) => {
      result.push({
        itemName: key,
        category: catMap.get(key) || 'Food',
        totalQtySold: value.qty,
        totalRevenue: value.revenue
      });
    });

    return result.sort((a, b) => b.totalQtySold - a.totalQtySold).slice(0, 8);
  });

  // 5. SVG Points for Sales Trend Line
  public salesTrendPoints = computed(() => {
    const data = this.rangeRevenue();
    if (data.length === 0) return [];

    const maxRev = Math.max(...data.map(d => Number(d.totalRevenue)), 1);
    const width = 500;
    const height = 160;
    const paddingX = 40;
    const paddingY = 20;

    return data.map((item, idx) => {
      const x = paddingX + idx * ((width - paddingX * 2) / Math.max(data.length - 1, 1));
      const y = (height - paddingY) - (Number(item.totalRevenue) / maxRev) * (height - paddingY * 2);
      const dateObj = new Date(item.date);
      return {
        x,
        y,
        label: dateObj.toLocaleDateString(undefined, { weekday: 'short', month: 'numeric', day: 'numeric' }),
        revenue: Number(item.totalRevenue),
        orders: item.totalOrders
      };
    });
  });

  public salesLinePointsString = computed(() =>
    this.salesTrendPoints().map(p => `${p.x},${p.y}`).join(' ')
  );

  public salesAreaPathString = computed(() => {
    const pts = this.salesTrendPoints();
    if (pts.length === 0) return '';
    const first = pts[0];
    const last = pts[pts.length - 1];
    const linePath = pts.map((p, idx) => `${idx === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
    return `${linePath} L ${last.x} 140 L ${first.x} 140 Z`;
  });

  // 6. SVG Points for Hourly Transaction Load Line
  public hourlyChartPoints = computed(() => {
    const bars = this.hourlyOrdersData();
    if (bars.length === 0) return [];

    const maxCount = Math.max(...bars.map(b => b.count), 1);
    const width = 500;
    const height = 160;
    const paddingX = 30;
    const paddingY = 20;

    return bars.map((bar, idx) => {
      const x = paddingX + idx * ((width - paddingX * 2) / Math.max(bars.length - 1, 1));
      const y = (height - paddingY) - (bar.count / maxCount) * (height - paddingY * 2);
      return { x, y, label: bar.hourLabel, count: bar.count };
    });
  });

  public hourlyLinePointsString = computed(() =>
    this.hourlyChartPoints().map(p => `${p.x},${p.y}`).join(' ')
  );

  public hourlyAreaPathString = computed(() => {
    const pts = this.hourlyChartPoints();
    if (pts.length === 0) return '';
    const first = pts[0];
    const last = pts[pts.length - 1];
    const linePath = pts.map((p, idx) => `${idx === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
    return `${linePath} L ${last.x} 140 L ${first.x} 140 Z`;
  });

  // 7. Kitchen SLA SVG ring values
  public readonly SLA_RING_CIRCUMFERENCE = 2 * Math.PI * 44; // r=44

  public slaRingOffset = computed(() => {
    const pct = this.kitchenSla()?.slaPercentage ?? 0;
    return this.SLA_RING_CIRCUMFERENCE * (1 - Math.min(pct, 100) / 100);
  });

  public slaRingColor = computed(() => {
    const pct = this.kitchenSla()?.slaPercentage ?? 0;
    if (pct >= 80) return '#22c55e';   // green
    if (pct >= 60) return '#f59e0b';   // amber
    return '#ef4444';                   // red
  });

  // ─── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Wire the trigger through switchMap: cancels any previous in-flight forkJoin
    this.fetchTrigger$.pipe(
      switchMap(() => {
        const fromStr = this.fromDate();
        const toStr = this.toDate();
        this.isLoading.set(true);
        this.errorMessage.set(null);

        return forkJoin({
          daily: this.reportService.getDailyRevenue(toStr),
          range: this.reportService.getRangeRevenue(fromStr, toStr),
          topItems: this.reportService.getTopSellingItems(20),
          category: this.reportService.getCategoryPerformance(fromStr, toStr),
          sla: this.reportService.getKitchenSla(fromStr, toStr),
          orders: this.orderService.getOrders({ pageNumber: 1, pageSize: 2000 }),
          bills: this.billService.getAllBills()
        });
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (results) => {
        this.dailyRevenue.set(results.daily);
        this.rangeRevenue.set(results.range);
        this.topSellingItemsMaster.set(results.topItems);
        this.categoryPerformance.set(results.category);
        this.kitchenSla.set(results.sla);
        this.rawOrders.set(results.orders);
        this.rawBills.set(results.bills);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Reports fetch error:', err);
        this.isLoading.set(false);
        this.errorMessage.set('Failed to load report data. Please check the date range and try again.');
      }
    });

    this.fetchTrigger$.next();
  }

  public onRangeChange(fromVal: string, toVal: string): void {
    // Guard: prevent fetching when range is invalid
    if (fromVal && toVal && new Date(fromVal) > new Date(toVal)) {
      this.errorMessage.set('Start date cannot be after end date.');
      return;
    }
    this.errorMessage.set(null);
    this.fromDate.set(fromVal);
    this.toDate.set(toVal);
    this.fetchTrigger$.next();
  }

  // Export top selling items as CSV
  public exportCsv(): void {
    const data = this.topSellingItems();
    if (data.length === 0) return;

    let csvContent = 'data:text/csv;charset=utf-8,';
    csvContent += 'Item Name,Category,Qty Sold,Total Revenue\r\n';

    data.forEach(item => {
      const row = `"${item.itemName.replace(/"/g, '""')}","${item.category}",${item.totalQtySold},${item.totalRevenue}`;
      csvContent += row + '\r\n';
    });

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `Ambrosia_Top_Selling_Items_${this.fromDate()}_to_${this.toDate()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  public printReport(): void {
    window.print();
  }
}
