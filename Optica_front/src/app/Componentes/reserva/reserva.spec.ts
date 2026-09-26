import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ReservaComponent } from './reserva';
import { ReservaService } from './reserva.service';

describe('Reservas', () => {
  const horarios = [
    { idHorario: 10, fecha: '2026-09-22T00:00:00', hora: '09:00:00' },
    { idHorario: 11, fecha: '2026-09-22T00:00:00', hora: '09:20:00' },
    { idHorario: 12, fecha: '2026-09-23T00:00:00', hora: '10:00:00' }
  ];
  const servicio = { obtenerDisponibles: vi.fn(), crearReserva: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.obtenerDisponibles.mockReturnValue(of(horarios));
    servicio.crearReserva.mockReturnValue(of({ id: 1, fecha: '2026-09-22', hora: '09:00', estado: 'Pendiente' }));
    await TestBed.configureTestingModule({
      imports: [ReservaComponent],
      providers: [{ provide: ReservaService, useValue: servicio }]
    }).compileComponents();
  });

  function preparar() {
    const fixture = TestBed.createComponent(ReservaComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, dom: fixture.nativeElement as HTMLElement };
  }

  it('carga los días disponibles al iniciar', () => {
    const { dom } = preparar();

    expect(servicio.obtenerDisponibles).toHaveBeenCalledTimes(1);
    expect(dom.querySelectorAll('#date option').length).toBe(3);
  });

  it('actualiza las horas cuando cambia el día', () => {
    const { fixture, component, dom } = preparar();
    component['reservationForm'].controls.date.setValue('2026-09-22');
    fixture.detectChanges();

    const opciones = Array.from(dom.querySelectorAll<HTMLSelectElement>('#time option'))
      .map(opcion => opcion.value);
    expect(opciones).toEqual(['', '10', '11']);
  });

  it('impide enviar una reserva incompleta', () => {
    const { component } = preparar();

    component['submitReservation']();

    expect(servicio.crearReserva).not.toHaveBeenCalled();
    expect(component['reservationForm'].touched).toBe(true);
  });

  it('impide enviar una reserva con dígito verificador inválido', () => {
    const { component } = preparar();
    component['reservationForm'].setValue({
      name: 'Ana Pérez',
      rut: '12.345.678-9',
      phone: '+56 9 1234 5678',
      email: 'ana@example.com',
      date: '2026-09-22',
      time: '10'
    });

    component['submitReservation']();

    expect(servicio.crearReserva).not.toHaveBeenCalled();
    expect(component['reservationForm'].controls.rut.hasError('rutInvalido')).toBe(true);
  });

  it('impide enviar una reserva con un teléfono de menos de nueve dígitos', () => {
    const { component } = preparar();
    component['reservationForm'].setValue({
      name: 'Ana Pérez',
      rut: '12.345.678-5',
      phone: '91234567',
      email: 'ana@example.com',
      date: '2026-09-22',
      time: '10'
    });

    component['submitReservation']();

    expect(servicio.crearReserva).not.toHaveBeenCalled();
    expect(component['reservationForm'].controls.phone.hasError('pattern')).toBe(true);
  });

  it('envía una reserva válida y muestra confirmación', () => {
    const { fixture, component, dom } = preparar();
    component['reservationForm'].setValue({
      name: 'Ana Pérez',
      rut: '12.345.678-5',
      phone: '+56 9 1234 5678',
      email: 'ana@example.com',
      date: '2026-09-22',
      time: '10'
    });

    component['submitReservation']();
    fixture.detectChanges();

    expect(servicio.crearReserva).toHaveBeenCalledWith({
      nombreCompleto: 'Ana Pérez',
      rut: '12.345.678-5',
      telefono: '+56 9 1234 5678',
      correo: 'ana@example.com',
      idHorario: 10
    });
    expect(dom.textContent).toContain('Solicitud recibida, Ana Pérez');
  });
});