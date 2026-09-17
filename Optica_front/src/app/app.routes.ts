import { Routes } from '@angular/router';
import { ReservaComponent } from './Componentes/reserva/reserva';
import { ProductoComponent } from './Componentes/registro-producto/registro-producto';
import { RegistroVentaComponent } from './Componentes/registro-venta/registro-venta';
import { AdminLayoutComponent } from './Componentes/home_admin/admin';
import { ClienteComponent } from './Componentes/home_cliente/cliente';
import { EnTrabajoComponent } from './Componentes/en-trabajo/en-trabajo';

export const routes: Routes = [
  {
    path: '',
    component: ClienteComponent,
    children: [
      { path: '', redirectTo: 'inicio', pathMatch: 'full' },
      { path: 'inicio', component: EnTrabajoComponent, data: { titulo: 'Inicio' } },
      { path: 'reservas', component: ReservaComponent },
      { path: 'productos', component: EnTrabajoComponent, data: { titulo: 'Productos' } }
    ]
  },

  {
    path: 'admin',
    component: AdminLayoutComponent,
    children: [
      { path: '', component: EnTrabajoComponent, data: { titulo: 'Dashboard' } },
      { path: 'productos', component: ProductoComponent },
      { path: 'ventas', component: RegistroVentaComponent }
    ]
  },

  { path: '**', redirectTo: '' }
];