import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { PanelModule } from 'primeng/panel';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { HabitsClient, HabitResponse, HabitRequest, ScheduleKind, MissPolicy } from '../server';

interface HabitForm {
  name: string;
  notes?: string;
  scheduleKind: ScheduleKind;
  intervalDays?: number;
  weekdays: number[];
  missPolicy: MissPolicy;
}

@Component({
  selector: 'app-habits',
  imports: [
    FormsModule,
    TableModule,
    DialogModule,
    PanelModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    InputNumberModule,
    SelectModule,
    SelectButtonModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './habits.html',
  styleUrl: './habits.scss',
})
export class Habits implements OnInit {
  private readonly client = inject(HabitsClient);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly ScheduleKind = ScheduleKind;
  protected readonly MissPolicy = MissPolicy;

  protected readonly habits = signal<HabitResponse[]>([]);
  protected readonly dialogVisible = signal(false);
  private editingId: number | undefined;

  protected form: HabitForm = Habits.emptyForm();

  protected readonly scheduleOptions = [
    { label: 'Täglich', value: ScheduleKind.Daily },
    { label: 'Alle N Tage', value: ScheduleKind.EveryNDays },
    { label: 'Wochentage', value: ScheduleKind.Weekdays },
  ];

  protected readonly missPolicyOptions = [
    { label: 'Verfällt', value: MissPolicy.Lapse },
    { label: 'Nachholen', value: MissPolicy.CarryOver },
  ];

  // DayOfWeek: Sunday = 0 (matches backend (int)DayOfWeek). Displayed Mon–Sun.
  protected readonly weekdayOptions = [
    { label: 'Mo', value: 1 },
    { label: 'Di', value: 2 },
    { label: 'Mi', value: 3 },
    { label: 'Do', value: 4 },
    { label: 'Fr', value: 5 },
    { label: 'Sa', value: 6 },
    { label: 'So', value: 0 },
  ];

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.client.list().subscribe((habits) => this.habits.set(habits ?? []));
  }

  protected openNew(): void {
    this.editingId = undefined;
    this.form = Habits.emptyForm();
    this.dialogVisible.set(true);
  }

  protected openEdit(habit: HabitResponse): void {
    this.editingId = habit.id;
    this.form = {
      name: habit.name ?? '',
      notes: habit.notes ?? undefined,
      scheduleKind: habit.scheduleKind ?? ScheduleKind.Daily,
      intervalDays: habit.intervalDays ?? 2,
      weekdays: habit.weekdays ?? [],
      missPolicy: habit.missPolicy ?? MissPolicy.Lapse,
    };
    this.dialogVisible.set(true);
  }

  protected save(): void {
    const input = new HabitRequest({
      name: this.form.name.trim(),
      notes: this.form.notes?.trim() || undefined,
      scheduleKind: this.form.scheduleKind,
      intervalDays:
        this.form.scheduleKind === ScheduleKind.EveryNDays ? this.form.intervalDays : undefined,
      weekdays: this.form.scheduleKind === ScheduleKind.Weekdays ? this.form.weekdays : [],
      missPolicy: this.form.missPolicy,
    });

    const request$ =
      this.editingId === undefined
        ? this.client.create(input)
        : this.client.update(this.editingId, input);

    request$.subscribe(() => {
      this.dialogVisible.set(false);
      this.load();
    });
  }

  protected remove(habit: HabitResponse): void {
    this.confirmation.confirm({
      header: 'Task löschen',
      message: `„${habit.name}" wirklich löschen?`,
      acceptLabel: 'Löschen',
      rejectLabel: 'Abbrechen',
      accept: () => this.client.delete(habit.id!).subscribe(() => this.load()),
    });
  }

  protected scheduleSummary(habit: HabitResponse): string {
    switch (habit.scheduleKind) {
      case ScheduleKind.Daily:
        return 'Täglich';
      case ScheduleKind.EveryNDays:
        return `Alle ${habit.intervalDays} Tage`;
      case ScheduleKind.Weekdays: {
        const labels = (habit.weekdays ?? [])
          .map((d) => this.weekdayOptions.find((o) => o.value === d)?.label ?? '')
          .filter((l) => l);
        const policy = habit.missPolicy === MissPolicy.CarryOver ? ' (nachholbar)' : '';
        return `${labels.join(', ')}${policy}`;
      }
      default:
        return '';
    }
  }

  private static emptyForm(): HabitForm {
    return {
      name: '',
      notes: undefined,
      scheduleKind: ScheduleKind.Daily,
      intervalDays: 2,
      weekdays: [],
      missPolicy: MissPolicy.Lapse,
    };
  }
}
