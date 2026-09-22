import { Routes } from '@angular/router';
import { ReservaComponent } from './Componentes/reserva/reserva';
import { ClientesComponent } from './Componentes/clientes/clientes';
import { ProductoComponent } from './Componentes/registro-producto/registro-producto';
import { RegistroVentaComponent } from './Componentes/registro-venta/registro-venta';
import { AdminLayoutComponent } from './Componentes/home_admin/admin';
import { ClienteComponent } from './Componentes/home_cliente/cliente';
import { EnTrabajoComponent } from './Componentes/en-trabajo/en-trabajo';
import { GestionHorariosComponent } from './Componentes/gestion-horarios/gestion-horarios';
import { RegistroRecetaComponent } from './Componentes/registro-receta/registro-receta';
import { BuscarProductoCliente } from './Componentes/buscar-producto-cliente/buscar-producto-cliente';
import { BuscarProductoAdmin } from './Componentes/buscar-producto-admin/buscar-producto-admin';

export const routes: Routes = [
  {
    path: '',
    component: ClienteComponent,
    children: [
      { path: '', redirectTo: 'inicio', pathMatch: 'full' },
      { path: 'inicio', component: EnTrabajoComponent, data: { titulo: 'Inicio' } },
      { path: 'reservas', component: ReservaComponent },
      { path: 'productos', component: BuscarProductoCliente }
      // <-- Se eliminó 'clientes' de la vista pública
    ]
  },

  {
    path: 'admin',
    component: AdminLayoutComponent,
    children: [
      { path: '', component: EnTrabajoComponent, data: { titulo: 'Dashboard' } },
      { path: 'horarios', component: GestionHorariosComponent },
      { path: 'clientes', component: ClientesComponent }, // <-- Solo accesible aquí: /admin/clientes
      { path: 'productos/nuevo', component: ProductoComponent },
      { path: 'productos', component: BuscarProductoAdmin },
      { path: 'ventas', component: RegistroVentaComponent },
      { path: 'recetas', component: RegistroRecetaComponent }
    ]
  },

  { path: '**', redirectTo: '' }
];