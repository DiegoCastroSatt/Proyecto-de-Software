import type { Venta } from '../../Componentes/registro-venta/registro-venta.service';

export const avisoComprobante = 'No es un documento tributario legal.';

export async function crearComprobanteVenta(venta: Venta) {
  const { jsPDF } = await import('jspdf');
  const pdf = new jsPDF({ unit: 'pt', format: 'a4' });
  pdf.setProperties({ title: `Comprobante de venta #${venta.idVenta}`, author: 'Centro Óptico San Francisco' });
  const ancho = pdf.internal.pageSize.getWidth();
  const alto = pdf.internal.pageSize.getHeight();
  const margen = 40;
  const moneda = (valor: number) => '$' + new Intl.NumberFormat('es-CL', {
    minimumFractionDigits: 0, maximumFractionDigits: 2
  }).format(valor);
  // Conserva la fecha local de Santiago devuelta por la API, sin convertirla a otra zona.
  const partes = /^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2}))?/.exec(venta.fecha);
  const fecha = partes ? `${partes[3]}/${partes[2]}/${partes[1]} ${partes[4] ?? ''}:${partes[5] ?? ''}`.trim().replace(/:$/, '') : 'Sin fecha registrada';
  const texto = (valor: string, x: number, y: number, tamano = 10, negrita = false, derecha = false) => {
    pdf.setFont('helvetica', negrita ? 'bold' : 'normal');
    pdf.setFontSize(tamano);
    pdf.setTextColor(35, 49, 52);
    pdf.text(valor, x, y, { align: derecha ? 'right' : 'left' });
  };
  const encabezado = () => {
    pdf.setFillColor(20, 76, 72);
    pdf.rect(0, 0, ancho, 7, 'F');
    texto('CENTRO ÓPTICO SAN FRANCISCO', margen, 44, 13, true);
    texto('COMPROBANTE DE VENTA', margen, 70, 19, true);
    texto(`#${venta.idVenta}`, ancho - margen, 70, 19, true, true);
    texto(avisoComprobante, margen, 92, 11, true);
    texto(`Fecha de venta: ${fecha}`, margen, 118);
    texto('Moneda: pesos chilenos (CLP)', ancho - margen, 118, 9, false, true);
  };
  const tabla = (y: number) => {
    pdf.setFillColor(232, 241, 239);
    pdf.rect(margen, y, ancho - margen * 2, 28, 'F');
    texto('PRODUCTO', margen + 10, y + 18, 9, true);
    texto('CANT.', 325, y + 18, 9, true, true);
    texto('PRECIO UNIT.', 438, y + 18, 9, true, true);
    texto('TOTAL', ancho - margen - 10, y + 18, 9, true, true);
    return y + 28;
  };
  encabezado();
  let y = 144;
  const cliente = venta.rutCliente
    ? `Cliente: ${venta.nombreCliente || 'Cliente vinculado'} | RUT: ${venta.rutCliente}`
    : 'Cliente: sin cliente vinculado';
  pdf.setFont('helvetica', 'normal'); pdf.setFontSize(10);
  const lineasCliente: string[] = pdf.splitTextToSize(cliente, ancho - margen * 2);
  for (const linea of lineasCliente) {
    if (y > alto - 110) { pdf.addPage(); encabezado(); y = 144; }
    texto(linea, margen, y); y += 14;
  }
  y = tabla(y + 14);
  for (const producto of venta.productos) {
    pdf.setFont('helvetica', 'normal'); pdf.setFontSize(10);
    const lineas: string[] = pdf.splitTextToSize(producto.nombre || 'Producto sin nombre registrado', 240);
    // Divide los nombres largos entre páginas sin invadir el total ni el pie.
    let primera = true;
    while (lineas.length) {
      if (y + 36 > alto - 100) { pdf.addPage(); encabezado(); y = tabla(144); }
      const capacidad = Math.max(1, Math.floor((alto - 100 - y - 18) / 14));
      const fragmento = lineas.splice(0, capacidad);
      const altura = Math.max(36, fragmento.length * 14 + 18);
      fragmento.forEach((linea, i) => texto(linea, margen + 10, y + 19 + i * 14));
      if (primera) {
        texto(String(producto.cantidad), 325, y + 19, 10, false, true);
        texto(moneda(producto.precioUnitario), 438, y + 19, 10, false, true);
        texto(moneda(producto.subtotal), ancho - margen - 10, y + 19, 10, true, true);
        primera = false;
      }
      pdf.setDrawColor(218, 228, 225);
      pdf.line(margen, y + altura, ancho - margen, y + altura);
      y += altura;
    }
  }
  if (y + 100 > alto - 85) { pdf.addPage(); encabezado(); y = 150; }
  y += 20;
  texto('TOTAL DE LA VENTA', margen + 10, y + 25, 12, true);
  texto(moneda(venta.total), ancho - margen - 10, y + 25, 20, true, true);
  texto('Importes registrados en la venta. No se desglosan impuestos.', margen + 10, y + 50, 9);
  const paginas = pdf.getNumberOfPages();
  for (let pagina = 1; pagina <= paginas; pagina++) {
    pdf.setPage(pagina);
    pdf.setDrawColor(218, 228, 225);
    pdf.line(margen, alto - 66, ancho - margen, alto - 66);
    texto(avisoComprobante, margen, alto - 48, 10, true);
    texto('Comprobante interno de la compra. No reemplaza una boleta ni una factura.', margen, alto - 33, 8);
    texto(`${pagina} / ${paginas}`, ancho - margen, alto - 48, 9, false, true);
  }
  return pdf;
}
