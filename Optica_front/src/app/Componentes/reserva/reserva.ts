import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ReservaService } from './reserva.service';

@Component({
  selector: 'app-reserva',
  imports: [ReactiveFormsModule],
  templateUrl: './reserva.html',
  styleUrl: './reserva.css'
})
export class ReservaComponent {
  protected readonly minDate = new Date().toISOString().split('T')[0];
  protected readonly confirmedName = signal('');
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');

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
      fecha: formValue.date ?? '',
      hora: `${formValue.time ?? ''}:00`
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
