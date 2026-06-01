import { Routes } from '@angular/router';
import { Review } from './review/review';
import { Habits } from './habits/habits';

export const routes: Routes = [
  { path: '', component: Review },
  { path: 'habits', component: Habits },
];
