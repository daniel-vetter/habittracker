import { Routes } from '@angular/router';
import { Review } from './review/review';
import { Habits } from './habits/habits';
import { Settings } from './settings/settings';
import { System } from './system/system';

export const routes: Routes = [
  { path: '', component: Review },
  { path: 'habits', component: Habits },
  { path: 'settings', component: Settings },
  { path: 'settings/system', component: System },
];
