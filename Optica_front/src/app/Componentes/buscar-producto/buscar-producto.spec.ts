import { FormBuilder } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { BuscarProductoComponent } from './buscar-producto';
import { BuscarProductoService, Producto } from './buscar-producto.service';

const productos: Producto[] = [
  {
    id: 1, codigo: 'SOL-001', nombre: 'Lentes de sol', marca: 'Vogue', modelo: 'V1',
    color: 'Negro', categoria: 'Lentes de sol', precio: 30000, stock: 4, stockMinimo: 1,
    estado: 'Disponible', rutaImagen: '/uploads/productos/sol.jpg'
  },
  {
    id: 2, codigo: 'OPT-002', nombre: 'Armazon clasico', marca: 'Ray-Ban', modelo: 'R2',
    color: 'Dorado', categoria: 'Armazones', precio: 15000, stock: 8, stockMinimo: 2,
    estado: 'Disponible'
  }
];

describe('BuscarProductoComponent', () => {
  let component: BuscarProductoComponent;
  let fixture: ComponentFixture<BuscarProductoComponent>;
  let service: {
    buscar: ReturnType<typeof vi.fn>;
    actualizar: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    service = {
      buscar: vi.fn(() => of(productos)),
      actualizar: vi.fn((id: number, producto: Producto) => of({ ...productos[0], ...producto, id }))
    };

    await TestBed.configureTestingModule({
      imports: [BuscarProductoComponent],
      providers: [
        FormBuilder,
        { provide: BuscarProductoService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BuscarProductoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga productos y crea opciones unicas para categoria y color', () => {
    expect(component['productosVisibles']()).toEqual(productos);
    expect(component['categorias']()).toEqual(['Armazones', 'Lentes de sol']);
    expect(component['colores']()).toEqual(['Dorado', 'Negro']);
    expect(service.buscar).toHaveBeenCalledWith('');
  });

  it('filtra por categoria y color y ordena por precio ascendente', () => {
    component['filtrosForm'].setValue({ categoria: 'Armazones', color: 'Dorado', ordenPrecio: 'menor' });
    component['aplicarFiltros']();

    expect(component['productosVisibles']().map((producto: Producto) => producto.id)).toEqual([2]);
    expect(component['mensaje']()).toBe('');
  });

  it('ordena los resultados de mayor a menor precio', () => {
    component['filtrosForm'].patchValue({ ordenPrecio: 'mayor' });
    component['aplicarFiltros']();

    expect(component['productosVisibles']().map((producto: Producto) => producto.precio)).toEqual([30000, 15000]);
  });

  it('informa cuando los filtros no encuentran productos', () => {
    component['filtrosForm'].patchValue({ categoria: 'Accesorios' });
    component['aplicarFiltros']();

    expect(component['productosVisibles']()).toEqual([]);
    expect(component['mensaje']()).toContain('No hay existencias');
  });

  it('normaliza rutas relativas y conserva URLs absolutas', () => {
    expect(component['imageUrl']('/uploads/a.jpg')).toBe('http://localhost:8080/uploads/a.jpg');
    expect(component['imageUrl']('uploads/a.jpg')).toBe('http://localhost:8080/uploads/a.jpg');
    expect(component['imageUrl']('https://cdn.example/a.jpg')).toBe('https://cdn.example/a.jpg');
    expect(component['imageUrl']()).toBe('');
  });

  it('guarda los cambios del popup con la imagen seleccionada', () => {
    const image = new File(['image'], 'nuevo.jpg', { type: 'image/jpeg' });
    component['abrirDetalle'](productos[0]);
    component['activarEdicion']();
    component['productoForm'].patchValue({ nombre: 'Lentes actualizados' });
    component['selectedImage'].set(image);
    component['guardarCambios']();

    expect(service.actualizar).toHaveBeenCalledWith(1, expect.objectContaining({
      nombre: 'Lentes actualizados',
      imagen: image
    }));
    expect(component['editando']()).toBe(false);
    expect(component['guardando']()).toBe(false);
  });

  it('muestra un error cuando falla la consulta', () => {
    service.buscar.mockReturnValueOnce(throwError(() => new Error('offline')));
    component['buscar']();

    expect(component['error']()).toBe('No se pudo consultar el catálogo.');
    expect(component['cargando']()).toBe(false);
  });
});
