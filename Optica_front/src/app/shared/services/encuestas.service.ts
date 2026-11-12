import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { obtenerOrigenApi } from './api-origin';

/** Datos públicos de la atención asociada a un enlace de encuesta. */
export interface EncuestaPublica {
  /** Fecha de la atención en formato yyyy-MM-dd. */
  fechaAtencion: string;
  /** Hora de inicio de la atención en formato HH:mm. */
  horaInicio: string;
}

export interface RespuestaEncuesta {
  calificacionGeneral: number;
  calificacionTrato: number;
  calificacionPuntualidad: number;
  comentario: string | null;
}

export interface MensajeEncuesta {
  mensaje: string;
}

export interface DistribucionEstrellas {
  estrellas: number;
  cantidad: number;
}

export interface EvolucionMensualEncuestas {
  /** Mes en formato yyyy-MM. */
  mes: string;
  respuestas: number;
  promedioGeneral: number;
  promedioTrato: number;
  promedioPuntualidad: number;
}

export interface ResumenEncuestas {
  enviadas: number;
  respondidas: number;
  /** Porcentaje de 0 a 100. */
  tasaRespuesta: number;
  promedioGeneral: number | null;
  promedioTrato: number | null;
  promedioPuntualidad: number | null;
  distribucionGeneral: DistribucionEstrellas[];
  evolucionMensual: EvolucionMensualEncuestas[];
}

export interface ComentarioEncuesta {
  id: number;
  /** Mes de la respuesta en formato yyyy-MM. No se expone la fecha exacta. */
  mes: string;
  calificacionGeneral: number;
  calificacionTrato: number;
  calificacionPuntualidad: number;
  /** Es null si no hay comentario o si el personal lo ocultó. */
  comentario: string | null;
  comentarioOculto: boolean;
}

export interface ComentariosEncuestasPaginados {
  elementos: ComentarioEncuesta[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface FiltroResultadosEncuestas {
  /** Fecha inicial en formato yyyy-MM-dd. */
  desde: string;
  /** Fecha final en formato yyyy-MM-dd. */
  hasta: string;
}

export interface FiltroComentariosEncuestas extends FiltroResultadosEncuestas {
  calificacion: number | null;
  soloConComentario: boolean;
  pagina: number;
  tamanoPagina: number;
}

@Injectable({ providedIn: 'root' })
export class EncuestasService {
  private readonly http = inject(HttpClient);
  // Los endpoints públicos no requieren cookie de sesión.
  private readonly apiPublica = `${obtenerOrigenApi()}/api/Encuestas`;
  // El interceptor del personal reescribe este origen y agrega las credenciales.
  private readonly apiPersonal = 'http://localhost:8080/api/Encuestas';

  obtenerEncuesta(token: string): Observable<EncuestaPublica> {
    return this.http.get<EncuestaPublica>(
      `${this.apiPublica}/responder/${encodeURIComponent(token)}`,
    );
  }

  responder(token: string, respuesta: RespuestaEncuesta): Observable<MensajeEncuesta> {
    return this.http.post<MensajeEncuesta>(
      `${this.apiPublica}/responder/${encodeURIComponent(token)}`,
      respuesta,
    );
  }

  obtenerResumen(filtro: FiltroResultadosEncuestas): Observable<ResumenEncuestas> {
    const params = new HttpParams().set('desde', filtro.desde).set('hasta', filtro.hasta);
    return this.http.get<ResumenEncuestas>(`${this.apiPersonal}/resumen`, { params });
  }

  obtenerComentarios(
    filtro: FiltroComentariosEncuestas,
  ): Observable<ComentariosEncuestasPaginados> {
    let params = new HttpParams()
      .set('desde', filtro.desde)
      .set('hasta', filtro.hasta)
      .set('soloConComentario', filtro.soloConComentario)
      .set('pagina', filtro.pagina)
      .set('tamanoPagina', filtro.tamanoPagina);
    if (filtro.calificacion !== null) params = params.set('calificacion', filtro.calificacion);
    return this.http.get<ComentariosEncuestasPaginados>(`${this.apiPersonal}/comentarios`, {
      params,
    });
  }

  /** Oculta o vuelve a mostrar un comentario. Las calificaciones de la respuesta siguen contando. */
  cambiarVisibilidadComentario(idRespuesta: number, oculto: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiPersonal}/respuestas/${idRespuesta}/comentario-oculto`, {
      oculto,
    });
  }
}
