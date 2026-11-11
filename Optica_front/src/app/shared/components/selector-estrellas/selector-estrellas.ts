import { Component, ElementRef, forwardRef, input, signal, viewChildren } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Selector de calificación de 1 a 5 estrellas, usable con formularios reactivos.
 * Se comporta como un grupo de opciones: Tab entra al grupo y las flechas cambian el valor.
 */
@Component({
  selector: 'app-selector-estrellas',
  standalone: true,
  templateUrl: './selector-estrellas.html',
  styleUrl: './selector-estrellas.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectorEstrellasComponent),
      multi: true,
    },
  ],
})
export class SelectorEstrellasComponent implements ControlValueAccessor {
  /** Id del elemento que contiene el texto de la pregunta. */
  readonly etiquetadoPor = input<string | null>(null);
  /** Texto alternativo cuando no hay un elemento que etiquete al grupo. */
  readonly etiqueta = input('Calificación de 1 a 5 estrellas');

  readonly estrellas = [1, 2, 3, 4, 5] as const;
  readonly valor = signal(0);
  readonly vistaPrevia = signal(0);
  readonly deshabilitado = signal(false);

  private readonly botones = viewChildren<ElementRef<HTMLButtonElement>>('boton');
  private alCambiar: (valor: number | null) => void = () => {};
  private alTocar: () => void = () => {};

  writeValue(valor: unknown): void {
    this.valor.set(
      typeof valor === 'number' && Number.isInteger(valor) && valor >= 1 && valor <= 5 ? valor : 0,
    );
  }

  registerOnChange(fn: (valor: number | null) => void): void {
    this.alCambiar = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.alTocar = fn;
  }

  setDisabledState(deshabilitado: boolean): void {
    this.deshabilitado.set(deshabilitado);
  }

  seleccionar(estrella: number): void {
    if (this.deshabilitado()) return;
    this.valor.set(estrella);
    this.alCambiar(estrella);
  }

  estaRellena(estrella: number): boolean {
    return estrella <= (this.vistaPrevia() || this.valor());
  }

  /** Solo una estrella recibe el foco con Tab: la seleccionada o, si no hay, la primera. */
  indiceTab(estrella: number): number {
    const enfocable = this.valor() || 1;
    return estrella === enfocable ? 0 : -1;
  }

  textoAccesible(estrella: number): string {
    return `${estrella} de 5 estrellas`;
  }

  alPresionarTecla(evento: KeyboardEvent, estrella: number): void {
    let destino: number | null = null;
    switch (evento.key) {
      case 'ArrowRight':
      case 'ArrowUp':
        destino = Math.min(estrella + 1, 5);
        break;
      case 'ArrowLeft':
      case 'ArrowDown':
        destino = Math.max(estrella - 1, 1);
        break;
      case 'Home':
        destino = 1;
        break;
      case 'End':
        destino = 5;
        break;
    }
    if (destino === null) return;
    evento.preventDefault();
    this.seleccionar(destino);
    this.botones()[destino - 1]?.nativeElement.focus();
  }

  alSalirDelGrupo(evento: FocusEvent): void {
    const grupo = evento.currentTarget as HTMLElement;
    if (!grupo.contains(evento.relatedTarget as Node | null)) this.alTocar();
  }
}
