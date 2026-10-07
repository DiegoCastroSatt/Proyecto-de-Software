import { VerVentasComponent } from './Componentes/ver-ventas/ver-ventas';
import { Routes } from '@angular/router';
import { ReservaComponent } from './Componentes/reserva/reserva';
import { BuscarReservas } from './Componentes/buscar-reservas/buscar-reservas';
import { ProductoComponent } from './Componentes/registro-producto/registro-producto';
import { RegistroVentaComponent } from './Componentes/registro-venta/registro-venta';
import { AdminLayoutComponent } from './Componentes/home_admin/admin';
import { ClienteLayoutComponent } from './Componentes/home_cliente/cliente';
import { InicioClienteComponent } from './Componentes/inicio-cliente/inicio-cliente';
import { GestionHorariosComponent } from './Componentes/gestion-horarios/gestion-horarios';
import { RegistroRecetaComponent } from './Componentes/registro-receta/registro-receta';
import { BuscarProductoCliente } from './Componentes/buscar-producto-cliente/buscar-producto-cliente';
import { BuscarProductoAdmin } from './Componentes/buscar-producto-admin/buscar-producto-admin';
import { PedidoComponent } from './Componentes/pedido/pedido';
import { CrearPedidoComponent } from './Componentes/crear-pedido/crear-pedido';
import { ClientesComponent } from './Componentes/clientes/clientes';
import { LoginAdmin } from './Componentes/login-admin/login-admin';
import { authAdminGuard } from './guards/auth-admin-guard';
import { DashboardComponent } from './Componentes/dashboard/dashboard';
import { ConsultarStock } from './Componentes/consultar-stock/consultar-stock';
import { HistorialGraduaciones } from './Componentes/historial-graduaciones/historial-graduaciones';

export const routes: Routes = [
  {
    path: '',
    component: ClienteLayoutComponent,
    children: [
      { path: '', redirectTo: 'inicio', pathMatch: 'full' },
      { path: 'inicio', component: InicioClienteComponent },
      { path: 'reservas', component: ReservaComponent },
      { path: 'productos', component: BuscarProductoCliente } 
    ]
  },

  {
    path: 'admin',
    children: [
      {path: '', component: LoginAdmin},
      {
        path: 'panel',
        component: AdminLayoutComponent,
        canActivate: [authAdminGuard],
        canActivateChild: [authAdminGuard],
        children: [
          { path: '', component: DashboardComponent },
          { path: 'horarios', component: GestionHorariosComponent },
          { path: 'agenda', component: BuscarReservas },
          { path: 'productos/nuevo', component: ProductoComponent },
          { path: 'clientes', component: ClientesComponent },
          { path: 'productos', component: BuscarProductoAdmin },
          { path: 'stock', component: ConsultarStock },
          { path: 'ventas', component: RegistroVentaComponent },
          { path: 'ventas/historial', component: VerVentasComponent },
          { path: 'recetas', component: RegistroRecetaComponent },
          { path: 'recetas/cliente', component: HistorialGraduaciones, data: { verRecetas: true } },
          { path: 'recetas/:id/editar', component: RegistroRecetaComponent },
          { path: 'pedido', component: PedidoComponent },
          { path: 'crear-pedido', component: CrearPedidoComponent },
          { path: 'historial-graduaciones', component: HistorialGraduaciones }
        ]
      }
    ]
  },

  { path: '**', redirectTo: '' }
];
