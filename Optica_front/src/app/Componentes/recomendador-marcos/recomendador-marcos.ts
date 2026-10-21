import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-recomendador-marcos',
  imports: [RouterLink],
  templateUrl: './recomendador-marcos.html',
  styleUrl: './recomendador-marcos.css',
})
export class RecomendadorMarcosComponent {
  readonly rostros = [
    {
      id: 'ovalado',
      nombre: 'Ovalado',
      rasgos: 'Un poco más largo que ancho, con mandíbula suavemente redondeada.',
      consejo:
        'Prueba distintas formas y mantén el ancho del marco cerca del ancho de tus pómulos.',
      marcos: [
        {
          nombre: 'Rectangulares',
          motivo: 'Aportan definición y contraste a las líneas suaves del rostro.',
        },
        {
          nombre: 'Geométricos',
          motivo: 'Destacan la mirada con ángulos y un estilo más marcado.',
        },
        { nombre: 'Aviador', motivo: 'Acompañan las proporciones equilibradas del rostro.' },
        { nombre: 'Cuadrados', motivo: 'Resaltan la mirada y aportan líneas definidas.' },
        { nombre: 'Redondos', motivo: 'Acompañan las facciones suaves con un estilo clásico.' },
        { nombre: 'Ovalados', motivo: 'Siguen las proporciones naturales del rostro.' },
        { nombre: 'Cat eye', motivo: 'Las esquinas elevadas dan protagonismo a los ojos.' },
        { nombre: 'Browline', motivo: 'El borde superior acentúa las cejas y la mirada.' },
        { nombre: 'Sin montura', motivo: 'Ofrecen una apariencia ligera y discreta.' },
        {
          nombre: 'Semimontura',
          motivo: 'Definen la parte superior sin cubrir demasiado las facciones.',
        },
      ],
    },
    {
      id: 'redondo',
      nombre: 'Redondo',
      rasgos: 'Mejillas llenas, contornos suaves y ancho similar al largo.',
      consejo:
        'Si buscas contraste, elige líneas definidas y un marco que no quede demasiado pequeño.',
      marcos: [
        {
          nombre: 'Rectangulares',
          motivo: 'Sus líneas rectas crean contraste con los contornos redondeados.',
        },
        { nombre: 'Cuadrados', motivo: 'Aportan estructura y destacan la zona de los ojos.' },
        { nombre: 'Cat eye', motivo: 'Las esquinas elevadas dirigen la atención hacia la mirada.' },
        {
          nombre: 'Geométricos',
          motivo: 'Sus ángulos añaden contraste a las mejillas y al contorno suave.',
        },
        { nombre: 'Browline', motivo: 'La línea superior marcada aporta definición a la mirada.' },
        {
          nombre: 'Wayfarer',
          motivo: 'Su perfil trapezoidal combina amplitud y líneas definidas.',
        },
        {
          nombre: 'Aviador de líneas angulares',
          motivo: 'Una versión menos redondeada permite explorar este estilo con más contraste.',
        },
      ],
    },
    {
      id: 'cuadrado',
      nombre: 'Cuadrado',
      rasgos: 'Frente amplia y mandíbula definida, con anchos parecidos.',
      consejo: 'Las curvas y los perfiles finos pueden dar un contraste suave a la mandíbula.',
      marcos: [
        { nombre: 'Redondos', motivo: 'Sus curvas contrastan con los ángulos de la mandíbula.' },
        { nombre: 'Ovalados', motivo: 'Aportan una línea suave sin ocultar las facciones.' },
        {
          nombre: 'Aviador',
          motivo: 'Su contorno curvo equilibra visualmente las líneas más rectas.',
        },
        {
          nombre: 'Cat eye de curvas suaves',
          motivo: 'Las esquinas elevadas y redondeadas acompañan la mirada.',
        },
        { nombre: 'Sin montura', motivo: 'Su presencia ligera deja las facciones a la vista.' },
        {
          nombre: 'Semimontura fina',
          motivo: 'Aporta definición arriba con una zona inferior más discreta.',
        },
        {
          nombre: 'Mariposa de curvas suaves',
          motivo: 'Su amplitud y sus curvas aportan contraste a una mandíbula marcada.',
        },
      ],
    },
    {
      id: 'alargado',
      nombre: 'Alargado',
      rasgos: 'El largo predomina sobre el ancho, con mejillas más estrechas.',
      consejo: 'Prueba lentes con más altura y evita que el marco quede demasiado estrecho.',
      marcos: [
        {
          nombre: 'Rectangulares altos',
          motivo: 'Añaden presencia horizontal y cubren más altura del rostro.',
        },
        {
          nombre: 'Redondos grandes',
          motivo: 'Aportan volumen visual en la zona central de la cara.',
        },
        { nombre: 'Aviador', motivo: 'La altura de sus lentes acompaña bien un rostro largo.' },
        {
          nombre: 'Cuadrados altos',
          motivo: 'Su altura ocupa más espacio en la zona central del rostro.',
        },
        {
          nombre: 'Wayfarer',
          motivo: 'La forma trapezoidal y una lente alta aportan presencia horizontal.',
        },
        { nombre: 'Browline', motivo: 'Su línea superior acentúa el ancho visual de la mirada.' },
        {
          nombre: 'Oversize proporcionados',
          motivo:
            'Un formato amplio puede acompañar el largo del rostro si ajusta bien sobre nariz y pómulos.',
        },
      ],
    },
    {
      id: 'corazon',
      nombre: 'Corazón',
      rasgos: 'Frente más amplia y rostro que se estrecha hacia el mentón.',
      consejo:
        'Los perfiles ligeros y las curvas suaves ayudan a que el marco no domine la frente.',
      marcos: [
        {
          nombre: 'Ovalados',
          motivo: 'Acompañan la transición entre la frente amplia y el mentón estrecho.',
        },
        {
          nombre: 'Redondos finos',
          motivo: 'Aportan suavidad sin añadir demasiado peso visual arriba.',
        },
        {
          nombre: 'Sin montura',
          motivo: 'Dan protagonismo a la mirada con una presencia discreta.',
        },
        {
          nombre: 'Aviador fino',
          motivo: 'La curva inferior aporta presencia cerca de las mejillas sin cargar la frente.',
        },
        {
          nombre: 'Semimontura discreta',
          motivo: 'Permite definir la mirada con menos peso visual que una montura gruesa.',
        },
        {
          nombre: 'Rectangulares de bordes suaves',
          motivo: 'Ofrecen contraste moderado con un perfil ligero.',
        },
        {
          nombre: 'Marcos transparentes',
          motivo:
            'Su acabado discreto permite explorar formas sin destacar demasiado la parte superior.',
        },
      ],
    },
    {
      id: 'diamante',
      nombre: 'Diamante',
      rasgos: 'Pómulos destacados, con frente y mentón más estrechos.',
      consejo: 'Busca un ajuste cómodo sobre los pómulos y prueba detalles en la parte superior.',
      marcos: [
        { nombre: 'Ovalados', motivo: 'Suavizan el contraste entre los pómulos y el mentón.' },
        { nombre: 'Cat eye', motivo: 'Destacan la zona superior y acompañan los pómulos.' },
        {
          nombre: 'Browline',
          motivo: 'Su borde superior marcado aporta presencia a la zona de la frente.',
        },
        {
          nombre: 'Redondos',
          motivo: 'Las curvas contrastan suavemente con los pómulos definidos.',
        },
        { nombre: 'Sin montura', motivo: 'Mantienen una presencia ligera alrededor de los ojos.' },
        {
          nombre: 'Semimontura',
          motivo: 'Destacan la zona superior sin recargar el contorno inferior.',
        },
        {
          nombre: 'Aviador fino',
          motivo: 'Su silueta curva ofrece otra opción si deja espacio cómodo sobre los pómulos.',
        },
      ],
    },
  ];
  readonly formasMarcos: Record<string, string> = {
    Rectangulares: 'rectangular',
    'Rectangulares altos': 'rectangular-alto',
    'Rectangulares de bordes suaves': 'rectangular',
    Cuadrados: 'cuadrado',
    'Cuadrados altos': 'cuadrado',
    Geométricos: 'geometrico',
    Aviador: 'aviador',
    'Aviador fino': 'aviador',
    'Aviador de líneas angulares': 'aviador-angular',
    Redondos: 'redondo',
    'Redondos finos': 'redondo',
    'Redondos grandes': 'redondo',
    Ovalados: 'ovalado',
    'Cat eye': 'cat-eye',
    'Cat eye de curvas suaves': 'cat-eye-suave',
    Browline: 'browline',
    Wayfarer: 'wayfarer',
    'Sin montura': 'sin-montura',
    Semimontura: 'semimontura',
    'Semimontura fina': 'semimontura',
    'Semimontura discreta': 'semimontura',
    'Mariposa de curvas suaves': 'mariposa',
    'Oversize proporcionados': 'oversize',
    'Marcos transparentes': 'transparente',
  };

  readonly seleccionado = signal<string | null>(null);
  seleccionar(id: string): void {
    this.seleccionado.set(id);
  }
  get recomendacion() {
    return this.rostros.find((rostro) => rostro.id === this.seleccionado());
  }
}
