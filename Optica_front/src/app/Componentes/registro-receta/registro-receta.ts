import { Component, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Validators } from '@angular/forms';
import { Graduacion, RegistrarRecetaService } from './registro-receta.service';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';

@Component({
  selector: 'app-registro-receta',
  imports: [ReactiveFormsModule, AutocompletadoRut, RouterLink],
  templateUrl: './registro-receta.html',
  styleUrl: './registro-receta.css'
})
export class RegistroRecetaComponent implements OnInit {
  @ViewChild('imagenInput') private imagenInput?: ElementRef<HTMLInputElement>;
  protected readonly recetaId: number | null;
  protected readonly isLoading = signal(false);
  protected readonly cargaFallida = signal(false);
  protected readonly imagenActual = signal<string | null>(null);
  protected readonly apiBase = 'http://localhost:8080';
  protected readonly rutOriginal = signal('');
  protected readonly modo = signal<'escrita' | 'imagen'>('escrita');
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly selectedImage = signal<File | null>(null);

  protected readonly rut = signal('');
  protected readonly rutTouched = signal(false);

  protected readonly recetaForm;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly recetaService: RegistrarRecetaService,
    private readonly route: ActivatedRoute
  ) {
    const id = this.route.snapshot.paramMap.get('id');
    this.recetaId = id === null ? null : Number(id);
    this.recetaForm = this.formBuilder.group({
      fecha: ['', Validators.required],
      observaciones: [''],
      esferaOD: [null as number | null, Validators.required],
      cilindroOD: [null as number | null, Validators.required],
      ejeOD: [null as number | null, Validators.required],
      adicionOD: [null as number | null],
      esferaOI: [null as number | null, Validators.required],
      cilindroOI: [null as number | null, Validators.required],
      ejeOI: [null as number | null, Validators.required],
      adicionOI: [null as number | null]
    });
  }

  ngOnInit(): void {
    if (this.recetaId === null) return;
    this.isLoading.set(true);
    this.recetaService.obtenerReceta(this.recetaId).subscribe({
      next: (receta) => {
        this.rut.set(receta.rut);
        this.rutOriginal.set(receta.rut);
        this.imagenActual.set(receta.imagenUrl ?? null);
        this.modo.set(receta.graduaciones.length ? 'escrita' : 'imagen');
        this.recetaForm.patchValue({ fecha: receta.fecha.slice(0, 10), observaciones: receta.observaciones ?? '' });
        for (const g of receta.graduaciones) {
          for (const campo of ['esfera', 'cilindro', 'eje', 'adicion'] as const) {
            this.recetaForm.get(`${campo}${g.ojo}`)?.setValue(g[campo]);
          }
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar la receta seleccionada.');
        this.cargaFallida.set(true);
        this.isLoading.set(false);
      }
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

  protected rutInvalido(): boolean {
    const valor = this.rut().trim();
    return !valor || valor.length > 13;
  }

  protected submitReceta(): void {
    if (this.isSubmitting() || this.isLoading() || this.cargaFallida()) return;
    const campos = ['esfera', 'cilindro', 'eje', 'adicion'];
    const ojos = (['OD', 'OI'] as const).filter(ojo =>
      campos.some(campo => this.recetaForm.get(`${campo}${ojo}`)?.value != null));
    const tieneImagen = Boolean(this.selectedImage() || this.imagenActual());
    const fechaInvalida = this.recetaForm.get('fecha')?.invalid;

    if (this.rutInvalido() || fechaInvalida) {
      this.rutTouched.set(true);
      this.recetaForm.markAllAsTouched();
      return;
    }

    for (const ojo of ojos) {
      if (['esfera', 'cilindro', 'eje'].some(campo => this.recetaForm.get(`${campo}${ojo}`)?.invalid)) {
        this.modo.set('escrita');
        this.recetaForm.markAllAsTouched();
        this.errorMessage.set('Completa la esfera, el cilindro y el eje de cada ojo que ingresaste.');
        return;
      }
    }

    if (!ojos.length && !tieneImagen) {
      this.errorMessage.set('Ingresa los datos escritos o selecciona una imagen de la receta.');
      return;
    }

    const formValue = this.recetaForm.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const graduaciones: Graduacion[] | undefined = ojos.length ? ojos.map(ojo => ({
      ojo,
      esfera: Number(this.recetaForm.get(`esfera${ojo}`)?.value),
      cilindro: Number(this.recetaForm.get(`cilindro${ojo}`)?.value),
      eje: Number(this.recetaForm.get(`eje${ojo}`)?.value),
      adicion: Number(this.recetaForm.get(`adicion${ojo}`)?.value ?? 0)
    })) : undefined;

    const datos = {
      rut: this.rut().trim(),
      fecha: formValue.fecha ?? '',
      observaciones: formValue.observaciones ?? undefined,
      graduaciones,
      imagen: this.selectedImage() ?? undefined
    };
    const solicitud = this.recetaId === null
      ? this.recetaService.crearReceta(datos)
      : this.recetaService.actualizarReceta(this.recetaId, datos);
    solicitud.subscribe({
      next: (receta) => {
        this.successMessage.set(this.recetaId === null ? 'Receta registrada correctamente.' : 'Receta actualizada correctamente.');
        this.isSubmitting.set(false);
        this.selectedImage.set(null);
        if (this.imagenInput) this.imagenInput.nativeElement.value = "";
        if (this.recetaId !== null) {
          this.imagenActual.set(receta.imagenUrl ?? null);
          this.rutOriginal.set(receta.rut);
          this.recetaForm.markAsPristine();
          return;
        }
        this.rut.set('');
        this.rutTouched.set(false);
        this.recetaForm.reset();
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudo guardar la receta. Inténtalo nuevamente.');
        this.isSubmitting.set(false);
      }
    });
  }

  protected hasError(controlName: string, error: string): boolean {
    const control = this.recetaForm.get(controlName);
    return Boolean(control?.touched && control.hasError(error));
  }
}