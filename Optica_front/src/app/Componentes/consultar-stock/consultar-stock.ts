import { Component, OnInit, inject, signal } from '@angular/core';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';

@Component({
  selector: 'app-consultar-stock',
  imports: [],
  templateUrl: './consultar-stock.html',
  styleUrl: './consultar-stock.css',
})
export class ConsultarStock implements OnInit {
  private readonly productoService = inject(BuscarProductoService);

  protected readonly productos = signal<Producto[]>([]);
  protected readonly busqueda = signal('');
  protected readonly cargando = signal(false);
  protected readonly error = signal('');
  protected readonly errorPdf = signal('');
  protected readonly generandoPdf = signal(false);
  protected readonly errorStock = signal('');
  protected readonly productoEditando = signal<number | null>(null);
  protected readonly borradorStock = signal({ stock: 0, stockMinimo: 0 });
  protected readonly motivoCambio = signal('');
  protected readonly guardandoStock = signal(false);

  ngOnInit(): void {
    this.cargarProductos();
  }

  protected cargarProductos(): void {
    this.cargando.set(true);
    this.error.set('');
    this.errorPdf.set('');
    this.productoService.buscar('').subscribe({
      next: productos => {
        this.productos.set(productos);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo consultar el stock. Intenta nuevamente.');
        this.cargando.set(false);
      }
    });
  }

  protected mostrarInforme(): void {
    void this.abrirPdf(false);
  }

  protected descargarInforme(): void {
    void this.abrirPdf(true);
  }

