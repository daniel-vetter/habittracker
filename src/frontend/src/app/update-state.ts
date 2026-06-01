import { computed, inject, Injectable, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { SystemClient, UpdateStatusResponse } from './server';

@Injectable({ providedIn: 'root' })
export class UpdateState {
  private readonly systemClient = inject(SystemClient);
  private readonly _status = signal<UpdateStatusResponse | undefined>(undefined);

  readonly status = this._status.asReadonly();
  readonly isUpdateAvailable = computed(() => this._status()?.isUpdateAvailable ?? false);

  async init(): Promise<void> {
    await this.refresh();
  }

  async refresh(): Promise<void> {
    try {
      const status = await lastValueFrom(this.systemClient.getUpdateStatus());
      this._status.set(status);
    } catch {
      // Status is best-effort — keep the previous value on failure.
    }
  }
}
