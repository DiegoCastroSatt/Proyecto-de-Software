import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { RegistrarProductoService } from './registro-producto.service';

@Component({
  selector: 'app-producto',
  imports: [ReactiveFormsModule],
  templateUrl: './registro-producto.html',
  styleUrl: './registro-producto.css'
})
export class ProductoComponent {
  protected readonly marcas: string[] = [];
  protected readonly colores: string[] = [];
  protected readonly categorias: string[] = [];
  protected readonly nuevaMarcaValue = '__nueva_marca__';
  protected readonly nuevoColorValue = '__nuevo_color__';
  protected readonly nuevaCategoriaValue = '__nueva_categoria__';

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
      nuevaMarca: [''],
      modelo: [''],
      color: [''],
      nuevoColor: [''],
      categoria: ['', Validators.required],
      nuevaCategoria: [''],
      precio: ['', [Validators.required, Validators.min(0)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      stockMinimo: [0, [Validators.required, Validators.min(0)]],
      estado: ['Disponible', Validators.required]
    });

    this.loadCatalogos();
  }

  private loadCatalogos(): void {
    forkJoin({
      marcas: this.productoService.obtenerCatalogo('Marca'),
      colores: this.productoService.obtenerCatalogo('Color'),
      categorias: this.productoService.obtenerCatalogo('Categoria')
    }).subscribe({
      next: (catalogos) => {
        this.marcas.push(...catalogos.marcas.map((item) => item.nombre));
        this.colores.push(...catalogos.colores.map((item) => item.nombre));
        this.categorias.push(...catalogos.categorias.map((item) => item.nombre));
      },
      error: () => this.errorMessage.set('No se pudieron cargar las opciones del formulario.')
    });
  }

  protected handleMarcaChange(value: string): void {
    if (value !== this.nuevaMarcaValue) {
      this.productForm.controls.nuevaMarca.reset('');
    }
  }

  protected handleColorChange(value: string): void {
    if (value !== this.nuevoColorValue) {
      this.productForm.controls.nuevoColor.reset('');
    }
  }

  protected handleCategoriaChange(value: string): void {
    if (value !== this.nuevaCategoriaValue) {
      this.productForm.controls.nuevaCategoria.reset('');
    }
  }

  protected addMarca(): void {
    this.addOption('marca', 'nuevaMarca', this.marcas);
  }

  protected addColor(): void {
    this.addOption('color', 'nuevoColor', this.colores);
  }

  protected addCategoria(): void {
    this.addOption('categoria', 'nuevaCategoria', this.categorias);
  }

  private addOption(
    optionControl: 'marca' | 'color' | 'categoria',
    newOptionControl: 'nuevaMarca' | 'nuevoColor' | 'nuevaCategoria',
    options: string[]
  ): void {
    const newOption = this.productForm.controls[newOptionControl].value?.trim();

    if (!newOption) {
      this.productForm.controls[newOptionControl].markAsTouched();
      return;
    }

    const tipo = optionControl === 'marca'
      ? 'Marca'
      : optionControl === 'color' ? 'Color' : 'Categoria';

    this.productoService.crearCatalogoItem(tipo, newOption).subscribe({
      next: (item) => {
        const existingOption = options.find((option) => option.toLowerCase() === item.nombre.toLowerCase());
        const selectedOption = existingOption ?? item.nombre;

        if (!existingOption) {
          options.push(item.nombre);
        }

        this.productForm.patchValue({ [optionControl]: selectedOption });
        this.productForm.controls[newOptionControl].reset('');
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudo guardar la nueva opción.');
      }
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
        this.productForm.reset({ stock: 0, stockMinimo: 0, estado: 'Disponible', nuevaMarca: '', nuevoColor: '', nuevaCategoria: '' });
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
