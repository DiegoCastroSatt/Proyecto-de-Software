import { TiposRostroService } from '../../shared/services/tipos-rostro.service';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { RecomendadorMarcosComponent } from './recomendador-marcos';
import { DisponibilidadMarcosService } from '../../shared/services/disponibilidad-marcos.service';

describe('Recomendador con tipos administrados', () => {
  it('incluye tipos nuevos por rostro y usa su imagen y código del servidor', () => {
    TestBed.configureTestingModule({ imports: [RecomendadorMarcosComponent], providers: [provideRouter([]),
      { provide: TiposRostroService, useValue: { activos: () => of(['triangular', 'diamante'].map((codigo, i) => ({ id: i + 1, codigo, nombre: codigo, rasgos: 'Rasgos', consejo: 'Consejo', activo: true, rutaImagen: '/assets/images/recomendador/rostro-ovalado.png', marcos: [] }))) } },
      { provide: DisponibilidadMarcosService, useValue: { consultar: () => of([
        { id: 20, codigo: 'nuevo', nombre: 'Marco nuevo', rostros: ['triangular'], stock: 4 },
        { id: 21, codigo: 'otro', nombre: 'Otro', rostros: ['redondo'], stock: 1 },
      ]) } },
    ] });
    const fixture = TestBed.createComponent(RecomendadorMarcosComponent);
    fixture.componentInstance.seleccionar('triangular'); fixture.detectChanges();
    expect(fixture.componentInstance.recomendacion?.marcos.map(m => m.codigo)).toEqual(['nuevo']);
    expect(fixture.nativeElement.querySelector('.sugerencias img').src).toContain('/api/Marcos/20/imagen');
    expect(fixture.nativeElement.querySelector('.sugerencias a').href).toContain('modelo=nuevo');
    fixture.componentInstance.seleccionar('diamante'); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.sugerencias article').length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('No hay marcos recomendados');
  });
});
