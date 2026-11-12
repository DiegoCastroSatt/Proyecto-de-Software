import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';
import { Component, HostListener, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { PedidoService, ClienteOption } from '../pedido/pedido.service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-crear-pedido',
  imports: [ReactiveFormsModule, RouterLink, AutocompletadoRut],
  templateUrl: './crear-pedido.html',
  styleUrl: './crear-pedido.css'
})
export class CrearPedidoComponent implements OnInit, OnDestroy {
  protected readonly isSubmitting = signal(false);
  protected readonly generandoCotizacion = signal(false);
  protected readonly mostrarModalCotizacion = signal(false);
  protected readonly pdfPreviewUrl = signal<SafeResourceUrl | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly clientes = signal<ClienteOption[]>([]);

  private pdfRawUrl: string | null = null;
  private pdfNombreArchivo = 'cotizacion-pedido.pdf';
  private redireccionTimeout?: ReturnType<typeof setTimeout>;

  protected readonly pedidoForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly pedidoService: PedidoService,
    private readonly router: Router,
    private readonly sanitizer: DomSanitizer
  ) {
    this.pedidoForm = this.formBuilder.group({
      rut: ['', Validators.required],
      fecha: ['', Validators.required],
      fechaVencimiento: [''],
      estado: ['Pendiente', Validators.required],
      productos: [''],
      servicios: [''],
      // El total lo ingresa el empleado: el sistema no lo calcula ni lo estima.
      total: [null as number | null, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
      anotaciones: ['']
    });
  }

  ngOnInit(): void {
    this.pedidoService.obtenerClientes().subscribe({
      next: (clientes) => this.clientes.set(clientes),
      error: () => this.errorMessage.set('No se pudieron cargar los clientes.')
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.redireccionTimeout);
    if (this.pdfRawUrl) {
      URL.revokeObjectURL(this.pdfRawUrl);
      this.pdfRawUrl = null;
    }
  }

  @HostListener('document:keydown.escape')
  protected alPulsarEscape(): void {
    if (this.mostrarModalCotizacion()) {
      this.cerrarModalCotizacion();
    }
  }

  protected submitPedido(): void {
    if (this.pedidoForm.invalid) {
      this.pedidoForm.markAllAsTouched();
      return;
    }

    const formValue = this.pedidoForm.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const productos = formValue.productos?.trim() ?? '';
    const servicios = formValue.servicios?.trim() ?? '';
    const anotaciones = formValue.anotaciones?.trim() ?? '';

    let anotacionesFinales = anotaciones;
    if (productos || servicios) {
      const secciones: string[] = [];
      if (productos) secciones.push(`Productos:\n${productos}`);
      if (servicios) secciones.push(`Servicios:\n${servicios}`);
      if (anotaciones) secciones.push(`Anotaciones:\n${anotaciones}`);
      anotacionesFinales = secciones.join('\n\n');
    }

    this.pedidoService.crearPedido({
      rut: formValue.rut ?? '',
      fecha: formValue.fecha ?? '',
      estado: formValue.estado ?? 'Pendiente',
      total: formValue.total ?? 0,
      anotaciones: anotacionesFinales
    }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Pedido guardado exitosamente.');
        // El modal solo se cierra cuando el guardado fue exitoso.
        this.cerrarModalCotizacion();
        this.pedidoForm.reset({ estado: 'Pendiente', total: null });
        this.redireccionTimeout = setTimeout(() => this.router.navigate(['/admin/panel/pedido']), 2000);
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(
          error.error?.mensaje ?? 'No se pudo crear el pedido. Inténtalo nuevamente.'
        );
        this.isSubmitting.set(false);
      }
    });
  }

  protected async generarCotizacion(): Promise<void> {
    if (this.generandoCotizacion()) return;

    const formValue = this.pedidoForm.getRawValue();
    const rut = formValue.rut?.trim() ?? '';

    if (!rut) {
      this.pedidoForm.controls.rut.markAsTouched();
      this.errorMessage.set('Ingresa el RUT del cliente para generar la cotización.');
      return;
    }

    if (this.pedidoForm.invalid) {
      this.pedidoForm.markAllAsTouched();
      this.errorMessage.set('Completa los datos obligatorios (fecha y un total mayor a 0) para generar la cotización.');
      return;
    }

    const errorCotizacion = this.validarDatosCotizacion();
    if (errorCotizacion) {
      this.errorMessage.set(errorCotizacion);
      return;
    }

    this.generandoCotizacion.set(true);
    this.errorMessage.set('');

    try {
      const clienteEncontrado = this.clientes().find(
        (c) => c.Rut.replace(/[.-]/g, '').toUpperCase() === rut.replace(/[.-]/g, '').toUpperCase()
      );

      const { crearCotizacionPedidoPdf } = await import('./cotizacion-pedido-pdf');
      const pdf = await crearCotizacionPedidoPdf({
        rutCliente: rut,
        nombreCliente: clienteEncontrado?.nombreCompleto,
        fecha: formValue.fecha ?? '',
        fechaVencimiento: formValue.fechaVencimiento ?? '',
        total: Number(formValue.total),
        productos: formValue.productos ?? '',
        servicios: formValue.servicios ?? '',
        anotaciones: formValue.anotaciones ?? ''
      });

      const blob = pdf.output('blob');
      const rutLimpio = rut.replace(/[.-]/g, '');
      this.pdfNombreArchivo = `cotizacion-pedido-${rutLimpio || 'cliente'}.pdf`;

      if (this.pdfRawUrl) {
        URL.revokeObjectURL(this.pdfRawUrl);
      }
      this.pdfRawUrl = URL.createObjectURL(blob);
      this.pdfPreviewUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.pdfRawUrl));
      this.mostrarModalCotizacion.set(true);
    } catch {
      this.errorMessage.set('No se pudo generar el archivo PDF de la cotización.');
    } finally {
      this.generandoCotizacion.set(false);
    }
  }

  protected cerrarModalCotizacion(): void {
    this.mostrarModalCotizacion.set(false);
    if (this.pdfRawUrl) {
      URL.revokeObjectURL(this.pdfRawUrl);
      this.pdfRawUrl = null;
    }
    this.pdfPreviewUrl.set(null);
  }

  protected descargarCotizacion(): void {
    if (!this.pdfRawUrl) return;
    const enlace = document.createElement('a');
    enlace.href = this.pdfRawUrl;
    enlace.download = this.pdfNombreArchivo;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
  }

  protected abrirCotizacionEnPestana(): void {
    if (!this.pdfRawUrl) return;
    window.open(this.pdfRawUrl, '_blank', 'noopener');
  }

  protected finalizarPedidoDesdeModal(): void {
    this.submitPedido();
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.pedidoForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }

  private validarDatosCotizacion(): string | null {
    const { productos, servicios, fecha, fechaVencimiento } = this.pedidoForm.getRawValue();

    if (!productos?.trim() && !servicios?.trim()) {
      return 'Indica al menos un producto o servicio para generar la cotización.';
    }
    if (!fechaVencimiento) {
      this.pedidoForm.controls.fechaVencimiento.markAsTouched();
      return 'Indica la fecha de vencimiento de la cotización.';
    }
    if (fecha && fechaVencimiento < fecha) {
      return 'La fecha de vencimiento no puede ser anterior a la fecha de la cotización.';
    }
    return null;
  }
}