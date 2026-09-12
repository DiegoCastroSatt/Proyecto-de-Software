import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RegistroVentaService, Venta } from './registro-venta.service';

@Component({
  selector: 'app-registro-venta',
  imports: [ReactiveFormsModule],
  templateUrl: './registro-venta.html',
  styleUrl: './registro-venta.css'
})
export class RegistroVentaComponent implements OnInit {
  protected readonly ventas = signal<Venta[]>([]);
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly guardando = signal(false);
  protected readonly formulario;

  constructor(private readonly fb: FormBuilder, private readonly servicio: RegistroVentaService) {
    this.formulario = this.fb.group({
      rutCliente: ['', Validators.required],
      codigoProducto: ['', Validators.required],
      cantidad: [1, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.cargarVentas();
  }

  protected cargarVentas(): void {
    this.servicio.listar().subscribe({
      next: ventas => this.ventas.set(ventas),
      error: () => this.error.set('No se pudo cargar el historial de ventas.')
    });
  }

  protected registrar(): void {
    if (this.formulario.invalid || this.guardando()) {
      this.formulario.markAllAsTouched();
      return;
    }
    const { rutCliente, codigoProducto, cantidad } = this.formulario.getRawValue();
    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.servicio.crear(rutCliente!.trim(), codigoProducto!.trim(), Number(cantidad)).subscribe({
      next: venta => {
        this.mensaje.set('Venta #' + venta.idVenta + ' registrada correctamente.');
        this.formulario.patchValue({ codigoProducto: '', cantidad: 1 });
        this.guardando.set(false);
        this.cargarVentas();
      },
      error: respuesta => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo registrar la venta.');
        this.guardando.set(false);
      }
    });
  }
}
