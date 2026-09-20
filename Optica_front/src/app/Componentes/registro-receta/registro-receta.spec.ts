import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegistroReceta } from './registro-receta';

describe('RegistroReceta', () => {
  let component: RegistroReceta;
  let fixture: ComponentFixture<RegistroReceta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegistroReceta],
    }).compileComponents();

    fixture = TestBed.createComponent(RegistroReceta);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
