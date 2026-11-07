import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AutenticacionClienteService } from '../../shared/services/autenticacion-cliente.service';

@Component({
  selector: 'app-portal-cliente',
  imports: [RouterLink],
  templateUrl: './portal-cliente.html',
  styleUrl: './portal-cliente.css'
})
export class PortalClienteComponent {
  protected readonly cliente = inject(AutenticacionClienteService).cliente;
  protected readonly primerNombre = computed(() => this.cliente()?.nombre.trim().split(/\s+/)[0] ?? '');
  protected readonly iniciales = computed(() => this.cliente()?.nombre.trim().split(/\s+/)
    .filter(Boolean).slice(0, 2).map(parte => parte[0]).join('').toUpperCase() ?? '');
  protected readonly accesos = [
    { ruta: '/mis-recetas', titulo: 'Consulta tus recetas', etiqueta: 'Mis recetas',
      descripcion: 'Revisa tus recetas y las indicaciones para tus lentes.',
      icono: 'M6 3h9l4 4v14H6z M14 3v5h5 M9 12h7 M9 16h7' },
    { ruta: '/recomendador-marcos', titulo: 'Encuentra tu estilo', etiqueta: 'Recomendador de marcos',
      descripcion: 'Descubre las formas que mejor acompañan tu rostro.',
      icono: 'M12 3l2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5Z' },
    { ruta: '/probador-marcos', titulo: 'Pruébatelos antes', etiqueta: 'Probador virtual',
      descripcion: 'Mira cómo te quedan distintos marcos con tu cámara.',
      icono: 'M2 10h7v7H2z M15 10h7v7h-7z M9 12h6 M2 10V7 M22 10V7' },
    { ruta: '/productos', titulo: 'Explora la colección', etiqueta: 'Catálogo de productos',
      descripcion: 'Revisa modelos, colores y precios para tu próxima visita.',
      icono: 'M4 7h16v14H4z M8 7V5a4 4 0 0 1 8 0v2' }
  ];
}
