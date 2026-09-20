import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
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
  private readonly formBuilder = inject(FormBuilder);
  private readonly horariosService = inject(GestionHorariosService);

  protected readonly horarios = signal<Horario[]>([]);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly changingId = signal<number | null>(null);
  protected readonly filterDate = signal('');

  protected readonly scheduleForm = this.formBuilder.group({
    fecha: ['', Validators.required],
    horaInicio: ['09:00', Validators.required],
    horaFin: ['18:00', Validators.required],
    duracionMinutos: [20, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.loadSchedules();
  }

  protected loadSchedules(): void {
    this.isLoading.set(true);
    this.horariosService.listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (horarios) => {
          this.horarios.set(horarios);
          this.isLoading.set(false);
        },
        error: (error: { error?: { mensaje?: string } }) => {
          this.errorMessage.set(error.error?.mensaje ?? 'No se pudieron cargar los horarios.');
          this.isLoading.set(false);
        }
      });
  }

  protected crearBloques(): void {
    if (this.scheduleForm.invalid) {
      this.scheduleForm.markAllAsTouched();
      return;
    }

    const { fecha, horaInicio, horaFin, duracionMinutos } = this.scheduleForm.getRawValue();
    if (!fecha || !horaInicio || !horaFin || !duracionMinutos || horaInicio >= horaFin) {
      this.errorMessage.set('La hora de término debe ser posterior a la hora de inicio.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
    this.horariosService.crearBloques({
      fecha,
      horaInicio,
      horaFin,
      duracionMinutos
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (horarios) => {
        this.horarios.update(actuales => [...actuales, ...horarios]);
        this.successMessage.set('Los bloques de atención fueron creados.');
        this.isSaving.set(false);
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudieron crear los bloques de atención.');
        this.isSaving.set(false);
      }
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
}
