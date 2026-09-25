import { AbstractControl, ValidationErrors } from '@angular/forms';

export function validarRutChileno(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  if (!valor) return null;

  const limpio = valor.replace(/\./g, '').replace(/-/g, '').trim().toUpperCase();

  if (limpio.length < 8 || limpio.length > 9) {
    return { rutInvalido: true };
  }

  const cuerpo = limpio.slice(0, -1);
  const dvIngresado = limpio.slice(-1);

  if (!/^\d+$/.test(cuerpo)) {
    return { rutInvalido: true };
  }

  let suma = 0;
  let multiplo = 2;
  for (let i = cuerpo.length - 1; i >= 0; i--) {
    suma += parseInt(cuerpo[i], 10) * multiplo;
    multiplo = multiplo === 7 ? 2 : multiplo + 1;
  }

  const resto = suma % 11;
  const dvEsperado = 11 - resto;

  let dvCalculado = '';
  if (dvEsperado === 11) dvCalculado = '0';
  else if (dvEsperado === 10) dvCalculado = 'K';
  else dvCalculado = dvEsperado.toString();

  return dvCalculado === dvIngresado ? null : { rutInvalido: true };
}