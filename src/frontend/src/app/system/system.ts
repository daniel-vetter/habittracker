import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PanelModule } from 'primeng/panel';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { lastValueFrom } from 'rxjs';
import {
  SystemClient,
  AppDetailsResponse,
  UpdateLogResponse,
  SetAutoUpdateRequest,
} from '../server';
import { UpdateState } from '../update-state';

@Component({
  selector: 'app-system',
  imports: [
    FormsModule,
    DatePipe,
    PanelModule,
    ButtonModule,
    ToggleSwitchModule,
    DialogModule,
    TagModule,
  ],
  templateUrl: './system.html',
  styleUrl: './system.scss',
})
export class System implements OnInit {
  private readonly systemClient = inject(SystemClient);
  protected readonly updateState = inject(UpdateState);

  protected readonly appDetails = signal<AppDetailsResponse | undefined>(undefined);
  protected readonly isChecking = signal(false);
  protected readonly updateInProgress = signal(false);

  protected readonly logsVisible = signal(false);
  protected readonly logs = signal<UpdateLogResponse[]>([]);

  async ngOnInit(): Promise<void> {
    this.appDetails.set(await lastValueFrom(this.systemClient.getAppDetails()));
    await this.updateState.refresh();
  }

  protected async check(): Promise<void> {
    this.isChecking.set(true);
    try {
      await lastValueFrom(this.systemClient.checkForUpdate());
    } catch {
      // A failed pull still leaves a usable local comparison in the status.
    } finally {
      await this.updateState.refresh();
      this.isChecking.set(false);
    }
  }

  protected async setAutoUpdate(enabled: boolean): Promise<void> {
    await lastValueFrom(this.systemClient.setAutoUpdate(new SetAutoUpdateRequest({ enabled })));
    await this.updateState.refresh();
  }

  protected async apply(): Promise<void> {
    this.updateInProgress.set(true);
    try {
      await lastValueFrom(this.systemClient.applyUpdate());
      this.pollUntilRestarted();
    } catch {
      this.updateInProgress.set(false);
    }
  }

  protected async openLogs(): Promise<void> {
    this.logs.set(await lastValueFrom(this.systemClient.getUpdateLogs()));
    this.logsVisible.set(true);
  }

  private pollUntilRestarted(): void {
    const poll = setInterval(async () => {
      try {
        await lastValueFrom(this.systemClient.getUpdateStatus());
        clearInterval(poll);
        window.location.reload();
      } catch {
        // Server is still restarting — keep polling.
      }
    }, 2000);
  }
}
