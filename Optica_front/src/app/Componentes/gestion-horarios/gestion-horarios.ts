import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { from, of, concatMap, catchError, toArray } from 'rxjs';
import { DatePipe, isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  EstadoHorario,
  GestionHorariosService,
  Horario
} from './gestion-horarios.service';

@Component({
  selector: 'app-gestion-horarios',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './gestion-horarios.html',
  styleUrl: './gestion-horarios.css',
})
export class GestionHorariosComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly formBuilder = inject(FormBuilder);
  private readonly horariosService = inject(GestionHorariosService);

  protected readonly horarios = signal<Horario[]>([]);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly changingId = signal<number | null>(null);
  protected readonly filterDate = signal('');
  protected readonly fechaMinima = signal(this.obtenerFechaChile());
  protected readonly horaMinima = signal('00:00');

  protected readonly periodo = signal<'dia' | 'semana' | 'mes'>('dia');
  protected readonly diasCalendario = signal<(string | null)[]>([]);
  protected readonly diasSeleccionados = signal<string[]>([]);
  protected readonly nombresDias = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];

  protected cambiarPeriodo(valor: string): void {
    if (valor !== 'dia' && valor !== 'semana' && valor !== 'mes') return;
    this.periodo.set(valor);
    if (!this.scheduleForm.controls.fecha.value) {
      this.scheduleForm.controls.fecha.setValue(this.obtenerFechaChile());
    }
    this.actualizarCalendario();
  }

  private actualizarCalendario(): void {
    const fecha = this.scheduleForm.controls.fecha.value;
    if (!fecha) {
      this.diasCalendario.set([]);
      this.diasSeleccionados.set([]);
      return;
    }
    const base = new Date(`${fecha}T12:00:00`);
    const inicio = new Date(base);
    let cantidad = 1;
    let espacios = 0;
    if (this.periodo() === 'semana') {
      inicio.setDate(inicio.getDate() - (inicio.getDay() + 6) % 7);
      cantidad = 7;
    } else if (this.periodo() === 'mes') {
      inicio.setDate(1);
      cantidad = new Date(base.getFullYear(), base.getMonth() + 1, 0).getDate();
      espacios = (inicio.getDay() + 6) % 7;
    }
    const dias: (string | null)[] = Array(espacios).fill(null);
    for (let i = 0; i < cantidad; i++) {
      const dia = new Date(inicio);
      dia.setDate(inicio.getDate() + i);
      dias.push(`${dia.getFullYear()}-${String(dia.getMonth() + 1).padStart(2, '0')}-${String(dia.getDate()).padStart(2, '0')}`);
    }
    this.diasCalendario.set(dias);
    this.diasSeleccionados.set(dias.filter((dia): dia is string => dia !== null && dia >= this.obtenerFechaChile()));
    this.actualizarLimites(fecha);
  }

  protected alternarDia(fecha: string): void {
    this.diasSeleccionados.update(dias => dias.includes(fecha)
      ? dias.filter(dia => dia !== fecha) : [...dias, fecha].sort());
    this.actualizarLimites(this.scheduleForm.controls.fecha.value);
  }

  protected seleccionarDias(seleccionar: boolean): void {
    this.diasSeleccionados.set(seleccionar
      ? this.diasCalendario().filter((dia): dia is string => dia !== null && dia >= this.fechaMinima()) : []);
    this.actualizarLimites(this.scheduleForm.controls.fecha.value);
  }

  protected limpiar(): void {
    this.scheduleForm.reset({ fecha: '', horaInicio: '09:00', horaFin: '18:00', duracionMinutos: 30 });
    this.actualizarCalendario();
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  protected readonly scheduleForm = this.formBuilder.group({
    fecha: ['', Validators.required],
    horaInicio: ['09:00', Validators.required],
    horaFin: ['18:00', Validators.required],
    duracionMinutos: [30, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.loadSchedules();
    this.scheduleForm.controls.fecha.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.actualizarCalendario());

    if (this.isBrowser) {
      const refreshId = setInterval(() => this.loadSchedules(false), 60_000);
      this.destroyRef.onDestroy(() => clearInterval(refreshId));
    }
  }

  protected loadSchedules(mostrarCarga = true): void {
    this.actualizarLimites(this.scheduleForm.controls.fecha.value);
    if (mostrarCarga) this.isLoading.set(true);
    this.errorMessage.set('');
    this.horariosService.listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (horarios) => {
          this.horarios.set(horarios);
          if (mostrarCarga) this.isLoading.set(false);
        },
        error: (error: { error?: { mensaje?: string } }) => {
          this.errorMessage.set(error.error?.mensaje ?? 'No se pudieron cargar los horarios.');
          if (mostrarCarga) this.isLoading.set(false);
        }
      });
  }

  protected crearBloques(): void {
    if (this.isSaving()) return;
    if (this.scheduleForm.invalid) {
      this.scheduleForm.markAllAsTouched();
      return;
    }

    const { fecha, horaInicio, horaFin, duracionMinutos } = this.scheduleForm.getRawValue();
    if (!fecha || !horaInicio || !horaFin || !duracionMinutos || horaInicio >= horaFin) {
      this.errorMessage.set('La hora de término debe ser posterior a la hora de inicio.');
      return;
    }

    const fechas = this.periodo() === 'dia' ? [fecha] : [...this.diasSeleccionados()].sort();
    if (!fechas.length) {
      this.errorMessage.set('Selecciona al menos un día de atención en el calendario.');
      return;
    }
    const minutos = (hora: string) => Number(hora.slice(0, 2)) * 60 + Number(hora.slice(3, 5));
    if (minutos(horaFin) - minutos(horaInicio) < duracionMinutos) {
      this.errorMessage.set('El rango debe permitir al menos un bloque completo.');
      return;
    }
    const hoyChile = this.obtenerFechaChile();
    if (fechas.some(dia => dia < hoyChile || (dia === hoyChile && horaInicio <= this.obtenerHoraChile()))) {
      this.errorMessage.set('No se pueden crear bloques que ya comenzaron según la hora de Chile.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
    from(fechas).pipe(
      concatMap(dia => this.horariosService.crearBloques({
        fecha: dia, horaInicio, horaFin, duracionMinutos
      }).pipe(
        concatMap(horarios => of({ dia, horarios, error: '' })),
        catchError((error: { error?: { mensaje?: string } }) => of({
          dia, horarios: [] as Horario[], error: error.error?.mensaje ?? 'No se pudieron crear los bloques.'
        }))
      )),
      toArray(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(resultados => {
      const correctos = resultados.filter(resultado => !resultado.error);
      const fallidos = resultados.filter(resultado => resultado.error);
      this.horarios.update(actuales => [...actuales, ...correctos.flatMap(resultado => resultado.horarios)]
        .sort((a, b) => a.fecha.localeCompare(b.fecha) || a.horaInicio.localeCompare(b.horaInicio)));
      if (correctos.length) {
        this.successMessage.set(`Los bloques de atención fueron creados para ${correctos.length} día(s).`);
      }
      if (fallidos.length) {
        this.errorMessage.set(fallidos.map(resultado => `${resultado.dia}: ${resultado.error}`).join(' · '));
      }
      if (this.periodo() !== 'dia') this.diasSeleccionados.set(fallidos.map(resultado => resultado.dia));
      this.isSaving.set(false);
    });
  }

  protected cambiarEstado(horario: Horario): void {
    const nuevoEstado: EstadoHorario = horario.estado === 'Habilitada'
      ? 'Inhabilitada'
      : 'Habilitada';

    this.changingId.set(horario.idHorario);
    this.errorMessage.set('');
    this.horariosService.cambiarEstado(horario.idHorario, nuevoEstado)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (actualizado) => {
          this.horarios.update(actuales => actuales.map(item =>
            item.idHorario === actualizado.idHorario ? actualizado : item));
          this.changingId.set(null);
        },
        error: (error: { error?: { mensaje?: string } }) => {
          this.errorMessage.set(error.error?.mensaje ?? 'No se pudo cambiar el estado del horario.');
          this.changingId.set(null);
        }
      });
  }

  protected horariosFiltrados(): Horario[] {
    const filtro = this.filterDate();
    return filtro ? this.horarios().filter(horario => horario.fecha.slice(0, 10) === filtro) : this.horarios();
  }

  protected formHasError(controlName: string): boolean {
    const control = this.scheduleForm.get(controlName);
    return Boolean(control?.touched && control.invalid);
  }

  private obtenerFechaChile(): string {
    const partes = new Intl.DateTimeFormat('en-CA', {
      timeZone: 'America/Santiago', year: 'numeric', month: '2-digit', day: '2-digit'
    }).formatToParts(new Date());
    const valor = (tipo: string) => partes.find(parte => parte.type === tipo)?.value ?? '';
    return `${valor('year')}-${valor('month')}-${valor('day')}`;
  }

  private obtenerHoraChile(): string {
    const partes = new Intl.DateTimeFormat('en-GB', {
      timeZone: 'America/Santiago', hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
    }).formatToParts(new Date());
    const valor = (tipo: string) => partes.find(parte => parte.type === tipo)?.value ?? '';
    return `${valor('hour')}:${valor('minute')}`;
  }

  private actualizarLimites(fechaSeleccionada: string | null): void {
    const hoyChile = this.obtenerFechaChile();
    this.fechaMinima.set(hoyChile);
    this.horaMinima.set((this.periodo() === 'dia' ? fechaSeleccionada === hoyChile : this.diasSeleccionados().includes(hoyChile)) ? this.obtenerHoraChile() : '00:00');
  }
}
