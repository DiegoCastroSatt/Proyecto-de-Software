import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
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
  private pdfBlob: Blob | null = null;
  private pdfNombreArchivo = 'cotizacion-pedido.pdf';

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
      estado: ['Pendiente', Validators.required],
      productos: [''],
      total: [0, [Validators.required, Validators.min(0)]],
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
    if (this.pdfRawUrl) {
      URL.revokeObjectURL(this.pdfRawUrl);
      this.pdfRawUrl = null;
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
    const anotaciones = formValue.anotaciones?.trim() ?? '';
    let anotacionesFinales = anotaciones;
    if (productos) {
      anotacionesFinales = anotaciones
        ? `Productos:\n${productos}\n\nAnotaciones:\n${anotaciones}`
        : `Productos:\n${productos}`;
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
        this.pedidoForm.reset();
        setTimeout(() => this.router.navigate(['/admin/panel/pedido']), 2000);
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
        fecha: formValue.fecha || new Date().toISOString().slice(0, 10),
        total: Number(formValue.total) || 0,
        productos: formValue.productos ?? '',
        anotaciones: formValue.anotaciones ?? '',
        estado: formValue.estado ?? 'Pendiente'
      });

      const blob = pdf.output('blob');
      this.pdfBlob = blob;
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
    if (!this.pdfBlob) return;
    const url = URL.createObjectURL(this.pdfBlob);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = this.pdfNombreArchivo;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  protected finalizarPedidoDesdeModal(): void {
    this.cerrarModalCotizacion();
    this.submitPedido();
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.pedidoForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }
}
