import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authAdminGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  if (!isBrowser || sessionStorage.getItem('isAdmin') === 'true') {
    return true;
  }

  return router.createUrlTree(['/admin'], {
    queryParams: { returnUrl: state.url }
  });
};
