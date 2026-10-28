import { Routes } from '@angular/router';
import { TicketsListComponent } from './components/tickets-list/tickets-list.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'tickets',
    pathMatch: 'full'
  },
  {
    path: 'tickets',
    component: TicketsListComponent
  },
  {
    path: '**',
    redirectTo: 'tickets'
  }
];