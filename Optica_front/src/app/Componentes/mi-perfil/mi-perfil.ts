import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { PerfilCliente, PerfilService } from './perfil.service';

@Component({
  selector: 'app-mi-perfil',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './mi-perfil.html',
  styleUrl: './mi-perfil.css',
})
export class MiPerfilComponent implements OnInit {
  private readonly servicio = inject(PerfilService);
  private readonly formularioBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly editando = signal(false);
  readonly error = signal('');
  readonly confirmacion = signal('');
  readonly perfil = signal<PerfilCliente | null>(null);
  readonly formulario = this.formularioBuilder.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(60)]],
    apellido: ['', [Validators.required, Validators.maxLength(60)]],
    telefono: ['', [Validators.pattern(/^(\+?56\s?)?9\s?\d{4}\s?\d{4}$/)]],
  });

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.cargando.set(true);
    this.error.set('');
    this.servicio.obtener().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: perfil => { this.perfil.set(perfil); this.formulario.patchValue({ nombre: perfil.nombre, apellido: perfil.apellido, telefono: perfil.telefono ?? '' }); this.cargando.set(false); },
      error: () => { this.error.set('No fue posible cargar tu perfil. Inténtalo nuevamente.'); this.cargando.set(false); },
    });
  }

  editar(): void { this.confirmacion.set(''); this.error.set(''); this.editando.set(true); }

  cancelar(): void {
    const perfil = this.perfil();
    if (perfil) this.formulario.reset({ nombre: perfil.nombre, apellido: perfil.apellido, telefono: perfil.telefono ?? '' });
    this.editando.set(false); this.error.set('');
  }

  guardar(): void {
    if (this.formulario.invalid || this.guardando()) { this.formulario.markAllAsTouched(); return; }
    this.guardando.set(true); this.error.set(''); this.confirmacion.set('');
    const valores = this.formulario.getRawValue();
    this.servicio.actualizar({ nombre: valores.nombre.trim(), apellido: valores.apellido.trim(), telefono: valores.telefono.trim() || null })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: perfil => { this.perfil.set(perfil); this.formulario.patchValue({ nombre: perfil.nombre, apellido: perfil.apellido, telefono: perfil.telefono ?? '' }); this.editando.set(false); this.guardando.set(false); this.confirmacion.set('Tus datos se actualizaron correctamente.'); },
        error: (respuesta: HttpErrorResponse) => { this.guardando.set(false); this.error.set(respuesta.error?.mensaje ?? 'No fue posible guardar los cambios. Inténtalo nuevamente.'); },
      });
  }
}
