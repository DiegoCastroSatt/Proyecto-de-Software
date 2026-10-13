import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DatePipe } from '@angular/common';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { GestionHorariosComponent } from './gestion-horarios';
import { GestionHorariosService, Horario } from './gestion-horarios.service';

describe('Gestión de horarios', () => {
  const horarios: Horario[] = [
    { idHorario: 1, fecha: '2026-09-22', horaInicio: '09:00', horaFin: '09:20', estado: 'Habilitada' },
    { idHorario: 2, fecha: '2026-09-22', horaInicio: '09:20', horaFin: '09:40', estado: 'Inhabilitada' }
  ];
  const servicio = { listar: vi.fn(), crearBloques: vi.fn(), cambiarEstado: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    servicio.listar.mockReturnValue(of(horarios));
    servicio.crearBloques.mockReturnValue(of([horarios[0]]));
    servicio.cambiarEstado.mockReturnValue(of({ ...horarios[0], estado: 'Inhabilitada' }));
    await TestBed.configureTestingModule({
      imports: [GestionHorariosComponent, DatePipe],
      providers: [{ provide: GestionHorariosService, useValue: servicio }]
    }).compileComponents();
  });

  function preparar() {
    const fixture = TestBed.createComponent(GestionHorariosComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, dom: fixture.nativeElement as HTMLElement };
  }

  it('carga los horarios al iniciar', () => {
    const { dom } = preparar();

    expect(servicio.listar).toHaveBeenCalledTimes(1);
    expect(dom.textContent).toContain('09:00 - 09:20');
    expect(dom.textContent).toContain('Habilitada');
  });

  it('valida el rango antes de crear bloques', () => {
    const { component } = preparar();
    component['scheduleForm'].setValue({
      fecha: '2026-09-22',
      horaInicio: '18:00',
      horaFin: '09:00',
      duracionMinutos: 20
    });

    component['crearBloques']();

    expect(servicio.crearBloques).not.toHaveBeenCalled();
    expect(component['errorMessage']()).toContain('hora de término');
  });

  it('crea bloques y los agrega al listado', () => {
    const { fixture, component, dom } = preparar();
    const nuevos = [{ idHorario: 3, fecha: '2026-12-25', horaInicio: '10:00', horaFin: '10:30', estado: 'Habilitada' as const }];
    servicio.crearBloques.mockReturnValue(of(nuevos));
    component['scheduleForm'].patchValue({
      fecha: '2026-12-25',
      horaInicio: '10:00',
      horaFin: '11:00'
    });

    component['crearBloques']();
    fixture.detectChanges();

    expect(servicio.crearBloques).toHaveBeenCalledWith({
      fecha: '2026-12-25', horaInicio: '10:00', horaFin: '11:00', duracionMinutos: 30
    });
    expect(dom.textContent).toContain('10:00 - 10:30');
    expect(component['successMessage']()).toContain('fueron creados');
  });

  it('cambia el estado de un horario', () => {
    const { fixture, component, dom } = preparar();
    component['cambiarEstado'](horarios[0]);
    fixture.detectChanges();

    expect(servicio.cambiarEstado).toHaveBeenCalledWith(1, 'Inhabilitada');
    expect(dom.textContent).toContain('Inhabilitada');
  });
  it('genera una semana excluyendo los días desmarcados', () => {
    const { component } = preparar();
    component['scheduleForm'].controls.fecha.setValue('2099-12-15');
    component['cambiarPeriodo']('semana');
    const dias = [...component['diasSeleccionados']()];
    expect(dias).toHaveLength(7);
    component['alternarDia'](dias[2]);
    component['crearBloques']();
    expect(servicio.crearBloques).toHaveBeenCalledTimes(6);
    expect(servicio.crearBloques.mock.calls.map(llamada => llamada[0].fecha)).not.toContain(dias[2]);
  });

  it('muestra todos los días del mes y exige al menos uno seleccionado', () => {
    const { component } = preparar();
    component['scheduleForm'].controls.fecha.setValue('2099-02-15');
    component['cambiarPeriodo']('mes');
    expect(component['diasSeleccionados']()).toHaveLength(28);
    component['seleccionarDias'](false);
    component['crearBloques']();
    expect(servicio.crearBloques).not.toHaveBeenCalled();
    expect(component['errorMessage']()).toContain('al menos un día');
  });

  it('conserva solo los días fallidos para reintentar una creación parcial', () => {
    const { component } = preparar();
    component['scheduleForm'].controls.fecha.setValue('2099-12-15');
    component['cambiarPeriodo']('semana');
    const fallido = component['diasSeleccionados']()[1];
    servicio.crearBloques.mockImplementation(request => request.fecha === fallido
      ? throwError(() => ({ error: { mensaje: 'No disponible' } })) : of([]));
    component['crearBloques']();
    expect(servicio.crearBloques).toHaveBeenCalledTimes(7);
    expect(component['diasSeleccionados']()).toEqual([fallido]);
    expect(component['errorMessage']()).toContain(fallido);
    expect(component['isSaving']()).toBe(false);
  });

});
