import { Component, inject, signal } from '@angular/core';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { HelloClient, HelloResponse } from '../server';

@Component({
  selector: 'app-hello',
  imports: [CardModule, ButtonModule],
  templateUrl: './hello.html',
  styleUrl: './hello.scss',
})
export class Hello {
  private readonly helloClient = inject(HelloClient);

  protected readonly message = signal<string | undefined>(undefined);
  protected readonly loading = signal(false);

  protected load(): void {
    this.loading.set(true);
    this.helloClient.get().subscribe({
      next: (response: HelloResponse) => {
        this.message.set(response.message);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
