import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { RippleModule } from 'primeng/ripple';
import { UpdateState } from './update-state';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, RippleModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly updateState = inject(UpdateState);

  ngOnInit(): void {
    void this.updateState.init();
  }
}
