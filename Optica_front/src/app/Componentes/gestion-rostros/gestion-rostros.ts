import { RouterLink } from '@angular/router';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TipoRostro, TiposRostroService, imagenRostro } from '../../shared/services/tipos-rostro.service';
import { TipoMarco, TiposMarcoService } from '../../shared/services/tipos-marco.service';

@Component({
  selector: 'app-gestion-rostros',
  imports: [FormsModule, RouterLink],
  templateUrl: './gestion-rostros.html',
  styleUrls: ['../gestion-marcos/gestion-marcos.css', './gestion-rostros.css'],
})
export class GestionRostrosComponent {
  private readonly servicio = inject(TiposRostroService);
  private readonly tiposMarco = inject(TiposMarcoService);
  readonly rostros = signal<TipoRostro[]>([]);
  readonly marcos = signal<TipoMarco[]>([]);
  readonly cargando = signal(true);
  readonly cargandoMarcos = signal(true);
  readonly errorMarcos = signal(false);
  readonly guardando = signal(false);
  readonly ocupado = signal<number | null>(null);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly confirmarBorrado = signal<number | null>(null);
  readonly editando = signal<number | null>(null);
  readonly imagenRostro = imagenRostro;
  nombre = '';
  rasgos = '';
  consejo = '';
  imagen: File | null = null;
  marcosSeleccionados = new Set<number>();
  asociaciones = new Set<number>();

  constructor() { this.cargar(); this.cargarMarcos(); }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: rostros => { this.rostros.set(rostros); this.cargando.set(false); },
      error: () => { this.error.set('No se pudieron cargar los tipos de rostro.'); this.cargando.set(false); },
    });
  }

  cargarMarcos(): void {
    this.cargandoMarcos.set(true); this.errorMarcos.set(false);
    this.tiposMarco.listar().subscribe({
      next: marcos => { this.marcos.set(marcos); this.cargandoMarcos.set(false); },
      error: () => { this.errorMarcos.set(true); this.cargandoMarcos.set(false); },
    });
  }

  elegirImagen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0]; this.imagen = null;
    if (!archivo) return;
    if (!['image/png', 'image/jpeg', 'image/webp'].includes(archivo.type) || archivo.size > 3_000_000) {
      this.error.set('Selecciona una imagen PNG, JPG o WebP de hasta 3 MB.'); input.value = ''; return;
    }
    this.error.set(''); this.imagen = archivo;
  }

  marcar(id: number, marcado: boolean, seleccion: Set<number>): void {
    if (marcado) seleccion.add(id); else seleccion.delete(id);
  }

  crear(archivo: HTMLInputElement): void {
    if (this.guardando()) return;
    this.error.set(''); this.aviso.set('');
    if (!this.nombre.trim() || !this.rasgos.trim() || !this.consejo.trim() || !this.imagen) {
      this.error.set('Completa el nombre, los rasgos, el consejo y la imagen.'); return;
    }
    this.guardando.set(true);
    this.servicio.crear({ nombre: this.nombre.trim(), rasgos: this.rasgos.trim(), consejo: this.consejo.trim(), imagen: this.imagen,
      marcos: [...this.marcosSeleccionados] }).subscribe({
      next: rostro => {
        this.rostros.update(lista => [...lista, rostro].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        this.nombre = ''; this.rasgos = ''; this.consejo = ''; this.imagen = null; archivo.value = '';
        this.marcosSeleccionados = new Set(); this.guardando.set(false);
        this.aviso.set('Tipo de rostro creado y disponible en el recomendador.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.guardando.set(false); },
    });
  }

  alternar(rostro: TipoRostro): void {
    if (this.ocupado() !== null) return;
    this.error.set(''); this.aviso.set(''); this.ocupado.set(rostro.id);
    this.servicio.cambiarEstado(rostro.id, !rostro.activo).subscribe({
      next: () => {
        this.rostros.update(lista => lista.map(r => r.id === rostro.id ? { ...r, activo: !r.activo } : r));
        this.ocupado.set(null);
        this.aviso.set(rostro.activo ? 'Rostro desactivado. Sus asociaciones se conservan.' : 'Rostro activado en el recomendador.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.ocupado.set(null); },
    });
  }

  editarAsociaciones(rostro: TipoRostro): void {
    this.editando.set(rostro.id); this.asociaciones = new Set(rostro.marcos); this.confirmarBorrado.set(null);
  }

  guardarAsociaciones(rostro: TipoRostro): void {
    if (this.ocupado() !== null || this.errorMarcos() || this.cargandoMarcos()) return;
    const marcos = [...this.asociaciones]; this.ocupado.set(rostro.id); this.error.set(''); this.aviso.set('');
    this.servicio.asociarMarcos(rostro.id, marcos).subscribe({
      next: () => {
        this.rostros.update(lista => lista.map(r => r.id === rostro.id ? { ...r, marcos } : r));
        this.ocupado.set(null); this.editando.set(null); this.aviso.set('Marcos recomendados actualizados.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.ocupado.set(null); },
    });
  }

  borrar(rostro: TipoRostro): void {
    if (this.ocupado() !== null || this.confirmarBorrado() !== rostro.id) return;
    this.ocupado.set(rostro.id); this.error.set(''); this.aviso.set('');
    this.servicio.borrar(rostro.id).subscribe({
      next: () => {
        this.rostros.update(lista => lista.filter(r => r.id !== rostro.id));
        this.ocupado.set(null); this.confirmarBorrado.set(null); this.editando.set(null);
        this.aviso.set('Tipo de rostro borrado. Los marcos y productos se conservaron.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.ocupado.set(null); },
    });
  }

  private mensajeError(error: HttpErrorResponse): string {
    return error.error?.mensaje || 'No se pudo guardar el cambio. Vuelve a intentarlo.';
  }
}
