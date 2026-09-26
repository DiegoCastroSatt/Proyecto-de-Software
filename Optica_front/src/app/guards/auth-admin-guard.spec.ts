import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';

import { authAdminGuard } from './auth-admin-guard';

describe('authAdminGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => authAdminGuard(...guardParameters));
  const route = {} as Parameters<CanActivateFn>[0];
  const state = { url: '/admin/panel/ventas' } as RouterStateSnapshot;

  beforeEach(() => {
    sessionStorage.removeItem('isAdmin');
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: PLATFORM_ID, useValue: 'browser' }]
    });
  });

  it('allows an authenticated administrator', () => {
    sessionStorage.setItem('isAdmin', 'true');

    expect(executeGuard(route, state)).toBe(true);
  });

  it('redirects an unauthenticated visitor to /admin with the requested URL', () => {
    const result = executeGuard(route, state) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe(
      '/admin?returnUrl=%2Fadmin%2Fpanel%2Fventas'
    );
  });

  it('does not access sessionStorage while rendering on the server', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: PLATFORM_ID, useValue: 'server' }]
    });

    expect(executeGuard(route, state)).toBe(true);
  });
});
