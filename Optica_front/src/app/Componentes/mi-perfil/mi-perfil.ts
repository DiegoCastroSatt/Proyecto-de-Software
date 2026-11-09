import { CommonModule } from '@angular/common';
import { Component, DestroyRef, ElementRef, HostListener, OnInit, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AutenticacionClienteService } from '../../shared/services/autenticacion-cliente.service';
import { PerfilCliente, PerfilService } from './perfil.service';

// Texto que el cliente debe escribir para confirmar la eliminación definitiva.
export const TEXTO_CONFIRMACION_ELIMINAR = 'ELIMINAR';

function textoConfirmacionValido(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === TEXTO_CONFIRMACION_ELIMINAR ? null : { confirmacion: true };
}

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
  private readonly autenticacion = inject(AutenticacionClienteService);
  private readonly router = inject(Router);
  @ViewChild('eliminarDialog') private eliminarDialog?: ElementRef<HTMLElement>;
  @ViewChild('eliminarTrigger') private eliminarTrigger?: ElementRef<HTMLButtonElement>;
  readonly textoConfirmacionEliminar = TEXTO_CONFIRMACION_ELIMINAR;
  readonly modalEliminarAbierto = signal(false);
  readonly eliminando = signal(false);
  readonly errorEliminar = signal('');
  readonly formularioEliminar = this.formularioBuilder.nonNullable.group({
    contrasenaActual: ['', [Validators.required]],
    confirmacion: ['', [textoConfirmacionValido]],
  });
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly editando = signal(false);
  readonly error = signal('');
  readonly confirmacion = signal('');
  readonly perfil = signal<PerfilCliente | null>(null);
  readonly editandoCorreo = signal(false);
  readonly procesandoCorreo = signal(false);
  readonly mensajeCorreo = signal('');
  readonly errorCorreo = signal('');
  readonly formularioCorreo = this.formularioBuilder.nonNullable.group({
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
    contrasenaActual: ['', [Validators.required]],
  });
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

  solicitarCambioCorreo(): void {
    if (this.formularioCorreo.invalid || this.procesandoCorreo()) { this.formularioCorreo.markAllAsTouched(); return; }
    this.procesandoCorreo.set(true); this.errorCorreo.set(''); this.mensajeCorreo.set('');
    const valores = this.formularioCorreo.getRawValue();
    this.servicio.solicitarCambioCorreo({ correo: valores.correo.trim(), contrasenaActual: valores.contrasenaActual })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: respuesta => { this.mensajeCorreo.set(respuesta.mensaje); this.procesandoCorreo.set(false); this.editandoCorreo.set(false); this.formularioCorreo.reset(); },
        error: (respuesta: HttpErrorResponse) => { this.errorCorreo.set(respuesta.error?.mensaje ?? 'No fue posible solicitar el cambio. Inténtalo nuevamente.'); this.procesandoCorreo.set(false); },
      });
  }

  cancelarCambioCorreo(): void {
    this.formularioCorreo.reset();
    this.errorCorreo.set('');
    this.editandoCorreo.set(false);
  }

  abrirEliminarCuenta(): void {
    this.formularioEliminar.reset();
    this.errorEliminar.set('');
    this.modalEliminarAbierto.set(true);
    setTimeout(() => this.eliminarDialog?.nativeElement.querySelector<HTMLInputElement>('#eliminar-contrasena')?.focus());
  }

  cerrarEliminarCuenta(): void {
    if (this.eliminando()) return;
    this.modalEliminarAbierto.set(false);
    this.formularioEliminar.reset();
    this.errorEliminar.set('');
    setTimeout(() => this.eliminarTrigger?.nativeElement.focus());
  }

  // No devuelve valor: si el manejador devolviera false, Angular cancelaría el envío del formulario.
  cerrarEliminarDesdeFondo(evento: MouseEvent): void {
    if (evento.target === evento.currentTarget) this.cerrarEliminarCuenta();
  }

  eliminarCuenta(): void {
    if (this.formularioEliminar.invalid || this.eliminando()) { this.formularioEliminar.markAllAsTouched(); return; }
    this.eliminando.set(true); this.errorEliminar.set('');
    this.servicio.eliminarCuenta(this.formularioEliminar.getRawValue().contrasenaActual)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          // La API ya cerró la sesión; se limpia el estado local y se vuelve al inicio.
          this.autenticacion.cliente.set(null);
          this.eliminando.set(false);
          this.modalEliminarAbierto.set(false);
          void this.router.navigateByUrl('/inicio');
        },
        error: (respuesta: HttpErrorResponse) => {
          this.eliminando.set(false);
          this.formularioEliminar.controls.contrasenaActual.reset();
          this.errorEliminar.set(respuesta.error?.mensaje ?? 'No fue posible eliminar la cuenta. Inténtalo nuevamente.');
        },
      });
  }

  // Cierre con Escape y foco atrapado dentro de la ventana de eliminación.
  @HostListener('document:keydown', ['$event'])
  controlarTeclado(evento: KeyboardEvent): void {
    if (!this.modalEliminarAbierto()) return;
    if (evento.key === 'Escape') { evento.preventDefault(); this.cerrarEliminarCuenta(); return; }
    if (evento.key !== 'Tab') return;
    const elementos = Array.from(this.eliminarDialog?.nativeElement.querySelectorAll<HTMLElement>(
      'button:not(:disabled), input:not(:disabled)') ?? []);
    if (!elementos.length) return;
    const primero = elementos[0];
    const ultimo = elementos[elementos.length - 1];
    if (evento.shiftKey && document.activeElement === primero) { evento.preventDefault(); ultimo.focus(); }
    else if (!evento.shiftKey && document.activeElement === ultimo) { evento.preventDefault(); primero.focus(); }
  }
}
