import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { validarRutChileno } from '../../shared/validators/rut-chileno.validator';
import { ReservaService } from './reserva.service';

@Component({
  selector: 'app-reserva',
  imports: [ReactiveFormsModule],
  templateUrl: './reserva.html',
  styleUrl: './reserva.css'
})
export class ReservaComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  protected readonly minDate = this.obtenerFechaChile();
  protected readonly confirmedName = signal('');
  protected readonly confirmationEmailSent = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly availableDates = signal<string[]>([]);
  protected readonly availableTimes = signal<{ idHorario: number; hora: string }[]>([]);
  private readonly availableSlots = signal<{ idHorario: number; fecha: string; hora: string }[]>([]);

  protected readonly reservationForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly reservaService: ReservaService
  ) {
    this.reservationForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      rut: ['', [Validators.required, validarRutChileno]],
      phone: ['', [Validators.required, Validators.pattern(/^(\+?56\s?)?9\s?\d{4}\s?\d{4}$/)]],
      email: ['', [Validators.required, Validators.email]],
      date: ['', Validators.required],
      time: ['', Validators.required]
    });

    this.reservationForm.controls.date.valueChanges.subscribe((date) => {
      this.availableTimes.set(
        this.availableSlots().filter((slot) => slot.fecha === date).map(({ idHorario, hora }) => ({ idHorario, hora }))
      );
      this.reservationForm.controls.time.reset('');
    });
  }

  ngOnInit(): void {
    this.cargarHorariosDisponibles();
    if (this.isBrowser) {
      const refreshId = setInterval(() => this.cargarHorariosDisponibles(), 60_000);
      this.destroyRef.onDestroy(() => clearInterval(refreshId));
    }
  }

  private cargarHorariosDisponibles(): void {
    this.reservaService.obtenerDisponibles().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (slots) => {
        const normalizedSlots = slots.map((slot) => ({
          idHorario: slot.idHorario,
          fecha: slot.fecha.slice(0, 10),
          hora: slot.hora.slice(0, 5)
        }));
        this.availableSlots.set(normalizedSlots);
        this.availableDates.set([...new Set(normalizedSlots.map((slot) => slot.fecha))]);
        const fechaSeleccionada = this.reservationForm.controls.date.value;
        this.availableTimes.set(normalizedSlots
          .filter((slot) => slot.fecha === fechaSeleccionada)
          .map(({ idHorario, hora }) => ({ idHorario, hora })));
        const idHorarioSeleccionado = Number(this.reservationForm.controls.time.value);
        if (idHorarioSeleccionado && !normalizedSlots.some((slot) => slot.idHorario === idHorarioSeleccionado)) {
          this.reservationForm.controls.time.reset('');
        }
      },
      error: () => this.errorMessage.set('No hay horarios disponibles en este momento.')
    });
  }

  protected submitReservation(): void {
    if (this.reservationForm.invalid) {
      this.reservationForm.markAllAsTouched();
      return;
    }

    const formValue = this.reservationForm.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set('');

    this.reservaService.crearReserva({
      nombreCompleto: formValue.name ?? '',
      rut: formValue.rut ?? '',
      telefono: formValue.phone ?? '',
      correo: formValue.email ?? '',
      idHorario: Number(formValue.time ?? 0)
    }).subscribe({
      next: (response) => {
        this.confirmedName.set(formValue.name ?? '');
        this.confirmationEmailSent.set(response.correoEnviado);
        this.isSubmitting.set(false);
        this.reservationForm.reset();
        this.cargarHorariosDisponibles();
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(
          error.error?.mensaje ?? 'No se pudo enviar la reserva. Inténtalo nuevamente.'
        );
        this.isSubmitting.set(false);
      }
    });
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.reservationForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }

  private obtenerFechaChile(): string {
    const partes = new Intl.DateTimeFormat('en-CA', {
      timeZone: 'America/Santiago', year: 'numeric', month: '2-digit', day: '2-digit'
    }).formatToParts(new Date());
    const valor = (tipo: string) => partes.find(parte => parte.type === tipo)?.value ?? '';
    return `${valor('year')}-${valor('month')}-${valor('day')}`;
  }
}
