import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { KnobModule } from 'primeng/knob';
import { DashboardClient, DashboardDay } from '../server';

@Component({
  selector: 'app-dashboard',
  imports: [FormsModule, PanelModule, KnobModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly client = inject(DashboardClient);

  protected readonly days = signal<DashboardDay[]>([]);

  ngOnInit(): void {
    this.client.get().subscribe((response) => this.days.set(response.days ?? []));
  }

  protected hasTasks(day: DashboardDay): boolean {
    return (day.total ?? 0) > 0;
  }

  protected percent(day: DashboardDay): number {
    const total = day.total ?? 0;
    return total > 0 ? (day.completed ?? 0) / total : 0;
  }

  protected percentInt(day: DashboardDay): number {
    return Math.round(this.percent(day) * 100);
  }

  protected label(day: DashboardDay): string {
    return this.hasTasks(day) ? Math.round(this.percent(day) * 100) + '%' : '–';
  }

  protected color(day: DashboardDay): string {
    if (!this.hasTasks(day)) return 'var(--p-surface-200)';
    const hue = Math.round(this.percent(day) * 130); // 0 = rot, 130 = grün
    return `hsl(${hue}, 65%, 45%)`;
  }

  protected perfect(day: DashboardDay): boolean {
    return this.hasTasks(day) && this.percent(day) >= 1;
  }

  protected weekday(date: Date): string {
    return new Intl.DateTimeFormat('de-DE', { weekday: 'short' }).format(date);
  }

  protected dayNumber(date: Date): string {
    return new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: '2-digit' }).format(date);
  }
}
