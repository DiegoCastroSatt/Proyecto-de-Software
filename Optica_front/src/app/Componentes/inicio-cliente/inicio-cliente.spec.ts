import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { InicioClienteComponent } from './inicio-cliente';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';

describe('InicioClienteComponent', () => {
  let fixture: ComponentFixture<InicioClienteComponent>;
  const servicio = { buscar: vi.fn() };
  const producto: Producto = {
    id: 1, codigo: 'AR-1', nombre: 'Armazón clásico', marca: 'Óptica', modelo: 'Uno', color: 'Negro',
    categoria: 'Armazones', precio: 25000, stock: 3, stockMinimo: 1, estado: 'Disponible',
    rutaImagen: '/imagenes/armazon.jpg', tieneVentas: false
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.buscar.mockReturnValue(of([producto]));
    await TestBed.configureTestingModule({
      imports: [InicioClienteComponent],
      providers: [provideRouter([]), { provide: BuscarProductoService, useValue: servicio }]
    }).compileComponents();
  });

  function crearComponente(): void {
    fixture = TestBed.createComponent(InicioClienteComponent);
    fixture.detectChanges();
  }

  it('muestra solo productos disponibles que tienen imagen', () => {
    servicio.buscar.mockReturnValueOnce(of([
      producto,
      { ...producto, id: 2, nombre: 'Sin foto', rutaImagen: '' },
      { ...producto, id: 3, nombre: 'Agotado', estado: 'Agotado' }
    ]));
    crearComponente();

    expect(fixture.nativeElement.textContent).toContain('Armazón clásico');
    expect(fixture.nativeElement.textContent).not.toContain('Sin foto');
    expect(fixture.nativeElement.textContent).not.toContain('Agotado');
    expect((fixture.nativeElement.querySelector('img') as HTMLImageElement).getAttribute('src'))
      .toBe('http://localhost:8080/imagenes/armazon.jpg');
  });

  it('avanza y envuelve el carrusel de productos', () => {
    servicio.buscar.mockReturnValueOnce(of([producto, { ...producto, id: 2, nombre: 'Armazón dos' }]));
    crearComponente();
    (fixture.nativeElement.querySelector('[aria-label="Producto siguiente"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Armazón dos');
    expect(fixture.nativeElement.textContent).toContain('2 / 2');

    (fixture.nativeElement.querySelector('[aria-label="Producto siguiente"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Armazón clásico');
    expect(fixture.nativeElement.textContent).toContain('1 / 2');
  });

  it('muestra error y reintenta la carga', () => {
    servicio.buscar.mockReturnValueOnce(throwError(() => new Error('sin conexión')))
      .mockReturnValueOnce(of([producto]));
    crearComponente();
    expect(fixture.nativeElement.textContent).toContain('No pudimos cargar los productos');

    (fixture.nativeElement.querySelector('[role="alert"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(servicio.buscar).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('Armazón clásico');
  });

  it('muestra alternativa cuando falla la imagen', () => {
    crearComponente();
    (fixture.nativeElement.querySelector('img') as HTMLImageElement).dispatchEvent(new Event('error'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Imagen no disponible');
  });
});
