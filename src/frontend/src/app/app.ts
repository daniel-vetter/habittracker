import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { RippleModule } from 'primeng/ripple';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, RippleModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
