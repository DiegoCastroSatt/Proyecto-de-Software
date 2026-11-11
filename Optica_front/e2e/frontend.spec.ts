import { expect, test } from '@playwright/test';

test('la portada carga y Angular muestra contenido visible', async ({ page }) => {
  const respuesta = await page.goto('/');

  expect(respuesta, 'El frontend debe devolver una respuesta HTTP').not.toBeNull();
  expect(respuesta?.ok(), 'El frontend debe responder con un estado exitoso').toBe(true);
  await expect(page).toHaveTitle('OpticaWeb');
  await expect(
    page.getByRole('heading', { name: 'Una nueva forma de ver tu mundo.' }),
  ).toBeVisible();
});
