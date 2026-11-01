import { Component, ElementRef, HostListener, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { InicioSesionService } from '../../shared/services/inicio-sesion.service';
import { AutenticacionClienteService, RegistroCliente } from './autenticacion-cliente.service';
import { validarRutChileno } from '../../shared/validators/rut-chileno.validator';

type CampoRegistro = keyof RegistroCliente;

@Component({
  selector: 'app-login-cliente',
  imports: [FormsModule, RouterLink],
  templateUrl: './login-cliente.html',
  styleUrl: './login-cliente.css',
})
export class LoginCliente implements OnDestroy {
  private readonly autenticacion = inject(AutenticacionClienteService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly inicioSesion = inject(InicioSesionService);
  @ViewChild('registroDialog') private registroDialog?: ElementRef<HTMLElement>;
  @ViewChild('registroTrigger') private registroTrigger?: ElementRef<HTMLButtonElement>;
  @ViewChild('recuperarDialog') private recuperarDialog?: ElementRef<HTMLElement>;
  @ViewChild('recuperarTrigger') private recuperarTrigger?: ElementRef<HTMLButtonElement>;

  correo = '';
  contrasena = '';
  errorLogin = signal('');
  mensajeLogin = signal('');
  cargandoLogin = signal(false);
  reenviandoConfirmacionLogin = signal(false);
  sesionIniciada = signal(false);

  modalRegistroAbierto = signal(false);
  registroExitoso = signal(false);
  registroMensaje = signal('');
  errorRegistro = signal('');
  cargandoRegistro = signal(false);
  reenviandoConfirmacion = signal(false);
  confirmacionReenviada = signal(false);
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
    if (this.cargandoLogin()) return;
    this.errorLogin.set('');
    this.mensajeLogin.set('');
    this.sesionIniciada.set(false);
    if (!this.correo.trim() || !this.contrasena) {
      this.errorLogin.set('Ingresa tu correo o usuario y contraseña.');
      return;
    }

    this.cargandoLogin.set(true);
    this.inicioSesion.iniciarSesion(this.correo.trim(), this.contrasena).subscribe({
      next: (resultado) => {
        this.cargandoLogin.set(false);
        this.sesionIniciada.set(true);
        this.contrasena = '';
        const retorno = this.route.snapshot.queryParamMap.get('returnUrl');
        const destino = resultado.tipoUsuario === 'Cliente'
          ? '/cliente/inicio'
          : retorno && (retorno === '/admin/panel' || retorno.startsWith('/admin/panel/'))
            ? retorno : '/admin/panel';
        void this.router.navigateByUrl(destino);
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoLogin.set(false);
        this.errorLogin.set(error.status === 401
          ? 'No fue posible iniciar sesión. Revisa tus datos. Si no tienes cuenta, regístrate; si acabas de crearla, confirma tu correo.'
          : 'No se pudo conectar con el servidor. Inténtalo nuevamente más tarde.');
      }
    });
  }

  reenviarConfirmacionDesdeLogin(): void {
    if (this.reenviandoConfirmacionLogin()) return;
    const correo = this.correo.trim();
    if (!this.correoValido(correo)) {
      this.errorLogin.set('Ingresa primero un correo válido para solicitar la confirmación.');
      return;
    }
    this.errorLogin.set('');
    this.mensajeLogin.set('');
    this.reenviandoConfirmacionLogin.set(true);
    this.autenticacion.reenviarConfirmacion(correo).subscribe({
      next: (respuesta) => {
        this.reenviandoConfirmacionLogin.set(false);
        this.mensajeLogin.set(respuesta.mensaje);
      },
      error: () => {
        this.reenviandoConfirmacionLogin.set(false);
        this.errorLogin.set('No se pudo solicitar el reenvío. Inténtalo nuevamente más tarde.');
      }
    });
  }

  abrirRegistro(): void {
    this.modalRegistroAbierto.set(true);
    this.registroExitoso.set(false);
    this.registroMensaje.set('');
    this.confirmacionReenviada.set(false);
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
      this.mensajeLogin.set('Confirma tu correo electrónico antes de iniciar sesión.');
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
      next: (respuesta) => {
        this.cargandoRegistro.set(false);
        this.registroMensaje.set(respuesta.mensaje);
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

  reenviarCorreoConfirmacion(): void {
    if (this.reenviandoConfirmacion() || !this.registro.correo.trim()) return;
    this.reenviandoConfirmacion.set(true);
    this.autenticacion.reenviarConfirmacion(this.registro.correo.trim()).subscribe({
      next: (respuesta) => {
        this.reenviandoConfirmacion.set(false);
        this.confirmacionReenviada.set(true);
        this.registroMensaje.set(respuesta.mensaje);
      },
      error: () => {
        this.reenviandoConfirmacion.set(false);
        this.registroMensaje.set('No se pudo solicitar el reenvío. Inténtalo nuevamente más tarde.');
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
