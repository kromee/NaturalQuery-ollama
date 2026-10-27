import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import {
  ApiError,
  DatabaseHealthResponse,
  HealthResponse,
  SqlExecutionResponse,
  SqlPreviewResponse,
} from './api.models';

/**
 * Cliente tipado del backend. Las rutas son relativas: en desarrollo las
 * resuelve el proxy de `proxy.conf.json` y en producción el mismo host de .NET.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  health(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>('/health').pipe(catchError(toApiError));
  }

  databaseHealth(): Observable<DatabaseHealthResponse> {
    return this.http.get<DatabaseHealthResponse>('/health/database').pipe(catchError(toApiError));
  }

  /** Genera y valida el SQL sin ejecutarlo. */
  preview(question: string): Observable<SqlPreviewResponse> {
    return this.http
      .post<SqlPreviewResponse>('/api/ai/sql/preview', { question })
      .pipe(catchError(toApiError));
  }

  /** Genera, valida y ejecuta la consulta de solo lectura. */
  ask(question: string): Observable<SqlExecutionResponse> {
    return this.http
      .post<SqlExecutionResponse>('/api/ai/sql', { question })
      .pipe(catchError(toApiError));
  }
}

/** Cuerpo de error posible: `{ error }` (400) o ProblemDetails (`title`, `detail`). */
interface ErrorBody {
  error?: string;
  title?: string;
  detail?: string;
}

function toApiError(response: HttpErrorResponse): Observable<never> {
  const body = (response.error ?? {}) as ErrorBody;
  const detail = body.detail ?? body.error;

  let error: ApiError;
  switch (response.status) {
    case 0:
      error = {
        kind: 'network',
        status: 0,
        title: 'No se pudo contactar con la API',
        detail: 'Verifica que el backend esté en ejecución (dotnet run --project src/NaturalQuery.Api).',
      };
      break;
    case 400:
      error = {
        kind: 'invalid-question',
        status: 400,
        title: 'Pregunta no válida',
        detail,
      };
      break;
    case 422:
      error = {
        kind: 'sql-rejected',
        status: 422,
        title: body.title ?? 'La consulta generada no pasó la validación',
        detail,
      };
      break;
    case 502:
      error = {
        kind: 'bad-model-response',
        status: 502,
        title: body.title ?? 'El modelo devolvió una respuesta inesperada',
        detail,
      };
      break;
    case 503:
      error = {
        kind: 'service-unavailable',
        status: 503,
        title: body.title ?? 'Servicio no disponible',
        detail,
      };
      break;
    default:
      error = {
        kind: 'unknown',
        status: response.status,
        title: body.title ?? 'Error inesperado',
        detail,
      };
  }

  return throwError(() => error);
}
