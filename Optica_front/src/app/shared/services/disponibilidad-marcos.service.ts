import { Injectable, inject } from '@angular/core';
import { TiposMarcoService } from './tipos-marco.service';

@Injectable({ providedIn: 'root' })
export class DisponibilidadMarcosService {
  private readonly tipos = inject(TiposMarcoService);
  consultar() { return this.tipos.disponibles(); }
}
