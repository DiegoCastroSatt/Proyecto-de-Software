import { Component, ElementRef, HostListener, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AutenticacionClienteService, RegistroCliente } from './autenticacion-cliente.service';
import { validarRutChileno } from '../../shared/validators/rut-chileno.validator';

type CampoRegistro = keyof RegistroCliente;

@Component({
  selector: 'app-login-cliente',
  imports: [FormsModule],
  templateUrl: './login-cliente.html',
  styleUrl: './login-cliente.css',
})
export class LoginCliente implements OnDestroy {
  private readonly autenticacion = inject(AutenticacionClienteService);
  private readonly router = inject(Router);
  @ViewChild('registroDialog') private registroDialog?: ElementRef<HTMLElement>;
  @ViewChild('registroTrigger') private registroTrigger?: ElementRef<HTMLButtonElement>;
  @ViewChild('recuperarDialog') private recuperarDialog?: ElementRef<HTMLElement>;
  @ViewChild('recuperarTrigger') private recuperarTrigger?: ElementRef<HTMLButtonElement>;

  correo = '';
  contrasena = '';
  errorLogin = signal('');
  mensajeLogin = signal('');
  cargandoLogin = signal(false);
  sesionIniciada = signal(false);

  modalRegistroAbierto = signal(false);
  registroExitoso = signal(false);
  errorRegistro = signal('');
  cargandoRegistro = signal(false);
  camposTocados = signal(false);
  private overflowAnterior = '';
  registro: RegistroCliente = {
    nombre: '', apellido: '', rut: '', correo: '', telefono: '', contrasena: '', confirmarContrasena: ''
  };

  // --- Estado para recuperación de contraseña ---
  modalRecuperarAbierto = signal(false);
  recuperacionEnviada = signal(false);
  correoRecuperacion = '';
  errorRecuperar = signal('');
  cargandoRecuperar = signal(false);

  iniciarSesion(): void {
    this.errorLogin.set('');
    this.mensajeLogin.set('');
    this.sesionIniciada.set(false);
    if (!this.correo.trim() || !this.contrasena) {
      this.errorLogin.set('Ingresa tu correo electrónico y contraseña.');
      return;
    }
    if (!this.correoValido(this.correo)) {
      this.errorLogin.set('Ingresa un correo electrónico válido.');
      return;
    }

    this.cargandoLogin.set(true);
    this.autenticacion.iniciarSesion(this.correo.trim(), this.contrasena).subscribe({
      next: () => {
        this.cargandoLogin.set(false);
        this.sesionIniciada.set(true);
        this.contrasena = '';
        void this.router.navigateByUrl('/cliente/en-trabajo');
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoLogin.set(false);
        this.errorLogin.set(error.status === 401
          ? 'No fue posible iniciar sesión. Revisa tus datos. Si no tienes cuenta, regístrate; si tus datos ya están registrados en el centro, contacta al personal para verificar o activar tu acceso.'
          : 'No se pudo conectar con el servidor. Inténtalo nuevamente más tarde.');
      }
    });
  }

  abrirRegistro(): void {
    this.modalRegistroAbierto.set(true);
    this.registroExitoso.set(false);
    this.errorRegistro.set('');
    this.camposTocados.set(false);
    this.registro = { nombre: '', apellido: '', rut: '', correo: '', telefono: '', contrasena: '', confirmarContrasena: '' };
    this.overflowAnterior = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    setTimeout(() => this.registroDialog?.nativeElement.querySelector<HTMLInputElement>('#registro-nombre')?.focus());
  }

  cerrarRegistro(): void {
    if (this.cargandoRegistro()) return;
    this.modalRegistroAbierto.set(false);
    document.body.style.overflow = this.overflowAnterior;
    setTimeout(() => this.registroTrigger?.nativeElement.focus());
    if (this.registroExitoso()) {
      this.correo = this.registro.correo;
      this.mensajeLogin.set('Tu cuenta fue creada correctamente. Ya puedes iniciar sesión.');
      this.errorLogin.set('');
    }
  }

  abrirRecuperar(): void {
    this.modalRecuperarAbierto.set(true);
    this.recuperacionEnviada.set(false);
    this.errorRecuperar.set('');
    this.correoRecuperacion = this.correo.trim();
    this.overflowAnterior = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    setTimeout(() => this.recuperarDialog?.nativeElement.querySelector<HTMLInputElement>('#recuperar-correo')?.focus());
  }

  cerrarRecuperar(): void {
    if (this.cargandoRecuperar()) return;
    this.modalRecuperarAbierto.set(false);
    document.body.style.overflow = this.overflowAnterior;
    setTimeout(() => this.recuperarTrigger?.nativeElement.focus());
  }