  private async abrirPdf(descargar: boolean): Promise<void> {
    if (this.generandoPdf()) return;

    this.generandoPdf.set(true);
    this.errorPdf.set('');
    const ventana = descargar ? null : window.open('', '_blank');
    if (!descargar && !ventana) {
      this.errorPdf.set('No se pudo abrir el PDF. Permite las ventanas emergentes e inténtalo nuevamente.');
      this.generandoPdf.set(false);
      return;
    }
    if (ventana) ventana.opener = null;

    try {
      const url = URL.createObjectURL(await this.crearInformePdf());
      if (descargar) {
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = `reporte-inventario-${this.claveFecha(new Date())}.pdf`;
        enlace.click();
        setTimeout(() => URL.revokeObjectURL(url), 60000);
        return;
      }

      if (!ventana) {
        URL.revokeObjectURL(url);
        this.errorPdf.set('No se pudo abrir el PDF. Permite las ventanas emergentes e inténtalo nuevamente.');
        return;
      }
      ventana.location.href = url;
      setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch {
      ventana?.close();
      this.errorPdf.set('No se pudo generar el reporte PDF. Intenta nuevamente.');
    } finally {
      this.generandoPdf.set(false);
    }
  }

  private async crearInformePdf(): Promise<Blob> {
    const { jsPDF } = await import('jspdf');
    const documento = new jsPDF({ unit: 'pt', format: 'a4' });
    const ancho = documento.internal.pageSize.getWidth();
    const alto = documento.internal.pageSize.getHeight();
    const margen = 38;
    const contenido = ancho - margen * 2;
    const verde = '#1d4a47';
    const gris = '#657873';
    const productos = this.productos();
    const categorias = [
      {
        titulo: 'Productos disponibles',
        productos: productos.filter(producto => producto.stock > producto.stockMinimo),
        color: '#e7f0eb'
      },
      {
        titulo: 'Productos con stock bajo',
        productos: productos.filter(producto => producto.stock > 0 && producto.stock <= producto.stockMinimo),
        color: '#f7eedc'
      },
      {
        titulo: 'Productos agotados',
        productos: productos.filter(producto => producto.stock <= 0),
        color: '#f5e6e3'
      }
    ];
    const fechaActual = new Date();
    const fecha = new Intl.DateTimeFormat('es-CL', { dateStyle: 'long', timeStyle: 'short' }).format(fechaActual);
    const texto = (
      valor: string,
      x: number,
      y: number,
      tamano = 9,
      color = verde,
      negrita = false,
      alineacion: 'left' | 'right' = 'left'
    ) => {
      documento.setFont('helvetica', negrita ? 'bold' : 'normal');
      documento.setFontSize(tamano);
      documento.setTextColor(color);
      documento.text(valor, x, y, { align: alineacion });
    };
    const fondo = (x: number, y: number, anchoFondo: number, altoFondo: number, color: string) => {
      documento.setFillColor(color);
      documento.rect(x, y, anchoFondo, altoFondo, 'F');
    };
    const truncar = (valor: string, anchoMaximo: number): string => {
      let resultado = valor;
      while (resultado && documento.getTextWidth(resultado) > anchoMaximo) {
        resultado = `${resultado.slice(0, -2)}…`;
      }
      return resultado;
    };
    const dibujarEncabezado = (primeraPagina: boolean): number => {
      fondo(0, 0, ancho, primeraPagina ? 132 : 72, verde);
      texto('CENTRO ÓPTICO SAN FRANCISCO', margen, 30, 9, '#e5c58d', true);
      texto('Reporte de inventario', margen, primeraPagina ? 68 : 54, primeraPagina ? 24 : 16, '#ffffff', true);
      if (primeraPagina) {
        texto(`Creado el ${fecha}`, margen, 94, 9, '#ffffff');
        texto('Estado actual de los productos registrados.', margen, 113, 9, '#ffffff');
        return 154;
      }
      texto(`Generado el ${fecha}`, ancho - margen, 54, 8, '#ffffff', false, 'right');
      return 94;
    };

    documento.setProperties({
      title: 'Reporte de inventario - Óptica San Francisco',
      author: 'Centro Óptico San Francisco'
    });

    let y = dibujarEncabezado(true);
    const tarjetas = [
      ['PRODUCTOS', String(productos.length)],
      ['DISPONIBLES', String(categorias[0].productos.length)],
      ['STOCK BAJO', String(categorias[1].productos.length)],
      ['AGOTADOS', String(categorias[2].productos.length)]
    ];
    const anchoTarjeta = (contenido - 24) / tarjetas.length;
    tarjetas.forEach(([titulo, valor], indice) => {
      const x = margen + indice * (anchoTarjeta + 8);
      fondo(x, y, anchoTarjeta, 54, '#eef3f0');
      texto(titulo, x + 9, y + 18, 7, gris, true);
      texto(valor, x + 9, y + 41, 17, verde, true);
    });
    y += 76;

    const nuevaPagina = (): void => {
      documento.addPage();
      y = dibujarEncabezado(false);
    };
    const dibujarEncabezadoTabla = (titulo: string, color: string): void => {
      fondo(margen, y, contenido, 26, color);
      texto(titulo, margen + 10, y + 17, 10, verde, true);
      y += 35;
      fondo(margen, y - 5, contenido, 22, verde);
      texto('PRODUCTO', margen + 8, y + 10, 7, '#ffffff', true);
      texto('CÓDIGO', margen + 190, y + 10, 7, '#ffffff', true);
      texto('CATEGORÍA', margen + 278, y + 10, 7, '#ffffff', true);
      texto('STOCK', ancho - margen - 55, y + 10, 7, '#ffffff', true, 'right');
      texto('MÍNIMO', ancho - margen - 7, y + 10, 7, '#ffffff', true, 'right');
      y += 22;
    };

    for (const categoria of categorias) {
      if (y + 60 > alto - 58) nuevaPagina();
      dibujarEncabezadoTabla(`${categoria.titulo} (${categoria.productos.length})`, categoria.color);
      if (!categoria.productos.length) {
        texto('No hay productos en esta categoría.', margen + 8, y + 15, 9, gris);
        y += 28;
        continue;
      }

      categoria.productos.forEach((producto, indice) => {
        if (y + 23 > alto - 58) {
          nuevaPagina();
          dibujarEncabezadoTabla(`${categoria.titulo} (${categoria.productos.length})`, categoria.color);
        }
        if (indice % 2 === 0) fondo(margen, y, contenido, 22, '#f5f7f5');
        documento.setFont('helvetica', 'normal');
        documento.setFontSize(8);
        texto(truncar(producto.nombre, 172), margen + 8, y + 14, 8);
        texto(truncar(producto.codigo, 76), margen + 190, y + 14, 8, gris);
        texto(truncar(producto.categoria, 112), margen + 278, y + 14, 8, gris);
        texto(String(producto.stock), ancho - margen - 55, y + 14, 8, verde, false, 'right');
        texto(String(producto.stockMinimo), ancho - margen - 7, y + 14, 8, verde, false, 'right');
        y += 22;
      });
      y += 15;
    }

    const totalPaginas = documento.getNumberOfPages();
    for (let pagina = 1; pagina <= totalPaginas; pagina++) {
      documento.setPage(pagina);
      documento.setDrawColor('#d5dfd9');
      documento.line(margen, alto - 36, ancho - margen, alto - 36);
      texto('Óptica San Francisco | Reporte del inventario actual', margen, alto - 20, 7, gris);
      texto(`Página ${pagina} de ${totalPaginas}`, ancho - margen, alto - 20, 7, gris, false, 'right');
    }

    return documento.output('blob');
  }

  private claveFecha(fecha: Date): string {
    return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}-${String(fecha.getDate()).padStart(2, '0')}`;
  }

  protected actualizarBusqueda(event: Event): void {
    this.busqueda.set((event.target as HTMLInputElement).value);
  }

  protected editarStock(producto: Producto): void {
    this.errorStock.set('');
    this.borradorStock.set({ stock: producto.stock, stockMinimo: producto.stockMinimo });
    this.motivoCambio.set('');
    this.productoEditando.set(producto.id);
  }

  protected actualizarMotivoCambio(event: Event): void {
    this.motivoCambio.set((event.target as HTMLInputElement).value);
  }

  protected actualizarBorradorStock(campo: 'stock' | 'stockMinimo', event: Event): void {
    const valor = (event.target as HTMLInputElement).valueAsNumber;
    this.borradorStock.update(borrador => ({ ...borrador, [campo]: valor }));
  }

  protected cancelarEdicionStock(): void {
    this.productoEditando.set(null);
    this.motivoCambio.set('');
    this.errorStock.set('');
  }

  protected guardarStock(producto: Producto): void {
    const { stock, stockMinimo } = this.borradorStock();
    if (!Number.isInteger(stock) || stock < 0 || !Number.isInteger(stockMinimo) || stockMinimo < 0) {
      this.errorStock.set('Ingresa cantidades enteras iguales o mayores a cero.');
      return;
    }
    const motivoCambio = this.motivoCambio().trim();
    if (!motivoCambio || motivoCambio.length > 255) {
      this.errorStock.set('Ingresa el motivo del cambio (máximo 255 caracteres).');
      return;
    }

    this.guardandoStock.set(true);
    this.errorStock.set('');
    this.productoService.actualizarStock(producto.id, stock, stockMinimo, motivoCambio).subscribe({
      next: actualizado => {
        this.productos.update(productos => productos.map(item => item.id === actualizado.id ? actualizado : item));
        this.productoEditando.set(null);
        this.motivoCambio.set('');
        this.guardandoStock.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.errorStock.set(respuesta.error?.mensaje ?? 'No se pudo actualizar el stock. Intenta nuevamente.');
        this.guardandoStock.set(false);
      }
    });
  }

  protected productosFiltrados(): Producto[] {
    const termino = this.busqueda().trim().toLocaleLowerCase();
    if (!termino) {
      return this.productos();
    }

    return this.productos().filter(producto =>
      `${producto.nombre} ${producto.codigo} ${producto.categoria}`.toLocaleLowerCase().includes(termino)
    );
  }
}
