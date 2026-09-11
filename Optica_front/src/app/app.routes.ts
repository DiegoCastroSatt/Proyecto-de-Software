import { Routes } from '@angular/router';
import { ReservaComponent } from './components/reserva/reserva';
import { ClientesComponent } from './components/clientes/clientes';

export const routes: Routes = [
  { path: '', redirectTo: 'reservas', pathMatch: 'full' },
  { path: 'reservas', component: ReservaComponent },
  { path: 'clientes', component: ClientesComponent }
];
