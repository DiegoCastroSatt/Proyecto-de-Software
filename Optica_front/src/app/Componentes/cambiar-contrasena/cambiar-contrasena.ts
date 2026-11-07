import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AutenticacionAdminService } from '../../shared/services/autenticacion-admin.service';
@Component({
  selector: 'app-cambiar-contrasena',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cambiar-contrasena.html',
  styleUrl: './cambiar-contrasena.css',
})
export class CambiarContrasenaComponent {
  private readonly sesion = inject(AutenticacionAdminService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly formulario = this.fb.nonNullable.group(
    {
      contrasenaActual: ['', [Validators.required, Validators.maxLength(255)]],
      nuevaContrasena: [
        '',
        [Validators.required, Validators.minLength(8), Validators.maxLength(255), Validators.pattern(/^(?=[\s\S]*\p{L})(?=[\s\S]*\p{N})(?=[\s\S]*[^\p{L}\p{N}\s])[\s\S]*$/u)],
      ],
      confirmarContrasena: ['', [Validators.required, Validators.maxLength(255)]],
    },
    {
      validators: (control) =>
        control.get('nuevaContrasena')?.value === control.get('confirmarContrasena')?.value
          ? null
          : { noCoinciden: true },
    },
  );
  guardar(): void {
    if (this.guardando()) return;
    this.error.set('');
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const datos = this.formulario.getRawValue();
    if (datos.nuevaContrasena === datos.contrasenaActual) {
      this.error.set('Elige una contraseña distinta de la actual.');
      return;
    }
    this.guardando.set(true);
    this.sesion.cambiarContrasena(datos).subscribe({
      next: () => {
        this.formulario.reset();
        this.sesion.administrador.set(null);
        void this.router.navigate(['/admin'], { queryParams: { claveActualizada: '1' } });
      },
      error: (respuesta) => {
        this.guardando.set(false);
        this.formulario.controls.contrasenaActual.reset();
        this.error.set(
          respuesta.error?.mensaje ??
            'No se pudo cambiar la contraseña. Revisa los datos e inténtalo nuevamente.',
        );
      },
    });
  }
}
