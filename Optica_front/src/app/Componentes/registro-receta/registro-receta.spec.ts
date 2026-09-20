import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegistroRecetaComponent } from './registro-receta';

describe('RegistroRecetaComponent', () => {
  let component: RegistroRecetaComponent;
  let fixture: ComponentFixture<RegistroRecetaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegistroRecetaComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegistroRecetaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});