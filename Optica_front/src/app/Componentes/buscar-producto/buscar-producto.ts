import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, Input, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, Subject, takeUntil } from 'rxjs';
import { BuscarProductoService, Producto, ProductoEditable } from './buscar-producto.service';
import { ResenaProducto, ResenasProductoService } from './resenas-producto.service';
import { AutenticacionClienteService } from '../../shared/services/autenticacion-cliente.service';
import { obtenerOrigenApi } from '../../shared/services/api-origin';

@Component({
  selector: 'app-busqueda-producto',
  imports: [ReactiveFormsModule, CurrencyPipe, RouterLink],
  templateUrl: './buscar-producto.html',
  styleUrl: './buscar-producto.css'
})
export class BuscarProductoComponent implements OnInit {
  @Input() esAdministrador = false;

  private readonly ruta = inject(ActivatedRoute, { optional: true });
  protected readonly idMarco = signal<number | null>(null);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly productosVisibles = signal<Producto[]>([]);
  protected readonly categorias = signal<string[]>([]);
  protected readonly colores = signal<string[]>([]);
  protected readonly marcas = signal<string[]>([]);
  protected readonly nuevaMarcaValue = '__nueva_marca__';
  protected readonly nuevoColorValue = '__nuevo_color__';
  protected readonly nuevaCategoriaValue = '__nueva_categoria__';
  protected readonly cargando = signal(false);
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly productoSeleccionado = signal<Producto | null>(null);
  protected readonly enviandoAvisoReposicion = signal(false);
  protected readonly mensajeAvisoReposicion = signal('');
  protected readonly errorAvisoReposicion = signal('');
  protected readonly avisoReposicionEnviado = signal(false);
  protected readonly sesionClienteVerificada = signal(false);
  protected readonly editando = signal(false);
  protected readonly guardando = signal(false);
  protected readonly selectedImage = signal<File | null>(null);
  protected readonly imagePreview = signal('');
  protected readonly resenas = signal<ResenaProducto[]>([]);
  protected readonly resumenResenas = signal<{ promedio: number; cantidad: number }>({ promedio: 0, cantidad: 0 });
  protected readonly productosResenables = signal(new Map<number, boolean>());
  protected readonly cargandoResenas = signal(false);
  protected readonly publicandoResena = signal(false);
  protected readonly mensajeResena = signal('');
  protected readonly errorResena = signal('');
  protected readonly resenaForm;

