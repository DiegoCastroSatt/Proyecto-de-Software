import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CrearPedidoComponent } from './crear-pedido';
import { PedidoService } from '../pedido/pedido.service';

describe('CrearPedidoComponent', () => {
  let fixture: ComponentFixture<CrearPedidoComponent>;
  let servicio: {
    obtenerClientes: ReturnType<typeof vi.fn>;
    crearPedido: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const clientes = [
    { Rut: '12345678-9', nombreCompleto: 'Ana Pérez' },
    { Rut: '98765432-1', nombreCompleto: 'Carlos Soto' }
  ];

  beforeEach(async () => {
    servicio = {
      obtenerClientes: vi.fn(() => of(clientes)),
      crearPedido: vi.fn(() => of({ idPedido: 1 }))
    };

    await TestBed.configureTestingModule({
      imports: [CrearPedidoComponent],
      providers: [
        { provide: PedidoService, useValue: servicio },
        provideRouter([])
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate');
    fixture = TestBed.createComponent(CrearPedidoComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('carga los clientes al iniciar', () => {
    expect(servicio.obtenerClientes).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance['clientes']()).toEqual(clientes);
  });

  it('no crea un pedido si el formulario es inválido', () => {
    const formulario = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    formulario.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(servicio.crearPedido).not.toHaveBeenCalled();
    expect(fixture.componentInstance['pedidoForm'].touched).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('El RUT es obligatorio');
    expect(fixture.nativeElement.textContent).toContain('La fecha es obligatoria');
  });

  it('crea un pedido válido, limpia el formulario y vuelve a la lista', () => {
    vi.useFakeTimers();
    fixture.componentInstance['pedidoForm'].setValue({
      rut: '12345678-9',
      fecha: '2026-10-14',
      estado: 'En proceso',
      total: 12000,
      anotaciones: 'Lentes antireflejo'
    });

    const formulario = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    formulario.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(servicio.crearPedido).toHaveBeenCalledWith({
      rut: '12345678-9',
      fecha: '2026-10-14',
      estado: 'En proceso',
      total: 12000,
      anotaciones: 'Lentes antireflejo'
    });
    expect(fixture.nativeElement.textContent).toContain('Pedido guardado exitosamente.');
    expect(fixture.componentInstance['pedidoForm'].pristine).toBe(true);

    vi.advanceTimersByTime(2000);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/pedido']);
  });

  it('muestra el mensaje devuelto por la API cuando falla la creación', () => {
    servicio.crearPedido.mockReturnValueOnce(
      throwError(() => ({ error: { mensaje: 'El cliente no existe.' } }))
    );
    fixture.componentInstance['pedidoForm'].setValue({
      rut: '12345678-9',
      fecha: '2026-10-14',
      estado: 'Pendiente',
      total: 0,
      anotaciones: ''
    });

    fixture.componentInstance['submitPedido']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error-banner').textContent)
      .toContain('El cliente no existe.');
    expect(fixture.nativeElement.querySelector('.success-banner')).toBeNull();
  });

  it('muestra un error si no se pueden cargar los clientes', () => {
    servicio.obtenerClientes.mockReturnValueOnce(
      throwError(() => new Error('offline'))
    );
    fixture.destroy();
    fixture = TestBed.createComponent(CrearPedidoComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent)
      .toContain('No se pudieron cargar los clientes.');
  });
});