  enviarRecuperacion(): void {
    if (this.cargandoRecuperar()) return;
    this.errorRecuperar.set('');

    const correo = this.correoRecuperacion.trim();
    if (!correo) {
      this.errorRecuperar.set('Ingresa tu correo electrónico.');
      return;
    }
    if (!this.correoValido(correo)) {
      this.errorRecuperar.set('Ingresa un correo electrónico válido.');
      return;
    }

    this.cargandoRecuperar.set(true);
    this.autenticacion.solicitarRecuperacion(correo).subscribe({
      next: () => {
        this.cargandoRecuperar.set(false);
        this.recuperacionEnviada.set(true);
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoRecuperar.set(false);
        if (error.status === 429) {
          this.errorRecuperar.set('Espera unos momentos antes de volver a solicitar un enlace.');
        } else {
          this.errorRecuperar.set('No se pudo enviar la solicitud. Inténtalo nuevamente más tarde.');
        }
      }
    });
  }

  @HostListener('document:keydown', ['$event'])
  controlarTeclado(evento: KeyboardEvent): void {
    if (!this.modalRegistroAbierto() && !this.modalRecuperarAbierto()) return;
    if (evento.key === 'Escape') {
      evento.preventDefault();
      if (this.modalRegistroAbierto()) this.cerrarRegistro();
      else this.cerrarRecuperar();
      return;
    }
    if (evento.key !== 'Tab') return;
    const dialog = this.modalRegistroAbierto() ? this.registroDialog : this.recuperarDialog;
    const elementos = Array.from(dialog?.nativeElement.querySelectorAll<HTMLElement>(
      'button:not(:disabled), input:not(:disabled)'
    ) ?? []);
    if (!elementos.length) return;
    const primero = elementos[0];
    const ultimo = elementos[elementos.length - 1];
    if (evento.shiftKey && document.activeElement === primero) {
      evento.preventDefault();
      ultimo.focus();
    } else if (!evento.shiftKey && document.activeElement === ultimo) {
      evento.preventDefault();
      primero.focus();
    }
  }

  enviarRegistro(): void {
    if (this.cargandoRegistro() || this.registroExitoso()) return;
    this.camposTocados.set(true);
    this.errorRegistro.set('');
    if (!this.registroValido()) return;

    this.cargandoRegistro.set(true);
    this.autenticacion.registrar(this.registro).subscribe({
      next: () => {
        this.cargandoRegistro.set(false);
        this.registroExitoso.set(true);
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoRegistro.set(false);
        this.errorRegistro.set(typeof error.error?.mensaje === 'string'
          ? error.error.mensaje
          : error.status === 409
            ? 'Ya existe un registro para esos datos. Contacta al personal para verificar o activar tu acceso.'
            : 'No se pudo crear la cuenta. Revisa los datos e inténtalo nuevamente.');
      }
    });
  }

  campoError(campo: CampoRegistro): string {
    if (!this.camposTocados()) return '';
    const valor = this.registro[campo];
    if (!valor.trim()) return 'Este campo es obligatorio.';
    switch (campo) {
      case 'nombre':
      case 'apellido':
        return valor.trim().length > 60 ? 'Usa hasta 60 caracteres.' : '';
      case 'rut':
        return this.rutValido(valor) ? '' : 'Ingresa un RUT chileno válido.';
      case 'correo':
        return this.correoValido(valor) && valor.trim().length <= 100 ? '' : 'Ingresa un correo válido (máximo 100 caracteres).';
      case 'telefono':
        return /^(\+?56\s?)?9\s?\d{4}\s?\d{4}$/.test(valor.trim()) ? '' : 'Ingresa un celular válido de 9 dígitos; puedes incluir +56.';
      case 'contrasena':
        return this.contrasenaValida(valor) ? '' : 'Usa al menos 8 caracteres e incluye letras, números y un símbolo.';
      case 'confirmarContrasena':
        return valor === this.registro.contrasena ? '' : 'Las contraseñas no coinciden.';
      default:
        return '';
    }
  }

  private registroValido(): boolean {
    const campos: CampoRegistro[] = ['nombre', 'apellido', 'rut', 'correo', 'telefono', 'contrasena', 'confirmarContrasena'];
    return campos.every(campo => !this.campoError(campo));
  }

  private correoValido(valor: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(valor.trim());
  }

  private contrasenaValida(valor: string): boolean {
    return valor.length >= 8 && valor.length <= 255
      && /\p{L}/u.test(valor) && /\p{N}/u.test(valor) && /[^\p{L}\p{N}\s]/u.test(valor);
  }

  private rutValido(valor: string): boolean {
    return validarRutChileno(new FormControl(valor)) === null;
  }

  ngOnDestroy(): void {
    if (this.modalRegistroAbierto() || this.modalRecuperarAbierto()) document.body.style.overflow = this.overflowAnterior;
  }
}