import { Component, inject, model, output, signal } from '@angular/core';
import { AutocompletadoRutService, ClienteSugerencia } from './autocompletado-rut.service';

@Component({
  selector: 'app-autocompletado-rut',
  imports: [],
  templateUrl: './autocompletado-rut.html',
  styleUrl: './autocompletado-rut.css',
})
export class AutocompletadoRut {
  private readonly autocompletadoRutService = inject(AutocompletadoRutService);

  value = model<string>('');
  blurred = output<void>();

  sugerencias = signal<ClienteSugerencia[]>([]);
  mostrarSugerencias = signal(false);

  private debounceTimeout?: ReturnType<typeof setTimeout>;

  onInputChange(texto: string) {
    this.value.set(texto);

    clearTimeout(this.debounceTimeout);

    if (texto.trim().length < 2) {
      this.sugerencias.set([]);
      this.mostrarSugerencias.set(false);
      return;
    }

    this.debounceTimeout = setTimeout(() => {
      this.autocompletadoRutService.buscarSugerencias(texto).subscribe({
        next: (resultados) => {
          this.sugerencias.set(resultados);
          this.mostrarSugerencias.set(resultados.length > 0);
        },
        error: () => {
          this.sugerencias.set([]);
          this.mostrarSugerencias.set(false);
        },
      });
    }, 300);
  }

  seleccionarSugerencia(sugerencia: ClienteSugerencia) {
    this.value.set(sugerencia.rut);
    this.sugerencias.set([]);
    this.mostrarSugerencias.set(false);
  }

  ocultarSugerencias() {
    this.mostrarSugerencias.set(false);
    this.blurred.emit();
  }
}