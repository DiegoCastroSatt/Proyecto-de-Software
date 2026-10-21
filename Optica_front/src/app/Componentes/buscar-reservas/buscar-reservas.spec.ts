import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, vi } from 'vitest';
import { BuscarReservas } from './buscar-reservas';
import { AgendaReservasService, ReservaAgenda } from './agenda-reservas.service';

describe('Agenda de reservas', () => {
  const reserva: ReservaAgenda = { id: 1, fecha: '2099-10-13', horaInicio: '09:00:00', horaFin: '09:30:00', estado: 'Pendiente', motivo: 'Control', nombreCliente: 'Ana Pérez', rutCliente: '12345678-5', telefonoCliente: '912345678', correoCliente: 'ana@example.test' };
  const horario = { idHorario: 9, fecha: '2099-10-14', hora: '10:00:00', horaFin: '10:30:00' };
  const servicio = { listar: vi.fn(), disponibles: vi.fn(), cancelar: vi.fn(), reprogramar: vi.fn() };
  let fixture: ComponentFixture<BuscarReservas>;
  let component: BuscarReservas;

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.listar.mockReturnValue(of([reserva]));
    servicio.disponibles.mockReturnValue(of([horario]));
    servicio.cancelar.mockReturnValue(of(undefined));
    servicio.reprogramar.mockReturnValue(of(undefined));
    await TestBed.configureTestingModule({ imports: [BuscarReservas], providers: [{ provide: AgendaReservasService, useValue: servicio }] }).compileComponents();
    fixture = TestBed.createComponent(BuscarReservas);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  afterEach(() => { fixture.destroy(); vi.restoreAllMocks(); });

  it('carga y muestra las reservas pendientes', () => {
    expect(servicio.listar).toHaveBeenCalledWith(false);
    expect(component.reservas()).toEqual([reserva]);
    expect(component.cargando()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Ana Pérez');
  });
  it('usa el estado real del backend para distinguir una reserva confirmada', () => {
    const confirmada = { ...reserva, estado: 'Confirmada' };
    servicio.listar.mockReturnValueOnce(of([confirmada]));

    component.mostrarPorAtender();
    fixture.detectChanges();

    const insignia = fixture.nativeElement.querySelector('.reserva-estado') as HTMLElement;
    expect(insignia.textContent).toBe('Confirmada');
    expect(insignia.classList.contains('confirmada')).toBe(true);
  });
  it('refleja en la lista el estado nuevo recibido del backend al actualizarla', () => {
    const realizada = { ...reserva, estado: 'Realizada' };
    servicio.listar.mockReturnValueOnce(of([realizada]));

    component['cargarReservas'](true, true);
    fixture.detectChanges();

    const insignia = fixture.nativeElement.querySelector('.reserva-estado') as HTMLElement;
    expect(insignia.textContent).toBe('Realizada');
    expect(insignia.classList.contains('atendida')).toBe(true);
  });
  it('cambia entre historial y pendientes y cierra el detalle', () => {
    component.abrirDetalle(reserva);
    component.mostrarAtendidas();
    expect(servicio.listar).toHaveBeenLastCalledWith(true);
    expect(component.mostrarHistorial()).toBe(true);
    expect(component.reservaSeleccionada()).toBeNull();
    component.mostrarPorAtender();
    expect(servicio.listar).toHaveBeenLastCalledWith(false);
    expect(component.mostrarHistorial()).toBe(false);
  });
  it('muestra un error de carga y permite volver a cargar', () => {
    servicio.listar.mockReturnValueOnce(throwError(() => new Error('Sin conexión')));
    component.mostrarAtendidas();
    expect(component.error()).toContain('No se pudieron cargar');
    expect(component.cargando()).toBe(false);
    component.mostrarPorAtender();
    expect(component.error()).toBeNull();
  });
  it('carga disponibilidad excluyendo la reserva actual y reinicia la selección al cambiar fecha', () => {
    component.abrirReprogramacion(reserva);
    expect(servicio.disponibles).toHaveBeenCalledWith('2099-10-13', 1);
    component.horarioSeleccionado.set(9);
    component.cambiarFecha('2099-10-14', 1);
    expect(servicio.disponibles).toHaveBeenLastCalledWith('2099-10-14', 1);
    expect(component.horarioSeleccionado()).toBeNull();
    expect(component.horariosDisponibles()).toEqual([horario]);
  });
  it('ignora una respuesta antigua si cambia la fecha durante la carga', () => {
    const solicitudAntigua = new Subject<typeof horario[]>();
    const horarioNuevo = { ...horario, fecha: '2099-10-15', idHorario: 10 };
    servicio.disponibles.mockReturnValueOnce(solicitudAntigua).mockReturnValueOnce(of([horarioNuevo]));

    component.abrirReprogramacion(reserva);
    component.cambiarFecha('2099-10-15', reserva.id);
    solicitudAntigua.next([horario]);
    solicitudAntigua.complete();

    expect(component.horariosDisponibles()).toEqual([horarioNuevo]);
  });
  it.each(['Cancelada', 'Realizada'])('impide reprogramar una reserva %s', estado => {
    component.abrirReprogramacion({ ...reserva, estado });
    expect(servicio.disponibles).not.toHaveBeenCalled();
    expect(component.mostrarReprogramacion()).toBe(false);
  });
  it('no consulta disponibilidad con fecha vacía', () => {
    component.cambiarFecha('', 1);
    expect(servicio.disponibles).not.toHaveBeenCalled();
    expect(component.horariosDisponibles()).toEqual([]);
  });
  it('muestra errores de disponibilidad y termina la carga', () => {
    servicio.disponibles.mockReturnValue(throwError(() => ({ error: { mensaje: 'Fecha inválida' } })));
    component.abrirReprogramacion(reserva);
    expect(component.mensajeReprogramacion()).toBe('Fecha inválida');
    expect(component.cargandoHorarios()).toBe(false);
    expect(component.horariosDisponibles()).toEqual([]);
  });
  it('no reprograma sin seleccionar un horario', () => {
    component.confirmarReprogramacion(reserva);
    expect(servicio.reprogramar).not.toHaveBeenCalled();
  });
  it('actualiza el listado y el detalle después de reprogramar', () => {
    component.abrirDetalle(reserva);
    component.abrirReprogramacion(reserva);
    component.horarioSeleccionado.set(9);
    component.confirmarReprogramacion(reserva);
    expect(servicio.reprogramar).toHaveBeenCalledWith(1, 9);
    expect(component.reservas()[0]).toEqual({ ...reserva, fecha: horario.fecha, horaInicio: horario.hora, horaFin: horario.horaFin });
    expect(component.reservaSeleccionada()).toEqual(component.reservas()[0]);
    expect(component.mostrarReprogramacion()).toBe(false);
    expect(component.reprogramando()).toBe(false);
    expect(component.mensajeExitoReprogramacion()).toBe('La reserva fue reprogramada correctamente.');
    expect(component.mensajeExitoReprogramacion()).not.toContain('se envió');
  });
  it('informa que el correo quedó pendiente de reintento sin afirmar que se envió', () => {
    servicio.reprogramar.mockReturnValueOnce(of({ correoEnviado: false, mensaje: 'Reserva guardada; correo pendiente.' }));
    component.abrirDetalle(reserva);
    component.abrirReprogramacion(reserva);
    component.horarioSeleccionado.set(9);

    component.confirmarReprogramacion(reserva);

    expect(component.mensajeExitoReprogramacion()).toBe('Reserva guardada; correo pendiente.');
    expect(component.reservas()[0].fecha).toBe(horario.fecha);
  });
  it('evita envíos duplicados y cerrar el detalle mientras reprograma', () => {
    const pendiente = new Subject<void>();
    servicio.reprogramar.mockReturnValue(pendiente);
    component.abrirDetalle(reserva);
    component.abrirReprogramacion(reserva);
    component.horarioSeleccionado.set(9);
    component.confirmarReprogramacion(reserva);
    component.confirmarReprogramacion(reserva);
    component.cerrarDetalle();
    expect(servicio.reprogramar).toHaveBeenCalledTimes(1);
    expect(component.reservaSeleccionada()).toEqual(reserva);
    expect(component.mostrarReprogramacion()).toBe(true);
    pendiente.next(); pendiente.complete();
  });
  it('conserva la reserva y refresca disponibilidad ante un conflicto', () => {
    servicio.reprogramar.mockReturnValue(throwError(() => ({ error: { mensaje: 'Horario ocupado' } })));
    component.abrirReprogramacion(reserva);
    component.horarioSeleccionado.set(9);
    component.confirmarReprogramacion(reserva);
    expect(component.reservas()).toEqual([reserva]);
    expect(component.mensajeReprogramacion()).toBe('Horario ocupado');
    expect(component.horarioSeleccionado()).toBeNull();
    expect(component.reprogramando()).toBe(false);
    expect(servicio.disponibles).toHaveBeenCalledTimes(2);
  });
  it('no cancela si el usuario rechaza la confirmación', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    component.cancelarReserva(reserva);
    expect(servicio.cancelar).not.toHaveBeenCalled();
  });
  it.each(['Cancelada', 'Realizada'])('impide cancelar una reserva %s', estado => {
    const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(true);
    component.cancelarReserva({ ...reserva, estado });
    expect(confirmar).not.toHaveBeenCalled();
    expect(servicio.cancelar).not.toHaveBeenCalled();
  });
  it('cancela y recarga la agenda cerrando el detalle', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    component.abrirDetalle(reserva);
    component.cancelarReserva(reserva);
    expect(servicio.cancelar).toHaveBeenCalledWith(1);
    expect(servicio.listar).toHaveBeenCalledTimes(2);
    expect(component.reservaSeleccionada()).toBeNull();
    expect(component.cancelando()).toBe(false);
  });
  it('evita cancelaciones duplicadas mientras espera respuesta', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const pendiente = new Subject<void>();
    servicio.cancelar.mockReturnValue(pendiente);
    component.cancelarReserva(reserva);
    component.cancelarReserva(reserva);
    expect(servicio.cancelar).toHaveBeenCalledTimes(1);
    expect(component.cancelando()).toBe(true);
    pendiente.next(); pendiente.complete();
  });
  it('muestra el error de cancelación conservando el detalle', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    servicio.cancelar.mockReturnValue(throwError(() => ({ error: { mensaje: 'Ya realizada' } })));
    component.abrirDetalle(reserva);
    component.cancelarReserva(reserva);
    expect(component.mensajeCancelacion()).toBe('Ya realizada');
    expect(component.reservaSeleccionada()).toEqual(reserva);
    expect(component.cancelando()).toBe(false);
  });
});
