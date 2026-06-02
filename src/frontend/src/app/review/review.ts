import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { ReviewClient, ReviewItem, ToggleRequest, PastDay } from '../server';

@Component({
  selector: 'app-review',
  imports: [FormsModule, PanelModule, CheckboxModule, ButtonModule, TagModule, ConfirmDialogModule],
  providers: [ConfirmationService],
  templateUrl: './review.html',
  styleUrl: './review.scss',
})
export class Review implements OnInit {
  private readonly client = inject(ReviewClient);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly items = signal<ReviewItem[]>([]);
  protected readonly logicalDate = signal<Date | undefined>(undefined);
  protected readonly pastDays = signal<PastDay[]>([]);

  protected readonly title = computed(() => {
    const date = this.logicalDate();
    if (!date) {
      return 'Heute';
    }

    // Aktuelles Datum, 6 Stunden zurück (langer Tag), auf Mitternacht normalisiert.
    const now = new Date();
    now.setHours(now.getHours() - 6, 0, 0, 0);
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const day = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    const diffDays = Math.round((day.getTime() - today.getTime()) / 86_400_000);
    switch (diffDays) {
      case 0:
        return 'Heute';
      case -1:
        return 'Gestern';
      case 1:
        return 'Morgen';
      default:
        return new Intl.DateTimeFormat('de-DE', { weekday: 'long' }).format(day);
    }
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.client.get().subscribe((response) => {
      this.items.set(response.items ?? []);
      this.logicalDate.set(response.logicalDate);
      this.pastDays.set(response.pastDays ?? []);
    });
  }

  protected toggle(item: ReviewItem, completed: boolean): void {
    this.client
      .toggle(new ToggleRequest({ habitId: item.habitId!, completed }))
      .subscribe(() => this.load());
  }

  protected endDay(): void {
    this.confirmation.confirm({
      header: 'Tag beenden',
      message: 'Den aktuellen Tag abschließen und zum nächsten wechseln?',
      acceptLabel: 'Tag beenden',
      rejectLabel: 'Abbrechen',
      accept: () => this.client.endDay().subscribe(() => this.load()),
    });
  }

  protected formatDate(date: Date): string {
    return new Intl.DateTimeFormat('de-DE', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    }).format(date);
  }
}
