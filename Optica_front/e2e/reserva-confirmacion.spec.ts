import { expect, test, type Page } from '@playwright/test';
import { randomInt } from 'node:crypto';

const contrasenaAdministrador = 'E2E-Admin-Acceso-481!';
const urlApi = 'http://localhost:8080';
const urlMailpit = 'http://localhost:8025';

interface MensajeMailpit {
  ID: string;
  To: Array<{ Address: string }>;
  Subject: string;
  Text: string;
  HTML: string;
}

interface BusquedaMailpit {
  messages: Array<{ ID: string }>;
}

test('cliente reserva sin sesión, confirma desde el correo y aparece en la agenda', async ({ page, request }) => {
  test.setTimeout(150_000);
  const idEjecucion = Date.now();
  const marca = `Cliente E2E ${idEjecucion}`;
  const correoCliente = `cliente-e2e-${idEjecucion}@optica.invalid`;
  const rut = crearRutE2E(randomInt(10_000_000, 90_000_000));
  let fechaSeleccionada = '';
  let horaSeleccionada = '';

  await test.step('reserva pública sin iniciar sesión', async () => {
    await page.goto('/reservas');
    await expect(page.getByRole('heading', { name: 'Agenda tu atención visual' })).toBeVisible();
    expect(await page.context().cookies(`${urlApi}/api`)).toHaveLength(0);

    const disponibles = await request.get(`${urlApi}/api/Reservas/disponibles`);
    expect(disponibles.ok(), 'GET /api/Reservas/disponibles debe permitir acceso público').toBe(true);
    const horarios: Array<{ idHorario: number; fecha: string; hora: string }> = await disponibles.json();
    expect(horarios.length, 'No hay horarios disponibles. Recrea el Compose E2E para regenerar el horario sintético futuro (consulta E2E.md).').toBeGreaterThan(0);
    const horario = horarios[0];
    expect(horario).toBeDefined();
    if (!horario) throw new Error('No hay horarios disponibles en el entorno E2E.');
    fechaSeleccionada = horario.fecha.slice(0, 10);
    horaSeleccionada = horario.hora.slice(0, 5);

    await page.getByLabel('Nombre completo').fill(marca);
    await page.getByLabel('RUT').fill(rut);
    await page.getByLabel('Número de teléfono').fill('+56 9 1234 5678');
    await page.getByLabel('Correo electrónico').fill(correoCliente);
    await page.getByLabel('Día de atención').selectOption(fechaSeleccionada);
    await page.getByLabel('Hora preferida').selectOption(String(horario.idHorario));

    const crear = page.waitForResponse(response =>
      response.url() === `${urlApi}/api/Reservas` && response.request().method() === 'POST',
    );
    await page.getByRole('button', { name: 'Solicitar reserva' }).click();
    const respuestaCrear = await crear;
    expect(respuestaCrear.status(), 'POST /api/Reservas debe registrar la reserva pública').toBe(200);
    const reservaCreada: { id: number; idHorario: number; fecha: string; hora: string; estado: string } = await respuestaCrear.json();
    expect(reservaCreada.id).toBeGreaterThan(0);
    expect(reservaCreada.idHorario).toBe(horario.idHorario);
    expect(reservaCreada.fecha.slice(0, 10)).toBe(fechaSeleccionada);
    expect(reservaCreada.hora.slice(0, 5)).toBe(horaSeleccionada);
    expect(reservaCreada.estado).toBe('Pendiente');
    await expect(page.getByRole('status')).toContainText(`Reserva registrada, ${marca}.`);
    expect(await page.context().cookies(`${urlApi}/api`)).toHaveLength(0);
  });

  let contenidoCorreo = '';
  await test.step('captura y validación del correo en Mailpit', async () => {
    let mensaje: MensajeMailpit | undefined;
    await expect.poll(async () => {
      const busquedaRespuesta = await request.get(`${urlMailpit}/api/v1/search?query=to:${encodeURIComponent(correoCliente)}`);
      if (!busquedaRespuesta.ok()) return 'Mailpit API no disponible';
      const busqueda: BusquedaMailpit = await busquedaRespuesta.json();
      const id = busqueda.messages[0]?.ID;
      if (!id) return 'Correo todavía en cola';
      const detalle = await request.get(`${urlMailpit}/api/v1/message/${id}`);
      if (!detalle.ok()) return 'Mensaje aún no disponible';
      mensaje = await detalle.json();
      return mensaje.To.some(destinatario => destinatario.Address.toLowerCase() === correoCliente)
        ? 'correo recibido'
        : 'destinatario incorrecto';
    }, {
      message: 'La cola de correo debe entregar a Mailpit dentro del máximo de 75 segundos',
      timeout: 75_000,
      intervals: [1_000, 2_000, 3_000, 5_000],
    }).toBe('correo recibido');

    expect(mensaje).toBeDefined();
    expect(mensaje?.Subject).toContain('Reserva registrada');
    contenidoCorreo = `${mensaje?.Text ?? ''}\n${mensaje?.HTML ?? ''}`;
    expect(contenidoCorreo.includes(marca), 'El mensaje debe identificar al cliente de la reserva').toBe(true);
    const fechaRegex = new RegExp(fechaSeleccionada.split('-').reverse().join('[-/]'));
    expect(fechaRegex.test(contenidoCorreo), 'El mensaje debe incluir la fecha reservada').toBe(true);
    expect(contenidoCorreo.includes(horaSeleccionada), 'El mensaje debe incluir la hora reservada').toBe(true);
  });

  await test.step('confirmación mediante el enlace real del correo', async () => {
    const enlaces = [...contenidoCorreo.matchAll(/https?:\/\/localhost:8080\/api\/Reservas\/acciones\/confirmar\/[^\s"'<>]+/g)];
    expect(enlaces.length, 'El correo capturado debe incluir el enlace real de confirmación').toBeGreaterThan(0);
    const enlace = enlaces[0]?.[0];
    expect(enlace).toBeDefined();
    if (!enlace) throw new Error('No se encontró el enlace de confirmación en el mensaje.');

    const respuestaIntermedia = await abrirEnlaceConfirmacion(page, enlace);
    expect(respuestaIntermedia?.status()).toBe(200);
    await expect(page).toHaveTitle('Confirmar hora');
    await expect(page.getByRole('heading', { name: 'Confirmar hora' })).toBeVisible();
    const confirmar = page.waitForResponse(response =>
      response.url().startsWith(`${urlApi}/api/Reservas/acciones/confirmar/`)
      && response.request().method() === 'POST',
    );
    await page.getByRole('button', { name: 'CONFIRMAR MI HORA' }).click();
    expect((await confirmar).status()).toBe(200);
    await expect(page.getByText('¡Hora confirmada! Tu hora quedó confirmada correctamente.')).toBeVisible();

    await abrirEnlaceConfirmacion(page, enlace);
    await page.getByRole('button', { name: 'CONFIRMAR MI HORA' }).click();
    await expect(page.getByText('Esta hora ya se encuentra confirmada.')).toBeVisible();
  });

  await test.step('verificación desde la agenda administrativa', async () => {
    await page.goto('/admin');
    await page.getByLabel('RUT, correo electrónico o nombre de usuario').fill('e2e-admin');
    await page.getByRole('textbox', { name: 'Contraseña' }).fill(contrasenaAdministrador);
    const login = page.waitForResponse(response =>
      response.url() === `${urlApi}/api/Autenticacion/login` && response.request().method() === 'POST',
    );
    await page.getByRole('button', { name: 'Ingresar' }).click();
    expect((await login).status(), 'El administrador sintético debe iniciar sesión').toBe(200);
    await expect(page).toHaveURL(/\/admin\/panel/);
    await page.goto('/admin/panel/agenda');
    await expect(page.getByRole('heading', { name: 'Agenda de atención' })).toBeVisible();
    const tarjeta = page.getByRole('button', { name: `Ver reserva de ${marca}` });
    await expect(tarjeta).toBeVisible();
    await expect(tarjeta).toContainText(fechaSeleccionada.split('-').reverse().join('/'));
    await expect(tarjeta).toContainText('Confirmada');
    await tarjeta.click();
    await expect(page.getByRole('dialog')).toContainText(correoCliente);
    await expect(page.getByRole('dialog')).toContainText(horaSeleccionada);
    await expect(page.getByRole('dialog')).toContainText('Confirmada');

    page.on('dialog', dialog => dialog.accept());
    await page.getByRole('dialog').getByRole('button', { name: 'Cancelar hora' }).click();
    await expect(page.getByRole('dialog')).toBeHidden();
    await expect(tarjeta).toHaveCount(0);
  });
});

function crearRutE2E(valor: number): string {
  const cuerpo = String((valor % 80_000_000) + 10_000_000);
  let suma = 0;
  let multiplicador = 2;
  for (const digito of [...cuerpo].reverse()) {
    suma += Number(digito) * multiplicador;
    multiplicador = multiplicador === 7 ? 2 : multiplicador + 1;
  }
  const resto = 11 - (suma % 11);
  const verificador = resto === 11 ? '0' : resto === 10 ? 'K' : String(resto);
  return `${cuerpo}-${verificador}`;
}

async function abrirEnlaceConfirmacion(page: Page, enlace: string) {
  try {
    return await page.goto(enlace);
  } catch {
    throw new Error('No se pudo abrir la página de confirmación recibida por correo.');
  }
}
