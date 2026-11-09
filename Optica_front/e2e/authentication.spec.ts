import { expect, test } from '@playwright/test';

const correoClienteE2E = 'cliente-e2e@optica.invalid';
const contrasenaClienteE2E = 'E2E-Cliente-Acceso-271!';
const urlSesionCliente = 'http://localhost:8080/api/Autenticacion/cliente/sesion';

test('el cliente inicia sesión y conserva su cookie al recargar', async ({ page }) => {
  await page.goto('/cliente/login');
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();

  const respuestaLogin = page.waitForResponse((respuesta) =>
    respuesta.url() === 'http://localhost:8080/api/Autenticacion/login'
      && respuesta.request().method() === 'POST',
  );

  await page.getByLabel('RUT, correo electrónico o nombre de usuario').fill(correoClienteE2E);
  await page.getByRole('textbox', { name: 'Contraseña' }).fill(contrasenaClienteE2E);
  await page.getByRole('button', { name: 'Ingresar' }).click();

  expect((await respuestaLogin).status()).toBe(200);
  await expect(page).toHaveURL('http://localhost:4200/cliente/inicio');
  await expect(page.getByRole('heading', { name: 'Portal del cliente' })).toBeVisible();

  const respuestaSesion = page.waitForResponse((respuesta) =>
    respuesta.url() === urlSesionCliente && respuesta.request().method() === 'GET',
  );
  await page.reload();

  expect((await respuestaSesion).status()).toBe(200);
  await expect(page).toHaveURL('http://localhost:4200/cliente/inicio');
  await expect(page.getByRole('heading', { name: 'Portal del cliente' })).toBeVisible();
});
