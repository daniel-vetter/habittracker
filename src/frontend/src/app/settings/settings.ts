import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PanelModule } from 'primeng/panel';
import { TagModule } from 'primeng/tag';
import { UpdateState } from '../update-state';

@Component({
  selector: 'app-settings',
  imports: [RouterLink, PanelModule, TagModule],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings {
  protected readonly updateState = inject(UpdateState);
}
