import { Routes } from '@angular/router';
import { Dashboard } from './dashboard/dashboard';
import { Review } from './review/review';
import { Habits } from './habits/habits';
import { Settings } from './settings/settings';
import { System } from './system/system';

export const routes: Routes = [
  { path: '', component: Dashboard },
  { path: 'today', component: Review },
  { path: 'habits', component: Habits },
  { path: 'settings', component: Settings },
  { path: 'settings/system', component: System },
];
