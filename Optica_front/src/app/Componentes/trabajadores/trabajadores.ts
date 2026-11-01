import { NgTemplateOutlet } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { AccesosService, Permiso, Rol, Trabajador } from './accesos.service';
@Component({
  selector: 'app-trabajadores',
  imports: [FormsModule, ReactiveFormsModule, NgTemplateOutlet],
  templateUrl: './trabajadores.html',
  styleUrl: './trabajadores.css',
})
export class TrabajadoresComponent implements OnInit {
  private readonly api = inject(AccesosService);
  private readonly fb = inject(FormBuilder);
  readonly permisos = signal<Permiso[]>([]);
  readonly roles = signal<Rol[]>([]);
  readonly trabajadores = signal<Trabajador[]>([]);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly vista = signal<'trabajadores' | 'roles'>('trabajadores');
  readonly nuevo = signal(false);
  @ViewChild('dialogoRol', { static: true }) dialogoRol!: ElementRef<HTMLDialogElement>;
  readonly creandoRolEnTrabajador = signal(false);
  readonly etiquetasPermisos: Record<string, string> = {
    resumen: 'Consultar resumen general',
    horarios: 'Modificar horarios',
    agenda: 'Gestionar reservas',
    clientes: 'Modificar clientes',
    inventario: 'Modificar productos y stock',
    ventas: 'Registrar y consultar ventas',
    recetas: 'Modificar recetas',
    pedidos: 'Modificar pedidos',
  };
  get rolSeleccionado(): Rol | undefined {
    return this.roles().find((rol) => rol.idRol === this.trabajadorForm.controls.idRol.value);
  }
  abrirRolEnTrabajador(): void {
    if (this.guardando() || this.cargando()) return;
    this.error.set('');
    this.rolEditado.set(null);
    this.rolForm.reset({ nombre: '', permisos: [] });
    this.creandoRolEnTrabajador.set(true);
    this.dialogoRol.nativeElement.showModal();
  }
  cancelarRolEnTrabajador(event?: Event): void {
    event?.preventDefault();
    if (this.guardando()) return;
    this.dialogoRol.nativeElement.close();
    this.creandoRolEnTrabajador.set(false);
    this.error.set('');
  }
  readonly rolEditado = signal<number | null>(null);
  readonly trabajadorForm = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(100)]],
    nombreUsuario: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9][a-zA-Z0-9._-]{2,59}$/)]],
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
    contrasena: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(255), Validators.pattern(/^(?=[\s\S]*\p{L})(?=[\s\S]*\p{N})(?=[\s\S]*[^\p{L}\p{N}\s])[\s\S]*$/u)]],
    idRol: [0, [Validators.min(1)]],
  });
  readonly rolForm = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(60)]],
    permisos: this.fb.nonNullable.control<string[]>([]),
  });
  ngOnInit(): void {
    this.cargar();
  }
  cargar(): void {
    this.cargando.set(true);
    forkJoin({
      permisos: this.api.permisos(),
      roles: this.api.roles(),
      trabajadores: this.api.trabajadores(),
    }).subscribe({
      next: (datos) => {
        this.permisos.set(datos.permisos);
        this.roles.set(datos.roles);
        this.trabajadores.set(datos.trabajadores);
        this.cargando.set(false);
      },
      error: (error) => {
        this.cargando.set(false);
        this.mostrarError(error);
      },
    });
  }
  crearTrabajador(): void {
    if (this.guardando()) return;
    if (this.trabajadorForm.invalid) {
      this.trabajadorForm.markAllAsTouched();
      return;
    }
    this.iniciarGuardado();
    this.api.crearTrabajador(this.trabajadorForm.getRawValue()).subscribe({
      next: () => {
        this.guardando.set(false);
        this.nuevo.set(false);
        this.trabajadorForm.reset();
        this.mensaje.set(
          'Trabajador creado. Puede iniciar sesión con su nombre de usuario y la contraseña inicial.',
        );
        this.cargar();
      },
      error: (error) => this.mostrarError(error),
    });
  }
  guardarAcceso(t: Trabajador): void {
    if (this.guardando()) return;
    this.iniciarGuardado();
    this.api.asignarAcceso(t).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mensaje.set(
          'Acceso actualizado. El cambio se aplica en la siguiente petición del trabajador.',
        );
        this.cargar();
      },
      error: (error) => this.mostrarError(error),
    });
  }
  editarRol(rol?: Rol): void {
    this.rolEditado.set(rol?.idRol ?? null);
    this.rolForm.reset({ nombre: rol?.nombre ?? '', permisos: [...(rol?.permisos ?? [])] });
    this.vista.set('roles');
  }
  alternarPermiso(codigo: string, marcado: boolean): void {
    const permisos = this.rolForm.controls.permisos.value;
    this.rolForm.controls.permisos.setValue(
      marcado ? [...new Set([...permisos, codigo])] : permisos.filter((p) => p !== codigo),
    );
  }
  guardarRol(): void {
    if (this.guardando()) return;
    if (this.rolForm.invalid) {
      this.rolForm.markAllAsTouched();
      return;
    }
    this.iniciarGuardado();
    this.api.guardarRol(this.rolEditado(), this.rolForm.getRawValue()).subscribe({
      next: (rol) => {
        this.guardando.set(false);
        if (this.creandoRolEnTrabajador()) {
          this.roles.update((roles) =>
            [...roles.filter((item) => item.idRol !== rol.idRol), rol].sort((a, b) =>
              a.nombre.localeCompare(b.nombre),
            ),
          );
          this.trabajadorForm.controls.idRol.setValue(rol.idRol);
          this.dialogoRol.nativeElement.close();
          this.creandoRolEnTrabajador.set(false);
          this.mensaje.set('Rol creado y seleccionado. Puedes continuar agregando al trabajador.');
        } else {
          this.mensaje.set(
            'Rol guardado. Sus permisos se aplican a todos los trabajadores que lo tienen asignado.',
          );
          this.editarRol();
          this.cargar();
        }
      },
      error: (error) => this.mostrarError(error),
    });
  }
  private iniciarGuardado(): void {
    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
  }
  private mostrarError(error: HttpErrorResponse): void {
    this.guardando.set(false);
    this.error.set(
      error.error?.mensaje ??
        (error.status === 403
          ? 'Solo un administrador puede gestionar trabajadores y roles.'
          : 'No se pudo completar la operación. Comprueba la conexión y que la migración de accesos esté aplicada.'),
    );
  }
}
