import { CrearMarcoComponent } from './Componentes/crear-marco/crear-marco';
import { GestionRostrosComponent } from './Componentes/gestion-rostros/gestion-rostros';
import { GestionMarcosComponent } from './Componentes/gestion-marcos/gestion-marcos';
import { CambiarContrasenaComponent } from './Componentes/cambiar-contrasena/cambiar-contrasena';
import { TrabajadoresComponent } from './Componentes/trabajadores/trabajadores';
import {
  InicioPersonalComponent,
  SinAccesoComponent,
} from './Componentes/inicio-personal/inicio-personal';
import { ProbadorMarcosComponent } from './Componentes/probador-marcos/probador-marcos';
import { RecomendadorMarcosComponent } from './Componentes/recomendador-marcos/recomendador-marcos';
import { VerVentasComponent } from './Componentes/ver-ventas/ver-ventas';
import { Routes } from '@angular/router';
import { ReservaComponent } from './Componentes/reserva/reserva';
import { BuscarReservas } from './Componentes/buscar-reservas/buscar-reservas';
import { ProductoComponent } from './Componentes/registro-producto/registro-producto';
import { RegistroVentaComponent } from './Componentes/registro-venta/registro-venta';
import { AdminLayoutComponent } from './Componentes/home_admin/admin';
import { ClienteLayoutComponent } from './Componentes/home_cliente/cliente';
import { PortalClienteComponent } from './Componentes/portal-cliente/portal-cliente';
import { InicioClienteComponent } from './Componentes/inicio-cliente/inicio-cliente';
import { GestionHorariosComponent } from './Componentes/gestion-horarios/gestion-horarios';
import { RegistroRecetaComponent } from './Componentes/registro-receta/registro-receta';
import { BuscarProductoCliente } from './Componentes/buscar-producto-cliente/buscar-producto-cliente';
import { BuscarProductoAdmin } from './Componentes/buscar-producto-admin/buscar-producto-admin';
import { PedidoComponent } from './Componentes/pedido/pedido';
import { CrearPedidoComponent } from './Componentes/crear-pedido/crear-pedido';
import { DetallePedidoComponent } from './Componentes/detalle-pedido/detalle-pedido';
import { ClientesComponent } from './Componentes/clientes/clientes';
import { authAdminGuard } from './guards/auth-admin-guard';
import { LoginCliente } from './Componentes/login-cliente/login-cliente';
import { DashboardComponent } from './Componentes/dashboard/dashboard';
import { ConsultarStock } from './Componentes/consultar-stock/consultar-stock';
import { HistorialGraduaciones } from './Componentes/historial-graduaciones/historial-graduaciones';
import { EnTrabajoComponent } from './Componentes/en-trabajo/en-trabajo';
import { authClienteGuard } from './guards/auth-cliente-guard';
import { RestablecerContrasenaComponent } from './Componentes/restablecer-contrasena/restablecer-contrasena';
import { HistorialPedidosComponent } from './Componentes/historial-pedidos/historial-pedidos';
import { MisRecetas } from './Componentes/mis-recetas/mis-recetas';
import { ConfirmarCuentaClienteComponent } from './Componentes/confirmar-cuenta-cliente/confirmar-cuenta-cliente';

export const routes: Routes = [
  {
    path: '',
    component: ClienteLayoutComponent,
    children: [
      { path: '', redirectTo: 'inicio', pathMatch: 'full' },
      { path: 'logincliente', redirectTo: 'cliente/login', pathMatch: 'full' },
      { path: 'cliente/login', component: LoginCliente },
      { path: 'cliente/confirmar-cuenta', component: ConfirmarCuentaClienteComponent },
      { path: 'cliente/restablecer-contrasena', component: RestablecerContrasenaComponent },
      { path: 'cliente/inicio', component: PortalClienteComponent, canActivate: [authClienteGuard] },
      { path: 'cliente/en-trabajo', component: EnTrabajoComponent, canActivate: [authClienteGuard], data: { titulo: 'Portal del cliente' } },
      { path: 'inicio', component: InicioClienteComponent },
      { path: 'reservas', component: ReservaComponent },
      { path: 'probador-marcos', component: ProbadorMarcosComponent },
      { path: 'recomendador-marcos', component: RecomendadorMarcosComponent },
      { path: 'productos', component: BuscarProductoCliente },
      { path: 'mis-recetas', component: MisRecetas, canActivate: [authClienteGuard] }
    ]
  },

  {
    path: 'admin',
    children: [
      { path: '', component: LoginCliente },
      {
        path: 'panel',
        component: AdminLayoutComponent,
        canActivate: [authAdminGuard],
        canActivateChild: [authAdminGuard],
        children: [
          { path: '', component: InicioPersonalComponent },
          {
            path: 'trabajadores',
            component: TrabajadoresComponent,
            data: { soloAdministrador: true },
          },
          {
            path: 'cambiar-contrasena',
            component: CambiarContrasenaComponent,
            data: { soloTrabajador: true },
          },
          { path: 'sin-acceso', component: SinAccesoComponent },
          { path: 'horarios', component: GestionHorariosComponent, data: { permiso: 'horarios', tituloInicio: 'Horarios' } },
          { path: 'agenda', component: BuscarReservas, data: { permiso: 'agenda', tituloInicio: 'Agenda' } },
          {
            path: 'productos/nuevo',
            component: ProductoComponent,
            data: { permiso: 'inventario' },
          },
          { path: 'clientes', component: ClientesComponent, data: { permiso: 'clientes', tituloInicio: 'Clientes' } },
          { path: 'productos', component: BuscarProductoAdmin, data: { permiso: 'inventario', tituloInicio: 'Inventario' } },
          { path: 'stock', component: ConsultarStock, data: { permiso: 'inventario' } },
          { path: 'marcos', component: GestionMarcosComponent, data: { permiso: 'inventario' } },
          { path: 'marcos/crear', component: CrearMarcoComponent, data: { permiso: 'inventario' } },
          { path: 'marcos/rostros', component: GestionRostrosComponent, data: { permiso: 'inventario' } },
          { path: 'rostros', redirectTo: 'marcos/rostros', pathMatch: 'full' },
          { path: 'pedidos/historial', component: HistorialPedidosComponent, data: { permiso: 'pedidos' } },
          { path: 'ventas', component: RegistroVentaComponent, data: { permiso: 'ventas', tituloInicio: 'Ventas' } },
          { path: 'ventas/historial', component: VerVentasComponent, data: { permiso: 'ventas' } },
          { path: 'recetas', component: RegistroRecetaComponent, data: { permiso: 'recetas', tituloInicio: 'Recetas' } },
          {
            path: 'recetas/cliente',
            component: HistorialGraduaciones,
            data: { permiso: 'recetas', verRecetas: true },
          },
          {
            path: 'recetas/:id/editar',
            component: RegistroRecetaComponent,
            data: { permiso: 'recetas' },
          },
          { path: 'pedido', component: PedidoComponent, data: { permiso: 'pedidos', tituloInicio: 'Pedidos' } },
          { path: 'crear-pedido', component: CrearPedidoComponent, data: { permiso: 'pedidos' } },
          { path: 'pedido/:id', component: DetallePedidoComponent, data: { permiso: 'pedidos' } },
          {
            path: 'historial-graduaciones',
            component: HistorialGraduaciones,
            data: { permiso: 'recetas' },
          },
        ],
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
