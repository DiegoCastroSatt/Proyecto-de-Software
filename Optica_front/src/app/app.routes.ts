import { Routes } from '@angular/router';
import { ProductoComponent } from './Componentes/registro-producto/registro-producto';
import { RegistroVentaComponent } from './Componentes/registro-venta/registro-venta';
import { ReservaComponent } from './Componentes/reserva/reserva';
import { PedidoComponent } from './Componentes/pedido/pedido';

export const routes: Routes = [
    { path: '', component: ReservaComponent },
    { path: 'registro-producto', component: ProductoComponent },
    { path: 'ventas', component: RegistroVentaComponent },
    { path: 'pedido', component: PedidoComponent }
];
