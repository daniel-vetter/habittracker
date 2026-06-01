import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { ReviewClient, ReviewItem, ToggleRequest } from '../server';

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

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.client.get().subscribe((response) => {
      this.items.set(response.items ?? []);
      this.logicalDate.set(response.logicalDate);
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
