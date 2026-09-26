import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { LoginAdmin } from './login-admin';

describe('LoginAdmin', () => {
  let component: LoginAdmin;
  let fixture: ComponentFixture<LoginAdmin>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginAdmin],
      providers: [provideHttpClient(), provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginAdmin);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
