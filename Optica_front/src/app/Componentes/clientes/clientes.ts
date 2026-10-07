import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ClienteService, Cliente, HistorialCliente } from './cliente.service';
import { validarRutChileno } from '../../shared/validators/rut-chileno.validator';

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
  styles: [`
    .clientes-contenedor {
      max-width: 980px;
      margin: 24px auto;
      padding: 0 16px;
      position: relative;
    }

    .titulo-vista {
      color: var(--color-principal);
      border-bottom: 2px solid var(--color-principal);
      padding-bottom: 8px;
      font-family: Georgia, 'Times New Roman', serif;
      font-weight: 400;
      letter-spacing: -0.02em;
    }

    .panel-modulo {
      background: var(--color-superficie);
      border: 1px solid var(--color-borde-panel);
      border-radius: 8px;
      box-shadow: 0 2px 8px var(--sombra-principal-08);
      margin-bottom: 28px;
      overflow: hidden;
    }

    .panel-encabezado {
      background-color: var(--color-principal);
      color: var(--color-superficie);
      padding: 12px 20px;
      font-weight: bold;
    }

    .panel-cuerpo {
      padding: 20px;
    }

    .modal-backdrop-glass {
      position: fixed !important;
      inset: 0 !important;
      top: 0 !important;
      left: 0 !important;
      width: 100vw !important;
      height: 100vh !important;
      background-color: var(--capa-modal, rgba(18, 39, 37, 0.6)) !important;
      backdrop-filter: blur(8px) !important;
      -webkit-backdrop-filter: blur(8px) !important;
      display: flex !important;
      justify-content: center !important;
      align-items: center !important;
      z-index: 10000 !important;
    }

    .modal-ventana-glass {
      background: color-mix(in srgb, var(--color-superficie-calida, #fffdf9) 94%, transparent) !important;
      backdrop-filter: blur(16px) !important;
      -webkit-backdrop-filter: blur(16px) !important;
      border: 1px solid color-mix(in srgb, var(--color-texto, #1d3434) 14%, transparent) !important;
      box-shadow: 0 24px 70px var(--sombra-principal-15, rgba(0, 0, 0, 0.25)) !important;
      border-radius: 8px !important;
      width: 90% !important;
      max-width: 820px !important;
      max-height: 88vh !important;
      display: flex !important;
      flex-direction: column !important;
      overflow: hidden !important;
    }

    .modal-encabezado {
      background-color: var(--color-principal) !important;
      color: var(--color-superficie) !important;
      padding: 14px 20px !important;
      display: flex !important;
      justify-content: space-between !important;
      align-items: center !important;
      border-bottom: 3px solid var(--color-acento) !important;
    }

    .modal-btn-cerrar {
      background: transparent !important;
      border: none !important;
      color: var(--color-superficie) !important;
      font-size: 24px !important;
      cursor: pointer !important;
      line-height: 1 !important;
    }

    .modal-cuerpo {
      padding: 20px !important;
      overflow-y: auto !important;
    }

    .modal-pie {
      display: flex !important;
      justify-content: flex-end !important;
      padding: 12px 20px !important;
      border-top: 1px solid var(--color-borde-panel) !important;
      background-color: var(--color-fondo-panel) !important;
    }

    .ficha-resumen {
      background-color: var(--color-fondo-panel) !important;
      border: 1px solid var(--color-borde-panel) !important;
      border-left: 4px solid var(--color-principal) !important;
      border-radius: 6px !important;
      padding: 10px 16px !important;
      margin-bottom: 18px !important;
      display: flex !important;
      flex-wrap: wrap !important;
      gap: 16px !important;
      font-size: 13px !important;
    }

    .tabs-navegacion {
      display: flex !important;
      gap: 8px !important;
      border-bottom: 2px solid var(--color-borde-panel) !important;
      margin-bottom: 16px !important;
    }

    .tab-boton {
      background: transparent !important;
      border: none !important;
      padding: 8px 16px !important;
      font-size: 14px !important;
      font-weight: bold !important;
      cursor: pointer !important;
      border-bottom: 3px solid transparent !important;
      color: var(--color-texto-tabla) !important;
      transition: all 0.2s ease !important;
    }

    .tab-boton.activa {
      border-bottom: 3px solid var(--color-acento) !important;
      color: var(--color-acento) !important;
    }

    .tarjeta-receta {
      border: 1px solid var(--color-borde-panel) !important;
      border-radius: 6px !important;
      padding: 14px !important;
      margin-bottom: 14px !important;
      background-color: var(--color-superficie) !important;
      box-shadow: 0 2px 5px var(--sombra-principal-minima) !important;
    }

    .tarjeta-receta-cabecera {
      display: flex !important;
      justify-content: space-between !important;
      align-items: center !important;
      margin-bottom: 8px !important;
      border-bottom: 1px solid var(--color-borde-panel) !important;
      padding-bottom: 6px !important;
      font-weight: bold !important;
      color: var(--color-principal) !important;
      font-size: 14px !important;
    }

    .tabla-datos {
      width: 100% !important;
      border-collapse: collapse !important;
      font-size: 13px !important;
    }

    .tabla-datos th {
      background-color: var(--color-fondo-panel) !important;
      border-bottom: 1px solid var(--color-borde-tabla) !important;
      padding: 6px !important;
      color: var(--color-texto) !important;
    }

    .tabla-datos td {
      padding: 6px !important;
      border-bottom: 1px solid var(--color-borde-panel) !important;
    }

    .btn-historial {
      background-color: var(--color-acento) !important;
      color: var(--color-superficie-calida) !important;
      border: none !important;
      padding: 6px 12px !important;
      border-radius: 4px !important;
      cursor: pointer !important;
      font-size: 13px !important;
      font-weight: 600 !important;
      display: inline-flex !important;
      align-items: center !important;
      gap: 4px !important;
    }

    .btn-cerrar-modal {
      background-color: var(--color-principal) !important;
      color: var(--color-superficie) !important;
      border: none !important;
      padding: 8px 20px !important;
      border-radius: 4px !important;
      cursor: pointer !important;
      font-weight: 600 !important;
      font-size: 13.5px !important;
    }
  `]
})
export class ClientesComponent implements OnInit {
  clienteForm!: FormGroup;
  editarForm!: FormGroup;
  terminoBusqueda: string = '';
  clientes: Cliente[] = [];
  busquedaRealizada: boolean = false;
  cargando: boolean = false;

