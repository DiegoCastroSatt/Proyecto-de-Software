import { CurrencyPipe } from '@angular/common';
import { Component, Input, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BuscarProductoService, Producto, ProductoEditable } from './buscar-producto.service';

@Component({
  selector: 'app-busqueda-producto',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './buscar-producto.html',
  styleUrl: './buscar-producto.css'
})
export class BuscarProductoComponent implements OnInit {
  @Input() esAdministrador = false;

  protected readonly productos = signal<Producto[]>([]);
  protected readonly productosVisibles = signal<Producto[]>([]);
  protected readonly categorias = signal<string[]>([]);
  protected readonly colores = signal<string[]>([]);
  protected readonly cargando = signal(false);
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly productoSeleccionado = signal<Producto | null>(null);
  protected readonly editando = signal(false);
  protected readonly guardando = signal(false);
  protected readonly selectedImage = signal<File | null>(null);
  protected readonly imagePreview = signal('');

  protected readonly busquedaForm;
  protected readonly filtrosForm;
  protected readonly productoForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly productoService: BuscarProductoService
  ) {
    this.busquedaForm = this.formBuilder.nonNullable.group({ termino: [''] });
    this.filtrosForm = this.formBuilder.nonNullable.group({
      categoria: [''],
      color: [''],
      ordenPrecio: ['']
    });
    this.productoForm = this.formBuilder.nonNullable.group({
      codigo: ['', [Validators.required, Validators.maxLength(30)]],
      nombre: ['', [Validators.required, Validators.maxLength(100)]],
      marca: [''], modelo: [''], color: [''],
      categoria: ['', Validators.required],
      precio: [0, [Validators.required, Validators.min(0)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      stockMinimo: [0, [Validators.required, Validators.min(0)]],
      estado: ['Disponible', Validators.required]
    });
  }

  ngOnInit(): void {
    this.buscar();
  }

  protected buscar(): void {
    this.cargando.set(true);
    this.error.set('');
    this.productoService.buscar(this.busquedaForm.controls.termino.value).subscribe({
      next: (productos) => {
        this.productos.set(productos);
        this.categorias.set(this.opcionesUnicas(productos.map(producto => producto.categoria)));
        this.colores.set(this.opcionesUnicas(productos.map(producto => producto.color)));
        this.aplicarFiltros();
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo consultar el catálogo.');
        this.cargando.set(false);
      }
    });
  }

  protected aplicarFiltros(): void {
    const { categoria, color, ordenPrecio } = this.filtrosForm.getRawValue();
    const filtrados = this.productos().filter(producto =>
      (!categoria || producto.categoria === categoria) &&
      (!color || producto.color === color)
    );

    if (ordenPrecio === 'menor') {
      filtrados.sort((a, b) => a.precio - b.precio);
    } else if (ordenPrecio === 'mayor') {
      filtrados.sort((a, b) => b.precio - a.precio);
    }

    this.productosVisibles.set(filtrados);
    this.mensaje.set(filtrados.length ? '' : 'No hay existencias o no se encuentra el producto.');
  }

  protected limpiarFiltros(): void {
    this.filtrosForm.reset({ categoria: '', color: '', ordenPrecio: '' });
    this.aplicarFiltros();
  }

  private opcionesUnicas(opciones: string[]): string[] {
    return [...new Set(opciones.filter(Boolean))].sort((a, b) => a.localeCompare(b));
  }

  protected abrirDetalle(producto: Producto): void {
    this.productoSeleccionado.set(producto);
    this.editando.set(false);
    this.productoForm.reset(producto);
  }

  protected cerrarDetalle(): void {
    this.productoSeleccionado.set(null);
    this.editando.set(false);
  }

  protected activarEdicion(): void {
    this.selectedImage.set(null);
    this.imagePreview.set('');
    this.editando.set(true);
  }

  protected handleImageChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const image = input.files?.[0] ?? null;
    this.selectedImage.set(image);
    this.imagePreview.set(image ? URL.createObjectURL(image) : '');
  }

  protected cancelarEdicion(): void {
    this.editando.set(false);
    this.selectedImage.set(null);
    this.imagePreview.set('');
  }

  protected imageUrl(rutaImagen?: string): string {
    if (!rutaImagen) {
      return '';
    }

    return rutaImagen.startsWith('http://') || rutaImagen.startsWith('https://')
      ? rutaImagen
      : `http://localhost:8080${rutaImagen.startsWith('/') ? '' : '/'}${rutaImagen}`;
  }

  protected guardarCambios(): void {
    const producto = this.productoSeleccionado();
    if (!producto || this.productoForm.invalid) {
      this.productoForm.markAllAsTouched();
      return;
    }

    this.guardando.set(true);
    const productoEditado: ProductoEditable = {
      ...this.productoForm.getRawValue(),
      imagen: this.selectedImage() ?? undefined
    };
    this.productoService.actualizar(producto.id, productoEditado).subscribe({
      next: (actualizado) => {
        this.productos.update(items => items.map(item => item.id === actualizado.id ? actualizado : item));
        this.productoSeleccionado.set(actualizado);
        this.editando.set(false);
        this.guardando.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo actualizar el producto.');
        this.guardando.set(false);
      }
    });
  }
}
