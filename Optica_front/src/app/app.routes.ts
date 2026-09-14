import { Routes } from '@angular/router';
import { ReservaComponent } from './Componentes/reserva/reserva';
import { PedidoComponent } from './Componentes/pedido/pedido';

export const routes: Routes = [
    { path: '', redirectTo: 'pedido', pathMatch: 'full' },
    { path: 'reserva', component: ReservaComponent },
    { path: 'pedido', component: PedidoComponent }
];
