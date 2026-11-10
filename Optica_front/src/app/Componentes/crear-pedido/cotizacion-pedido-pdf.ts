export interface CotizacionPedidoDatos {
  rutCliente: string;
  nombreCliente?: string;
  fecha: string;
  total: number;
  productos?: string;
  anotaciones?: string;
  estado?: string;
}

export const avisoCotizacion = 'Documento referencial de cotización. No representa una boleta, factura ni compromiso vinculante de compra.';

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

  const moneda = (valor: number) => '$' + new Intl.NumberFormat('es-CL', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2
  }).format(valor);

  const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(datos.fecha);
  const fecha = partes ? `${partes[3]}/${partes[2]}/${partes[1]}` : (datos.fecha || 'Sin fecha');

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
    texto('PRESUPUESTO', ancho - margen, 70, 14, true, true, [191, 133, 66]);
    texto(avisoCotizacion, margen, 92, 9, false, false, [108, 123, 120]);
    texto(`Fecha de emisión: ${fecha}`, margen, 116, 10);
    texto('Moneda: pesos chilenos (CLP)', ancho - margen, 116, 9, false, true, [108, 123, 120]);
  };

  encabezado();

  let y = 142;

  // Cuadro con información del cliente
  pdf.setFillColor(248, 250, 249);
  pdf.setDrawColor(218, 228, 225);
  pdf.roundedRect(margen, y, ancho - margen * 2, 48, 4, 4, 'FD');

  const nombreMostrar = datos.nombreCliente ? datos.nombreCliente.trim() : 'Cliente sin registrar';
  texto('DATOS DEL CLIENTE', margen + 14, y + 18, 8, true, false, [20, 76, 72]);
  texto(`Nombre: ${nombreMostrar}`, margen + 14, y + 34, 10, true);
  texto(`RUT: ${datos.rutCliente}`, ancho - margen - 14, y + 34, 10, true, true);

  y += 68;

  // Cabecera de la tabla de conceptos cotizados
  const tabla = (posicionY: number) => {
    pdf.setFillColor(232, 241, 239);
    pdf.rect(margen, posicionY, ancho - margen * 2, 26, 'F');
    texto('PRODUCTOS / DETALLE COTIZADO', margen + 12, posicionY + 17, 9, true, false, [20, 76, 72]);
    texto('TOTAL ESTIMADO', ancho - margen - 12, posicionY + 17, 9, true, true, [20, 76, 72]);
    return posicionY + 26;
  };

  y = tabla(y);

  // Fila del pedido con productos o concepto principal
  const detalleProductos = datos.productos?.trim() || 'Confección y preparación de trabajo óptico';
  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(10);
  const lineasProductos: string[] = pdf.splitTextToSize(detalleProductos, ancho - margen * 2 - 130);
  const filaAltura = Math.max(34, lineasProductos.length * 15 + 16);

  if (y + filaAltura > alto - 120) {
    pdf.addPage();
    encabezado();
    y = tabla(142);
  }

  pdf.setFillColor(255, 255, 255);
  pdf.rect(margen, y, ancho - margen * 2, filaAltura, 'F');

  lineasProductos.forEach((linea, index) => {
    texto(linea, margen + 12, y + 18 + index * 15, 10, false, false, [35, 49, 52]);
  });
  texto(moneda(datos.total), ancho - margen - 12, y + 21, 11, true, true);

  pdf.setDrawColor(218, 228, 225);
  pdf.line(margen, y + filaAltura, ancho - margen, y + filaAltura);
  y += filaAltura + 18;

  // Bloque de Anotaciones / Especificaciones (si existen)
  if (datos.anotaciones && datos.anotaciones.trim()) {
    texto('ESPECIFICACIONES / ANOTACIONES:', margen, y, 9, true, false, [20, 76, 72]);
    y += 14;

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(9.5);
    const lineasAnotaciones: string[] = pdf.splitTextToSize(datos.anotaciones.trim(), ancho - margen * 2 - 20);

    const altoCaja = lineasAnotaciones.length * 14 + 18;
    if (y + altoCaja > alto - 120) {
      pdf.addPage();
      encabezado();
      y = 142;
    }

    pdf.setFillColor(247, 248, 245);
    pdf.setDrawColor(224, 232, 229);
    pdf.roundedRect(margen, y, ancho - margen * 2, altoCaja, 4, 4, 'FD');

    lineasAnotaciones.forEach((linea, index) => {
      texto(linea, margen + 10, y + 16 + index * 14, 9.5, false, false, [35, 49, 52]);
    });

    y += altoCaja + 20;
  }

  // Resumen del Total
  if (y + 90 > alto - 85) {
    pdf.addPage();
    encabezado();
    y = 142;
  }

  pdf.setFillColor(240, 246, 244);
  pdf.roundedRect(margen, y, ancho - margen * 2, 54, 4, 4, 'F');

  texto('TOTAL ESTIMADO DE LA COTIZACIÓN', margen + 16, y + 33, 11, true, false, [20, 76, 72]);
  texto(moneda(datos.total), ancho - margen - 16, y + 35, 18, true, true, [20, 76, 72]);

  y += 70;
  texto('* Esta cotización es válida por 15 días corridos a contar de su fecha de emisión.', margen, y, 8.5, false, false, [108, 123, 120]);
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

