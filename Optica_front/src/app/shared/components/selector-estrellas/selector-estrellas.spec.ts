import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';

import { SelectorEstrellasComponent } from './selector-estrellas';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, SelectorEstrellasComponent],
  template: `
    <p id="pregunta">¿Qué tal?</p>
    <app-selector-estrellas [formControl]="control" etiquetadoPor="pregunta" />
  `,
})
class AnfitrionPrueba {
  readonly control = new FormControl<number | null>(null);
}

describe('SelectorEstrellasComponent', () => {
  let fixture: ComponentFixture<AnfitrionPrueba>;
  let anfitrion: AnfitrionPrueba;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AnfitrionPrueba] }).compileComponents();
    fixture = TestBed.createComponent(AnfitrionPrueba);
    anfitrion = fixture.componentInstance;
    fixture.detectChanges();
  });

  function estrellas(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('button[role="radio"]'));
  }

  it('muestra cinco estrellas accesibles, sin texto visible, dentro de un grupo etiquetado', () => {
    const grupo = fixture.nativeElement.querySelector('[role="radiogroup"]') as HTMLElement;

    expect(grupo.getAttribute('aria-labelledby')).toBe('pregunta');
    expect(estrellas().length).toBe(5);
    expect(estrellas()[2].getAttribute('aria-label')).toBe('3 de 5 estrellas');
    expect(estrellas()[0].getAttribute('aria-label')).toBe('1 de 5 estrellas');
    expect(estrellas().every((boton) => boton.textContent?.trim() === '★')).toBe(true);
  });

  it('actualiza el formulario al hacer clic en una estrella', () => {
    estrellas()[3].click();
    fixture.detectChanges();

    expect(anfitrion.control.value).toBe(4);
    expect(estrellas()[3].getAttribute('aria-checked')).toBe('true');
    expect(estrellas().filter((boton) => boton.classList.contains('rellena')).length).toBe(4);
  });

  it('refleja el valor escrito desde el formulario', () => {
    anfitrion.control.setValue(2);
    fixture.detectChanges();

    expect(estrellas()[1].getAttribute('aria-checked')).toBe('true');
    expect(estrellas()[1].getAttribute('tabindex')).toBe('0');
    expect(estrellas()[0].getAttribute('tabindex')).toBe('-1');
  });

  it('permite cambiar la calificación con las flechas, Inicio y Fin', () => {
    const teclear = (indice: number, key: string) => {
      estrellas()[indice].dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
      fixture.detectChanges();
    };

    teclear(0, 'ArrowRight');
    expect(anfitrion.control.value).toBe(2);
    teclear(1, 'ArrowLeft');
    expect(anfitrion.control.value).toBe(1);
    teclear(0, 'ArrowLeft');
    expect(anfitrion.control.value).toBe(1);
    teclear(0, 'End');
    expect(anfitrion.control.value).toBe(5);
    expect(document.activeElement).toBe(estrellas()[4]);
    teclear(4, 'Home');
    expect(anfitrion.control.value).toBe(1);
  });

  it('solo la primera estrella es enfocable con Tab cuando no hay valor', () => {
    expect(estrellas().map((boton) => boton.getAttribute('tabindex'))).toEqual([
      '0',
      '-1',
      '-1',
      '-1',
      '-1',
    ]);
  });

  it('no cambia el valor cuando el control está deshabilitado', () => {
    anfitrion.control.disable();
    fixture.detectChanges();
    estrellas()[4].click();

    expect(anfitrion.control.value).toBeNull();
    expect(estrellas().every((boton) => boton.disabled)).toBe(true);
  });
});
