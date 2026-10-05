import { Injectable, computed, signal } from '@angular/core';

export type TipoNotificacionAdmin = 'informacion' | 'exito' | 'advertencia' | 'error';

export interface NotificacionAdmin {
  id: number;
  titulo: string;
  mensaje: string;
  tipo: TipoNotificacionAdmin;
}

export interface NuevaNotificacionAdmin {
  titulo: string;
  mensaje: string;
  tipo?: TipoNotificacionAdmin;
}

@Injectable({ providedIn: 'root' })
export class NotificacionesAdminService {
  private readonly elementos = signal<NotificacionAdmin[]>([]);
  private siguienteId = 1;

  readonly notificaciones = this.elementos.asReadonly();
  readonly cantidad = computed(() => this.elementos().length);

  agregar(notificacion: NuevaNotificacionAdmin): number {
    const id = this.siguienteId++;
    this.elementos.update(elementos => [
      { ...notificacion, tipo: notificacion.tipo ?? 'informacion', id },
      ...elementos
    ]);
    return id;
  }

  eliminar(id: number): void {
    this.elementos.update(elementos => elementos.filter(elemento => elemento.id !== id));
  }
}
