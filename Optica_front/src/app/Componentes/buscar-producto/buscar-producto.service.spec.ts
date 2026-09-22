import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BuscarProductoService, ProductoEditable } from './buscar-producto.service';

describe('BuscarProductoService', () => {
  let service: BuscarProductoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        BuscarProductoService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(BuscarProductoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('busca productos usando el término como query string', () => {
    service.buscar('lentes').subscribe();

    const request = http.expectOne(request =>
      request.url === 'http://localhost:8080/api/Productos' && request.params.get('termino') === 'lentes'
    );
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('actualiza un producto enviando sus datos como FormData', () => {
    const producto: ProductoEditable = {
      codigo: 'OPT-001', nombre: 'Armazon', marca: 'Vogue', modelo: 'V1', color: 'Negro',
      categoria: 'Armazones', precio: 25000, stock: 3, stockMinimo: 1, estado: 'Disponible',
      imagen: new File(['image'], 'armazon.png', { type: 'image/png' })
    };

    service.actualizar(7, producto).subscribe();

    const request = http.expectOne('http://localhost:8080/api/Productos/7');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect(request.request.body.get('codigo')).toBe('OPT-001');
    expect(request.request.body.get('precio')).toBe('25000');
    const imagenEnviada = request.request.body.get('imagen') as File;
    expect(imagenEnviada).toBeInstanceOf(File);
    expect(imagenEnviada.name).toBe('armazon.png');
    expect(imagenEnviada.type).toBe('image/png');
    request.flush({ ...producto, id: 7, imagen: undefined });
  });

  it('elimina un producto por su identificador', () => {
    service.eliminar(7).subscribe();

    const request = http.expectOne('http://localhost:8080/api/Productos/7');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
