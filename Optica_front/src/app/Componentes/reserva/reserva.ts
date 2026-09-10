import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ReservaService } from './reserva.service';

@Component({
  selector: 'app-reserva',
  imports: [ReactiveFormsModule],
  templateUrl: './reserva.html',
  styleUrl: './reserva.css'
})
export class ReservaComponent implements OnInit {
  protected readonly minDate = new Date().toISOString().split('T')[0];
  protected readonly confirmedName = signal('');
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
      rut: ['', [Validators.required, Validators.pattern(/^[0-9.]+-[0-9kK]$/)]],
      phone: ['', [Validators.required, Validators.pattern(/^[+0-9 ()-]{8,20}$/)]],
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
    this.reservaService.obtenerDisponibles().subscribe({
      next: (slots) => {
        const normalizedSlots = slots.map((slot) => ({
          idHorario: slot.idHorario,
          fecha: slot.fecha.slice(0, 10),
          hora: slot.hora.slice(0, 5)
        }));
        this.availableSlots.set(normalizedSlots);
        this.availableDates.set([...new Set(normalizedSlots.map((slot) => slot.fecha))]);
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
      next: () => {
        this.confirmedName.set(formValue.name ?? '');
        this.isSubmitting.set(false);
        this.reservationForm.reset();
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
}
