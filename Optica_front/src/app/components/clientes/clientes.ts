import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ClienteService, Cliente } from '../../services/cliente';

function validarRutChileno(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  if (!valor) return null;

  const limpio = valor.replace(/\./g, '').replace(/-/g, '').trim().toUpperCase();

  if (limpio.length < 8 || limpio.length > 9) {
    return { rutInvalido: true };
  }

  const cuerpo = limpio.slice(0, -1);
  const dvIngresado = limpio.slice(-1);

  if (!/^\d+$/.test(cuerpo)) {
    return { rutInvalido: true };
  }

  let suma = 0;
  let multiplo = 2;
  for (let i = cuerpo.length - 1; i >= 0; i--) {
    suma += parseInt(cuerpo[i], 10) * multiplo;
    multiplo = multiplo === 7 ? 2 : multiplo + 1;
  }

  const resto = suma % 11;
  const dvEsperado = 11 - resto;

  let dvCalculado = '';
  if (dvEsperado === 11) dvCalculado = '0';
  else if (dvEsperado === 10) dvCalculado = 'K';
  else dvCalculado = dvEsperado.toString();

  return dvCalculado === dvIngresado ? null : { rutInvalido: true };
}

function requireContactValidator(group: AbstractControl): ValidationErrors | null {
  const telefono = group.get('telefono')?.value;
  const correo = group.get('correo')?.value;
  return (!telefono && !correo) ? { requireContact: true } : null;
}

@Component({
  selector: 'app-clientes',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './clientes.html',
  styleUrls: ['./clientes.css']
})
export class ClientesComponent implements OnInit {
  clienteForm!: FormGroup;
  terminoBusqueda: string = '';
  clientes: Cliente[] = [];
  busquedaRealizada: boolean = false;
  cargando: boolean = false;

  // Notificación Pop-up / Toast
  toast: { tipo: 'success' | 'error' | 'warning', titulo: string, mensaje: string } | null = null;
  private toastTimeout: any;

  constructor(
    private fb: FormBuilder,
    private clienteService: ClienteService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.clienteForm = this.fb.group({
      rut: ['', [Validators.required, validarRutChileno]],
      nombre: ['', [Validators.required, Validators.pattern(/^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]{2,}$/)]],
      apellido: ['', [Validators.required, Validators.pattern(/^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]{2,}$/)]],
      telefono: ['', [Validators.pattern(/^(\+?56\s?)?9\s?\d{4}\s?\d{4}$/)]],
      correo: ['', [Validators.email]]
    }, { validators: requireContactValidator });

    this.consultar();
  }

  mostrarAviso(tipo: 'success' | 'error' | 'warning', titulo: string, mensaje: string): void {
    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
    }
    this.toast = { tipo, titulo, mensaje };
    this.cd.detectChanges();

    this.toastTimeout = setTimeout(() => {
      this.cerrarAviso();
    }, 4500);
  }

  cerrarAviso(): void {
    this.toast = null;
    this.cd.detectChanges();
  }

  registrar(): void {
    // Si intenta guardar y el formulario tiene errores, notificamos con pop-up exactamente qué falló
    if (this.clienteForm.invalid) {
      this.clienteForm.markAllAsTouched();

      if (this.clienteForm.get('rut')?.hasError('rutInvalido')) {
        this.mostrarAviso('warning', 'RUT Inválido', 'El RUT ingresado o su dígito verificador no es correcto.');
        return;
      }
      if (this.clienteForm.hasError('requireContact')) {
        this.mostrarAviso('warning', 'Contacto requerido', 'Debe registrar al menos un número de teléfono o correo electrónico.');
        return;
      }
      this.mostrarAviso('warning', 'Formulario incompleto', 'Por favor, revise los campos marcados en rojo.');
      return;
    }

    this.cargando = true;

    this.clienteService.registrar(this.clienteForm.value).subscribe({
      next: () => {
        this.cargando = false;
        this.mostrarAviso('success', 'Registro exitoso', 'El cliente ha sido ingresado correctamente en el sistema.');
        this.clienteForm.reset();
        this.consultar();
      },
      error: (err) => {
        this.cargando = false;
        
        let detalle = 'Ocurrió un error inesperado al procesar la solicitud.';

        if (err.status === 409) {
          detalle = err.error?.mensaje || 'Ya existe un cliente registrado con este RUT en la base de datos.';
          this.mostrarAviso('error', 'RUT Duplicado', detalle);
          return;
        }

        if (err.status === 400 && err.error?.mensaje) {
          detalle = err.error.mensaje;
        }

        this.mostrarAviso('error', 'Error de registro', detalle);
      }
    });
  }

  consultar(): void {
    this.cargando = true;
    this.clienteService.buscar(this.terminoBusqueda).subscribe({
      next: (data) => {
        this.clientes = data;
        this.busquedaRealizada = true;
        this.cargando = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.clientes = [];
        this.busquedaRealizada = true;
        this.cargando = false;
        this.mostrarAviso('error', 'Error de conexión', 'No se pudo conectar con el servidor para consultar clientes.');
      }
    });
  }
}
