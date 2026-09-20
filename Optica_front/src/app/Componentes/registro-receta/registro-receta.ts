import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Validators } from '@angular/forms';
import { Graduacion, RegistrarRecetaService } from './registro-receta.service';

@Component({
  selector: 'app-registro-receta',
  imports: [ReactiveFormsModule],
  templateUrl: './registro-receta.html',
  styleUrl: './registro-receta.css'
})
export class RegistroRecetaComponent {
  protected readonly modo = signal<'escrita' | 'imagen'>('escrita');
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly selectedImage = signal<File | null>(null);

  protected readonly recetaForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly recetaService: RegistrarRecetaService
  ) {
    this.recetaForm = this.formBuilder.group({
      rut: ['', [Validators.required, Validators.maxLength(13)]],
      fecha: ['', Validators.required],
      observaciones: [''],
      esferaOD: [0, Validators.required],
      cilindroOD: [0, Validators.required],
      ejeOD: [0, Validators.required],
      adicionOD: [0],
      esferaOI: [0, Validators.required],
      cilindroOI: [0, Validators.required],
      ejeOI: [0, Validators.required],
      adicionOI: [0]
    });
  }

  protected setModo(modo: 'escrita' | 'imagen'): void {
    this.modo.set(modo);
    this.errorMessage.set('');
  }

  protected handleImageChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const image = input.files?.[0] ?? null;
    this.selectedImage.set(image);
  }

  protected submitReceta(): void {
    const modo = this.modo();

    const rutInvalido = this.recetaForm.get('rut')?.invalid;
    const fechaInvalida = this.recetaForm.get('fecha')?.invalid;

    if (rutInvalido || fechaInvalida) {
      this.recetaForm.markAllAsTouched();
      return;
    }

    if (modo === 'escrita') {
      const camposGraduacion = ['esferaOD', 'cilindroOD', 'ejeOD', 'esferaOI', 'cilindroOI', 'ejeOI'];
      const invalido = camposGraduacion.some((campo) => this.recetaForm.get(campo)?.invalid);
      if (invalido) {
        this.recetaForm.markAllAsTouched();
        return;
      }
    }

    if (modo === 'imagen' && !this.selectedImage()) {
      this.errorMessage.set('Debes seleccionar una imagen.');
      return;
    }

    const formValue = this.recetaForm.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    let graduaciones: Graduacion[] | undefined;
    if (modo === 'escrita') {
      graduaciones = [
        {
          ojo: 'OD',
          esfera: Number(formValue.esferaOD),
          cilindro: Number(formValue.cilindroOD),
          eje: Number(formValue.ejeOD),
          adicion: Number(formValue.adicionOD ?? 0)
        },
        {
          ojo: 'OI',
          esfera: Number(formValue.esferaOI),
          cilindro: Number(formValue.cilindroOI),
          eje: Number(formValue.ejeOI),
          adicion: Number(formValue.adicionOI ?? 0)
        }
      ];
    }

    this.recetaService.crearReceta({
      rut: formValue.rut ?? '',
      fecha: formValue.fecha ?? '',
      observaciones: formValue.observaciones ?? undefined,
      graduaciones,
      imagen: modo === 'imagen' ? this.selectedImage() ?? undefined : undefined
    }).subscribe({
      next: () => {
        this.successMessage.set('Receta registrada correctamente.');
        this.isSubmitting.set(false);
        this.selectedImage.set(null);
        this.recetaForm.reset();
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudo registrar la receta. Inténtalo nuevamente.');
        this.isSubmitting.set(false);
      }
    });
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.recetaForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }
}