  protected readonly busquedaForm;
  protected readonly filtrosForm;
  protected readonly productoForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly productoService: BuscarProductoService,
    private readonly resenasService: ResenasProductoService,
    protected readonly autenticacion: AutenticacionClienteService
  ) {
    this.busquedaForm = this.formBuilder.nonNullable.group({ termino: [''] });
    this.filtrosForm = this.formBuilder.nonNullable.group({
      estado: ['Disponible'],
      categoria: [''],
      color: [''],
      marca: [''],
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
      estado: ['Disponible', Validators.required],
      nuevaMarca: [''],
      nuevoColor: [''],
      nuevaCategoria: ['']
    });
    this.resenaForm = this.formBuilder.nonNullable.group({
      calificacion: [0, [Validators.required, Validators.min(1), Validators.max(5)]],
      comentario: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(1000)]]
    });
  }

  ngOnInit(): void {
    if (!this.esAdministrador) {
      this.filtrosForm.controls.estado.setValue('');
      this.cargarSesionYProductosResenables();
      if (this.ruta) {
        this.ruta.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(parametros => {
          const marco = Number(parametros.get('marco'));
          this.idMarco.set(Number.isInteger(marco) && marco > 0 && marco <= 2147483647 ? marco : null);
          this.buscar();
        });
        return;
      }
    }
    if (this.esAdministrador) {
      this.cargarCatalogos();
    }
    this.buscar();
  }

  private cargarSesionYProductosResenables(): void {
    const cargarProductos = () => {
      this.sesionClienteVerificada.set(true);
      if (!this.autenticacion.cliente()) return;
      this.resenasService.obtenerMisProductos().subscribe({
        next: productos => this.productosResenables.set(new Map(productos.map(item => [item.idProducto, item.yaResenado]))),
        error: () => this.productosResenables.set(new Map())
      });
    };
    if (this.autenticacion.cliente()) {
      cargarProductos();
    } else {
      this.autenticacion.verificarSesion().subscribe({
        next: cargarProductos,
        error: () => this.sesionClienteVerificada.set(true)
      });
    }
  }

  private cargarCatalogos(): void {
    forkJoin({
      marcas: this.productoService.obtenerCatalogo('Marca'),
      colores: this.productoService.obtenerCatalogo('Color'),
      categorias: this.productoService.obtenerCatalogo('Categoria')
    }).subscribe({
      next: catalogos => {
        this.marcas.set(catalogos.marcas.map(item => item.nombre));
        this.colores.set(catalogos.colores.map(item => item.nombre));
        this.categorias.set(catalogos.categorias.map(item => item.nombre));
      },
      error: () => this.error.set('No se pudieron cargar las opciones del formulario.')
    });
  }

  protected readonly pagina = signal(1);
  protected readonly totalPaginas = signal(0);
  private terminoAplicado = '';
  private readonly cancelarCatalogo = new Subject<void>();

  protected buscar(): void {
    this.terminoAplicado = this.busquedaForm.controls.termino.value;
    this.cambiarPagina(1, !this.esAdministrador);
  }

  protected aplicarFiltros(): void {
    this.cambiarPagina(1);
  }

  protected cambiarPagina(pagina: number, incluirOpciones = false): void {
    this.cancelarCatalogo.next();
    this.cargando.set(true);
    this.error.set('');
    this.productoService.buscarPagina({
      ...this.filtrosForm.getRawValue(), pagina, termino: this.terminoAplicado, incluirOpciones,
      ...(this.idMarco() !== null ? { marco: this.idMarco()! } : {})
    }).pipe(takeUntil(this.cancelarCatalogo), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: resultado => {
        this.pagina.set(pagina);
        this.totalPaginas.set(resultado.totalPaginas);
        this.productosVisibles.set(resultado.productos);
        if (resultado.categorias) this.categorias.set(resultado.categorias);
        if (resultado.colores) this.colores.set(resultado.colores);
        if (resultado.marcas) this.marcas.set(resultado.marcas);
        this.mensaje.set(resultado.productos.length ? '' : 'No hay existencias o no se encuentra el producto.');
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo consultar el catálogo.');
        this.cargando.set(false);
      }
    });
  }

  protected limpiarFiltros(): void {
    this.filtrosForm.reset({ estado: 'Disponible', categoria: '', color: '', marca: '', ordenPrecio: '' });
    this.aplicarFiltros();
  }



  protected abrirDetalle(producto: Producto): void {
    this.productoSeleccionado.set(producto);
    this.editando.set(false);
    this.productoForm.reset(producto);
    this.enviandoAvisoReposicion.set(false);
    this.mensajeAvisoReposicion.set('');
    this.errorAvisoReposicion.set('');
    this.avisoReposicionEnviado.set(false);
    this.mensajeResena.set('');
    this.errorResena.set('');
    this.resenaForm.reset({ calificacion: 0, comentario: '' });
    if (!this.esAdministrador) this.cargarResenas(producto.id);
  }

  protected esProductoAgotado(producto: Producto): boolean {
    return producto.stock <= 0 || producto.estado === 'Agotado';
  }

  protected solicitarAvisoReposicion(idProducto: number): void {
    if (!this.autenticacion.cliente() || this.enviandoAvisoReposicion() || this.avisoReposicionEnviado()) {
      return;
    }

    this.enviandoAvisoReposicion.set(true);
    this.errorAvisoReposicion.set('');
    this.mensajeAvisoReposicion.set('');
    this.productoService.solicitarAvisoReposicion(idProducto).subscribe({
      next: () => {
        this.avisoReposicionEnviado.set(true);
        this.mensajeAvisoReposicion.set('Solicitud registrada. Te enviaremos un correo cuando el producto vuelva a estar disponible.');
        this.enviandoAvisoReposicion.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.errorAvisoReposicion.set(
          respuesta.error?.mensaje ?? 'No se pudo registrar la solicitud. Inténtalo nuevamente.'
        );
        this.enviandoAvisoReposicion.set(false);
      }
    });
  }

  private cargarResenas(idProducto: number): void {
    this.cargandoResenas.set(true);
    this.resenasService.obtenerPorProducto(idProducto).subscribe({
      next: resultado => {
        this.resenas.set(resultado.resenas);
        this.resumenResenas.set(resultado.resumen);
        this.cargandoResenas.set(false);
      },
      error: () => {
        this.errorResena.set('No se pudieron cargar las reseñas. Inténtalo nuevamente.');
        this.cargandoResenas.set(false);
      }
    });
  }

  protected seleccionarCalificacion(calificacion: number): void {
    this.resenaForm.controls.calificacion.setValue(calificacion);
    this.resenaForm.controls.calificacion.markAsTouched();
  }

  protected puedeResenar(idProducto: number): boolean {
    return this.autenticacion.cliente() !== null && this.productosResenables().has(idProducto)
      && this.productosResenables().get(idProducto) === false;
  }

  protected yaReseno(idProducto: number): boolean {
    return this.productosResenables().get(idProducto) === true;
  }

  protected publicarResena(idProducto: number): void {
    if (this.resenaForm.invalid || !this.puedeResenar(idProducto) || this.publicandoResena()) {
      this.resenaForm.markAllAsTouched();
      return;
    }
    const { calificacion, comentario } = this.resenaForm.getRawValue();
    this.publicandoResena.set(true);
    this.errorResena.set('');
    this.mensajeResena.set('');
    this.resenasService.publicar(idProducto, calificacion, comentario.trim()).subscribe({
      next: resena => {
        this.resenas.update(items => [resena, ...items]);
        this.resumenResenas.update(resumen => ({
          cantidad: resumen.cantidad + 1,
          promedio: Math.round(((resumen.promedio * resumen.cantidad + calificacion) / (resumen.cantidad + 1)) * 10) / 10
        }));
        this.productosResenables.update(items => new Map(items).set(idProducto, true));
        this.resenaForm.reset({ calificacion: 0, comentario: '' });
        this.mensajeResena.set('Tu reseña se publicó correctamente.');
        this.publicandoResena.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.errorResena.set(respuesta.error?.mensaje ?? 'No se pudo publicar la reseña. Inténtalo nuevamente.');
        this.publicandoResena.set(false);
      }
    });
  }

  protected estrellas(calificacion: number): string {
    return '★'.repeat(calificacion) + '☆'.repeat(5 - calificacion);
  }

  protected fechaResena(fecha: string): string {
    return new Intl.DateTimeFormat('es-CL', { dateStyle: 'medium', timeZone: 'America/Santiago' }).format(new Date(fecha));
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

  protected handleCatalogoChange(control: 'marca' | 'color' | 'categoria', value: string): void {
    const nuevoControl = control === 'marca' ? 'nuevaMarca' : control === 'color' ? 'nuevoColor' : 'nuevaCategoria';
    if (value !== this.valorNuevaOpcion(control)) {
      this.productoForm.controls[nuevoControl].reset('');
    }
  }

  protected agregarOpcion(control: 'marca' | 'color' | 'categoria'): void {
    const nuevoControl = control === 'marca' ? 'nuevaMarca' : control === 'color' ? 'nuevoColor' : 'nuevaCategoria';
    const opciones = control === 'marca' ? this.marcas : control === 'color' ? this.colores : this.categorias;
    const nombre = this.productoForm.controls[nuevoControl].value.trim();
    if (!nombre) {
      this.productoForm.controls[nuevoControl].markAsTouched();
      return;
    }

    const tipo = control === 'marca' ? 'Marca' : control === 'color' ? 'Color' : 'Categoria';
    this.productoService.crearCatalogoItem(tipo, nombre).subscribe({
      next: item => {
        const existente = opciones().find(opcion => opcion.toLowerCase() === item.nombre.toLowerCase());
        if (!existente) {
          opciones.update(items => [...items, item.nombre]);
        }
        this.productoForm.controls[control].setValue(existente ?? item.nombre);
        this.productoForm.controls[nuevoControl].reset('');
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo guardar la nueva opción.');
      }
    });
  }

  protected valorNuevaOpcion(control: 'marca' | 'color' | 'categoria'): string {
    return control === 'marca' ? this.nuevaMarcaValue : control === 'color' ? this.nuevoColorValue : this.nuevaCategoriaValue;
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
      : `${obtenerOrigenApi()}${rutaImagen.startsWith('/') ? '' : '/'}${rutaImagen}`;
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
      marca: this.productoForm.controls.marca.value === this.nuevaMarcaValue ? '' : this.productoForm.controls.marca.value,
      color: this.productoForm.controls.color.value === this.nuevoColorValue ? '' : this.productoForm.controls.color.value,
      categoria: this.productoForm.controls.categoria.value === this.nuevaCategoriaValue ? '' : this.productoForm.controls.categoria.value,
      imagen: this.selectedImage() ?? undefined
    };
    this.productoService.actualizar(producto.id, productoEditado).subscribe({
      next: (actualizado) => {
        this.productoSeleccionado.set(actualizado);
        this.cambiarPagina(this.pagina());
        this.editando.set(false);
        this.guardando.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo actualizar el producto.');
        this.guardando.set(false);
      }
    });
  }

  protected eliminarProducto(): void {
    const producto = this.productoSeleccionado();
    if (!producto || !confirm(`¿Está seguro de que desea eliminar el artículo "${producto.nombre}"?`)) {
      return;
    }

    this.guardando.set(true);
    this.productoService.eliminar(producto.id).subscribe({
      next: () => {
        this.cerrarDetalle();
        this.aplicarFiltros();
        this.guardando.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo eliminar el producto.');
        this.guardando.set(false);
      }
    });
  }
}
