export interface CotizacionPedidoDatos {
  rutCliente: string;
  nombreCliente?: string;
  fecha: string;
  fechaVencimiento: string;
  total: number;
  productos?: string;
  servicios?: string;
  anotaciones?: string;
}

export const avisoCotizacion = 'Documento referencial de cotización. No representa una boleta, factura ni compromiso vinculante de compra.';

export function formatearFechaCotizacion(valor: string): string {
  const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(valor);
  return partes ? `${partes[3]}/${partes[2]}/${partes[1]}` : (valor || 'Sin fecha');
}

export async function crearCotizacionPedidoPdf(datos: CotizacionPedidoDatos) {
  const { jsPDF } = await import('jspdf');
  const pdf = new jsPDF({ unit: 'pt', format: 'a4' });
  pdf.setProperties({
    title: `Cotización de pedido - ${datos.rutCliente}`,
    author: 'Centro Óptico San Francisco'
  });

  const ancho = pdf.internal.pageSize.getWidth();
  const alto = pdf.internal.pageSize.getHeight();
  const margen = 40;
  const limiteInferior = alto - 90;

  // El peso chileno no tiene decimales; el separador de miles se arma a mano
  // para no depender de las reglas de agrupación del locale.
  const moneda = (valor: number) =>
    '$' + String(Math.round(valor)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');

  const fecha = formatearFechaCotizacion(datos.fecha);
  const vencimiento = formatearFechaCotizacion(datos.fechaVencimiento);

  const texto = (
    valor: string,
    x: number,
    y: number,
    tamano = 10,
    negrita = false,
    derecha = false,
    color = [35, 49, 52]
  ) => {
    pdf.setFont('helvetica', negrita ? 'bold' : 'normal');
    pdf.setFontSize(tamano);
    pdf.setTextColor(color[0], color[1], color[2]);
    pdf.text(valor, x, y, { align: derecha ? 'right' : 'left' });
  };

  const encabezado = () => {
    // Franja superior verde institucional
    pdf.setFillColor(20, 76, 72);
    pdf.rect(0, 0, ancho, 7, 'F');

    // Acento dorado sutil
    pdf.setFillColor(191, 133, 66);
    pdf.rect(0, 7, ancho, 2, 'F');

    texto('CENTRO ÓPTICO SAN FRANCISCO', margen, 44, 13, true, false, [20, 76, 72]);
    texto('COTIZACIÓN DE PEDIDO', margen, 70, 19, true, false, [35, 49, 52]);
    texto(`Válida hasta: ${vencimiento}`, ancho - margen, 70, 12, true, true, [191, 133, 66]);
    texto(avisoCotizacion, margen, 92, 9, false, false, [108, 123, 120]);
    texto(`Fecha de emisión: ${fecha}`, margen, 116, 10);
    texto('Moneda: pesos chilenos (CLP)', ancho - margen, 116, 9, false, true, [108, 123, 120]);
  };

  encabezado();

  let y = 142;

  const nuevaPagina = () => {
    pdf.addPage();
    encabezado();
    y = 142;
  };

  const asegurarEspacio = (necesario: number) => {
    if (y + necesario > limiteInferior) {
      nuevaPagina();
    }
  };

  // Escribe un título y un texto largo línea por línea, saltando de página
  // cuando hace falta (el título nunca queda solo al final de una página).
  const escribirBloque = (titulo: string, contenido: string, tamano: number, colorTitulo: number[]) => {
    asegurarEspacio(14 + 15);
    texto(titulo, margen + 12, y, 9, true, false, colorTitulo);
    y += 14;

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(tamano);
    const lineas: string[] = pdf.splitTextToSize(contenido, ancho - margen * 2 - 24);

    for (const linea of lineas) {
      asegurarEspacio(15);
      texto(linea, margen + 12, y, tamano, false, false, [35, 49, 52]);
      y += 15;
    }
    y += 10;
  };

  // Cuadro con información del cliente
  pdf.setFillColor(248, 250, 249);
  pdf.setDrawColor(218, 228, 225);
  pdf.roundedRect(margen, y, ancho - margen * 2, 48, 4, 4, 'FD');

  const nombreMostrar = datos.nombreCliente ? datos.nombreCliente.trim() : 'Cliente sin registrar';
  texto('DATOS DEL CLIENTE', margen + 14, y + 18, 8, true, false, [20, 76, 72]);
  texto(`Nombre: ${nombreMostrar}`, margen + 14, y + 34, 10, true);
  texto(`RUT: ${datos.rutCliente}`, ancho - margen - 14, y + 34, 10, true, true);

  y += 68;

  // Cabecera de la tabla de detalle
  asegurarEspacio(26 + 14 + 15);
  pdf.setFillColor(232, 241, 239);
  pdf.rect(margen, y, ancho - margen * 2, 26, 'F');
  texto('PRODUCTOS / DETALLE COTIZADO', margen + 12, y + 17, 9, true, false, [20, 76, 72]);
  y += 26 + 16;

  const productos = datos.productos?.trim() ?? '';
  const servicios = datos.servicios?.trim() ?? '';

  if (productos) {
    escribirBloque('Productos', productos, 10, [35, 49, 52]);
  }
  if (servicios) {
    escribirBloque('Servicios', servicios, 10, [35, 49, 52]);
  }
  if (!productos && !servicios) {
    escribirBloque('Detalle', 'Sin detalle especificado.', 10, [35, 49, 52]);
  }

  pdf.setDrawColor(218, 228, 225);
  pdf.line(margen, y - 4, ancho - margen, y - 4);
  y += 12;

  const anotaciones = datos.anotaciones?.trim() ?? '';
  if (anotaciones) {
    escribirBloque('ESPECIFICACIONES / ANOTACIONES', anotaciones, 9.5, [20, 76, 72]);
    y += 6;
  }

  // Resumen del total
  asegurarEspacio(110);

  pdf.setFillColor(240, 246, 244);
  pdf.roundedRect(margen, y, ancho - margen * 2, 54, 4, 4, 'F');

  texto('TOTAL DE LA COTIZACIÓN', margen + 16, y + 33, 11, true, false, [20, 76, 72]);
  texto(moneda(datos.total), ancho - margen - 16, y + 35, 18, true, true, [20, 76, 72]);

  y += 70;
  texto(`* Esta cotización es válida hasta el ${vencimiento}.`, margen, y, 8.5, false, false, [108, 123, 120]);
  texto('* Los valores están sujetos a la confirmación de receta médica y disponibilidad de armazones o cristales.', margen, y + 13, 8.5, false, false, [108, 123, 120]);

  // Pie de página en todas las páginas
  const paginas = pdf.getNumberOfPages();
  for (let pagina = 1; pagina <= paginas; pagina++) {
    pdf.setPage(pagina);
    pdf.setDrawColor(218, 228, 225);
    pdf.line(margen, alto - 55, ancho - margen, alto - 55);

    texto(avisoCotizacion, margen, alto - 40, 8, false, false, [108, 123, 120]);
    texto('Centro Óptico San Francisco | Presupuesto y Cotización', margen, alto - 26, 8, true, false, [20, 76, 72]);
    texto(`${pagina} / ${paginas}`, ancho - margen, alto - 26, 8.5, false, true, [108, 123, 120]);
  }

  return pdf;
}