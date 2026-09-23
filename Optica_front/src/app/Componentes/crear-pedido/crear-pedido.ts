import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { PedidoService, ClienteOption } from '../pedido/pedido.service';

@Component({
  selector: 'app-crear-pedido',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './crear-pedido.html',
  styleUrl: './crear-pedido.css'
})
export class CrearPedidoComponent implements OnInit {
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly clientes = signal<ClienteOption[]>([]);

  protected readonly pedidoForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly pedidoService: PedidoService,
    private readonly router: Router
  ) {
    this.pedidoForm = this.formBuilder.group({
      rut: ['', Validators.required],
      fecha: ['', Validators.required],
      estado: ['Pendiente', Validators.required],
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

  protected submitPedido(): void {
    if (this.pedidoForm.invalid) {
      this.pedidoForm.markAllAsTouched();
      return;
    }

    const formValue = this.pedidoForm.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.pedidoService.crearPedido({
      rut: formValue.rut ?? '',
      fecha: formValue.fecha ?? '',
      estado: formValue.estado ?? 'Pendiente',
      total: formValue.total ?? 0,
      anotaciones: formValue.anotaciones ?? ''
    }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Pedido guardado exitosamente.');
        this.pedidoForm.reset();
        setTimeout(() => this.router.navigate(['/admin/pedido']), 2000);
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(
          error.error?.mensaje ?? 'No se pudo crear el pedido. Inténtalo nuevamente.'
        );
        this.isSubmitting.set(false);
      }
    });
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.pedidoForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }
}
