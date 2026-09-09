import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RegistrarProductoService } from './registrar-producto.service';

@Component({
  selector: 'app-producto',
  imports: [ReactiveFormsModule],
  templateUrl: './registrar-producto.html',
  styleUrl: './registrar-producto.css'
})
export class ProductoComponent {
  protected readonly categorias = [
    'Lentes ópticos',
    'Lentes de sol',
    'Armazones',
    'Lentes de contacto',
    'Accesorios'
  ];

  protected readonly savedName = signal('');
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');

  protected readonly productForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly productoService: RegistrarProductoService
  ) {
    this.productForm = this.formBuilder.group({
      codigo: ['', [Validators.required, Validators.maxLength(30)]],
      nombre: ['', [Validators.required, Validators.minLength(2)]],
      marca: [''],
      modelo: [''],
      color: [''],
      categoria: ['', Validators.required],
      precio: ['', [Validators.required, Validators.min(0)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      stockMinimo: [0, [Validators.required, Validators.min(0)]],
      estado: ['Disponible', Validators.required]
    });
  }

  protected submitProduct(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    const formValue = this.productForm.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.productoService.crearProducto({
      codigo: formValue.codigo ?? '',
      nombre: formValue.nombre ?? '',
      marca: formValue.marca ?? '',
      modelo: formValue.modelo ?? '',
      color: formValue.color ?? '',
      categoria: formValue.categoria ?? '',
      precio: Number(formValue.precio ?? 0),
      stock: Number(formValue.stock ?? 0),
      stockMinimo: Number(formValue.stockMinimo ?? 0),
      estado: formValue.estado ?? 'Disponible'
    }).subscribe({
      next: () => {
        this.savedName.set(formValue.nombre ?? '');
        this.isSubmitting.set(false);
        this.productForm.reset({ stock: 0, stockMinimo: 0, estado: 'Disponible' });
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(
          error.error?.mensaje ?? 'No se pudo guardar el producto. Inténtalo nuevamente.'
        );
        this.isSubmitting.set(false);
      }
    });
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.productForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }
}