  modalEdicionAbierto: boolean = false;
  clienteSeleccionado: Cliente | null = null;
  guardandoEdicion: boolean = false;

  modalEstadoAbierto: boolean = false;
  clienteEstadoSeleccionado: Cliente | null = null;
  nuevoEstadoObjetivo: 'Activo' | 'Inactivo' = 'Inactivo';
  procesandoEstado: boolean = false;

  modalHistorialAbierto: boolean = false;
  cargandoHistorial: boolean = false;
  errorHistorial: string | null = null;
  historialSeleccionado: HistorialCliente | null = null;
  pestanaActiva: 'recetas' | 'pedidos' = 'recetas';

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

    this.editarForm = this.fb.group({
      rut: [{ value: '', disabled: true }],
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
          detalle = err.error?.mensaje || 'El RUT o correo ya está registrado en la base de datos.';
          this.mostrarAviso('error', 'Dato duplicado', detalle);
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

  abrirModalEdicion(cliente: Cliente): void {
    this.clienteSeleccionado = { ...cliente };
    this.editarForm.reset({
      rut: cliente.rut,
      nombre: cliente.nombre,
      apellido: cliente.apellido,
      telefono: cliente.telefono || '',
      correo: cliente.correo || ''
    });
    this.modalEdicionAbierto = true;
  }

  cerrarModalEdicion(): void {
    this.modalEdicionAbierto = false;
    this.clienteSeleccionado = null;
    this.editarForm.reset();
  }

  guardandoEdicionSubmit(): void {
    if (this.editarForm.invalid) {
      this.editarForm.markAllAsTouched();
      if (this.editarForm.hasError('requireContact')) {
        this.mostrarAviso('warning', 'Contacto requerido', 'Debe registrar al menos un número de teléfono o correo electrónico.');
        return;
      }
      this.mostrarAviso('warning', 'Formulario incompleto', 'Por favor, revise los campos marcados en rojo.');
      return;
    }

    if (!this.clienteSeleccionado?.idCliente) return;

    this.guardandoEdicion = true;

    const datosModificados: Partial<Cliente> = {
      ...this.editarForm.getRawValue()
    };

    this.clienteService.actualizar(this.clienteSeleccionado.idCliente, datosModificados).subscribe({
      next: () => {
        this.guardandoEdicion = false;
        this.cerrarModalEdicion();
        this.consultar();
        this.mostrarAviso('success', 'Cliente Actualizado', 'La información del cliente se guardó permanentemente.');
      },
      error: (err) => {
        this.guardandoEdicion = false;
        let detalle = 'No se pudo guardar la información del cliente en el servidor.';
        if (err.status === 400 && err.error?.mensaje) {
          detalle = err.error.mensaje;
        } else if (err.status === 404) {
          detalle = 'El cliente no fue encontrado en la base de datos.';
        }
        this.mostrarAviso('error', 'Error al actualizar', detalle);
      }
    });
  }

  abrirModalEstado(cliente: Cliente, nuevoEstado: 'Activo' | 'Inactivo'): void {
    this.clienteEstadoSeleccionado = cliente;
    this.nuevoEstadoObjetivo = nuevoEstado;
    this.modalEstadoAbierto = true;
    this.cd.detectChanges();
  }

  cerrarModalEstado(): void {
    this.modalEstadoAbierto = false;
    this.clienteEstadoSeleccionado = null;
    this.procesandoEstado = false;
    this.cd.markForCheck();
    this.cd.detectChanges();
  }

  confirmarCambioEstado(): void {
    if (!this.clienteEstadoSeleccionado) return;

    const cliente = this.clienteEstadoSeleccionado as any;
    const id = cliente.idCliente ?? cliente.id ?? cliente.IdCliente ?? cliente.id_cliente;

    if (!id) {
      this.mostrarAviso('error', 'Error interno', 'No se pudo identificar el ID del cliente.');
      this.cerrarModalEstado();
      return;
    }

    this.procesandoEstado = true;
    const nuevoEstado = this.nuevoEstadoObjetivo;
    const rutCliente = cliente.rut;

    this.clienteService.cambiarEstado(id, nuevoEstado).subscribe({
      next: () => {
        const clienteEnLista = this.clientes.find(c => c.idCliente === id || c.rut === rutCliente);
        if (clienteEnLista) {
          clienteEnLista.estado = nuevoEstado;
        }

        this.cerrarModalEstado();
        const accion = nuevoEstado === 'Inactivo' ? 'desactivado' : 'activado';
        this.mostrarAviso('success', 'Estado Actualizado', `El cliente fue ${accion} exitosamente.`);
      },
      error: (err) => {
        this.cerrarModalEstado();
        const detalle = err.error?.mensaje || 'No se pudo cambiar el estado del cliente.';
        this.mostrarAviso('error', 'Error al cambiar estado', detalle);
      }
    });
  }

  abrirModalHistorial(cliente: Cliente): void {
    const id = cliente.idCliente ?? (cliente as any).id ?? (cliente as any).IdCliente;
    if (!id) {
      this.mostrarAviso('error', 'Error', 'No se pudo identificar el identificador del cliente.');
      return;
    }

    this.modalHistorialAbierto = true;
    this.cargandoHistorial = true;
    this.errorHistorial = null;
    this.historialSeleccionado = null;
    this.pestanaActiva = 'recetas';
    this.cd.detectChanges();

    this.clienteService.obtenerHistorial(id).subscribe({
      next: (data) => {
        this.historialSeleccionado = data;
        this.cargandoHistorial = false;
        this.cd.detectChanges();
      },
      error: (err) => {
        this.errorHistorial = err.error?.mensaje || 'No fue posible cargar el historial del cliente.';
        this.cargandoHistorial = false;
        this.cd.detectChanges();
      }
    });
  }

  cerrarModalHistorial(): void {
    this.modalHistorialAbierto = false;
    this.historialSeleccionado = null;
    this.errorHistorial = null;
    this.cd.detectChanges();
  }

  cambiarPestana(pestana: 'recetas' | 'pedidos'): void {
  this.pestanaActiva = pestana;
  this.cd.detectChanges();
  }
}