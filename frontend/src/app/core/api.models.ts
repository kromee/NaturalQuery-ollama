/** Contrato de la API NaturalQuery.Api (ver Program.cs). */

export interface HealthResponse {
  status: string;
  databaseConfigured: boolean;
  ollamaModel: string | null;
}

export interface DatabaseHealthResponse {
  status: string;
  database: string;
}

export interface SqlPreviewResponse {
  question: string;
  model: string;
  sql: string;
  executed: false;
}

/** Celda de una fila: la API serializa `object?` como JSON primitivo. */
export type CellValue = string | number | boolean | null;

export interface SqlExecutionResponse {
  question: string;
  model: string;
  sql: string;
  answer: string;
  columns: string[];
  rows: CellValue[][];
  truncated: boolean;
  executed: true;
}

/**
 * Categorías de error según el código HTTP que devuelve la API:
 * - invalid-question: 400, pregunta vacía o de más de 500 caracteres
 * - sql-rejected: 422, el SQL generado no pasó el validador de solo lectura
 * - bad-model-response: 502, Ollama respondió fuera del formato esperado
 * - service-unavailable: 503, Ollama o SQL Server no responden
 * - network: no hubo respuesta (API caída o proxy sin destino)
 * - unknown: cualquier otro caso
 */
export type ApiErrorKind =
  | 'invalid-question'
  | 'sql-rejected'
  | 'bad-model-response'
  | 'service-unavailable'
  | 'network'
  | 'unknown';

export interface ApiError {
  kind: ApiErrorKind;
  status: number;
  /** Título legible para mostrar al usuario. */
  title: string;
  /** Detalle devuelto por la API, si lo hay. */
  detail?: string;
}

export const MAX_QUESTION_LENGTH = 500;
