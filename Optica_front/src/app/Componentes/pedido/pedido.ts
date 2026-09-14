import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PedidoService } from './pedido.service';

@Component({
  selector: 'app-pedido',
  imports: [ReactiveFormsModule],
  templateUrl: './pedido.html',
  styleUrl: './pedido.css'
})
export class PedidoComponent {
  protected readonly confirmedClient = signal('');
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');

  protected readonly pedidoForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly pedidoService: PedidoService
  ) {
    this.pedidoForm = this.formBuilder.group({
      cliente: ['', [Validators.required, Validators.minLength(3)]],
      receta: ['', [Validators.required, Validators.minLength(10)]],
      fecha: ['', Validators.required]
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

    this.pedidoService.crearPedido({
      cliente: formValue.cliente ?? '',
      receta: formValue.receta ?? '',
      fecha: formValue.fecha ?? '',
      estado: 'Pendiente'
    }).subscribe({
      next: () => {
        this.confirmedClient.set(formValue.cliente ?? '');
        this.isSubmitting.set(false);
        this.pedidoForm.reset();
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
