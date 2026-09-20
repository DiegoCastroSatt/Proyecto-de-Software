import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ProductoComponent } from './registro-producto';
import { RegistrarProductoService } from './registro-producto.service';

describe('Registro de productos', () => {
  const servicio = {
    obtenerCatalogo: vi.fn(),
    crearCatalogoItem: vi.fn(),
    crearProducto: vi.fn()
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.obtenerCatalogo.mockImplementation((tipo: string) => of([
      { tipo, nombre: tipo === 'Marca' ? 'Ray-Ban' : tipo === 'Color' ? 'Negro' : 'Armazones' }
    ]));
    servicio.crearCatalogoItem.mockReturnValue(of({ tipo: 'Marca', nombre: 'Oakley' }));
    servicio.crearProducto.mockReturnValue(of({
      id: 1,
      codigo: 'OPT-001',
      nombre: 'Armazón clásico',
      estado: 'Disponible'
    }));

    await TestBed.configureTestingModule({
      imports: [ProductoComponent],
      providers: [{ provide: RegistrarProductoService, useValue: servicio }]
    }).compileComponents();
  });

  function preparar() {
    const fixture = TestBed.createComponent(ProductoComponent);
    fixture.detectChanges();
    return {
      fixture,
      component: fixture.componentInstance,
      dom: fixture.nativeElement as HTMLElement
    };
  }

  it('carga marcas, colores y categorías al iniciar', () => {
    const { component } = preparar();

    expect(servicio.obtenerCatalogo).toHaveBeenCalledWith('Marca');
    expect(servicio.obtenerCatalogo).toHaveBeenCalledWith('Color');
    expect(servicio.obtenerCatalogo).toHaveBeenCalledWith('Categoria');
    expect(component['marcas']).toEqual(['Ray-Ban']);
    expect(component['colores']).toEqual(['Negro']);
    expect(component['categorias']).toEqual(['Armazones']);
  });

  it('impide guardar un producto inválido', () => {
    const { component } = preparar();

    component['submitProduct']();

    expect(servicio.crearProducto).not.toHaveBeenCalled();
    expect(component['productForm'].touched).toBe(true);
  });

  it('guarda un producto válido y muestra confirmación', () => {
    const { fixture, component, dom } = preparar();
    component['productForm'].setValue({
      codigo: 'OPT-001',
      nombre: 'Armazón clásico',
      marca: 'Ray-Ban',
      nuevaMarca: '',
      modelo: 'RB2140',
      color: 'Negro',
      nuevoColor: '',
      categoria: 'Armazones',
      nuevaCategoria: '',
      precio: '45000',
      stock: 5,
      stockMinimo: 1,
      estado: 'Disponible'
    });

    component['submitProduct']();
    fixture.detectChanges();

    expect(servicio.crearProducto).toHaveBeenCalledWith({
      codigo: 'OPT-001',
      nombre: 'Armazón clásico',
      marca: 'Ray-Ban',
      modelo: 'RB2140',
      color: 'Negro',
      categoria: 'Armazones',
      precio: 45000,
      stock: 5,
      stockMinimo: 1,
      estado: 'Disponible',
      imagen: undefined
    });
    expect(component['savedName']()).toBe('Armazón clásico');
    expect(dom.textContent).toContain('Producto guardado.');
  });

  it('agrega una nueva marca al catálogo y la selecciona', () => {
    const { component } = preparar();
    component['productForm'].controls.marca.setValue('__nueva_marca__');
    component['productForm'].controls.nuevaMarca.setValue('Oakley');

    component['addMarca']();

    expect(servicio.crearCatalogoItem).toHaveBeenCalledWith('Marca', 'Oakley');
    expect(component['marcas']).toContain('Oakley');
    expect(component['productForm'].controls.marca.value).toBe('Oakley');
    expect(component['productForm'].controls.nuevaMarca.value).toBe('');
  });
});
