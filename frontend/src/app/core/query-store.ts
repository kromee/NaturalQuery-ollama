import { Injectable, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiService } from './api.service';
import {
  ApiError,
  MAX_QUESTION_LENGTH,
  SqlExecutionResponse,
  SqlPreviewResponse,
} from './api.models';

/** `preview` solo genera y valida el SQL; `execute` además lo ejecuta. */
export type QueryMode = 'preview' | 'execute';

/**
 * Estado compartido de la consulta en curso. Los componentes leen los signals
 * y llaman a `submit()` / `cancel()`; no conocen HTTP.
 */
@Injectable({ providedIn: 'root' })
export class QueryStore {
  private readonly api = inject(ApiService);
  private subscription?: Subscription;
  private timer?: ReturnType<typeof setInterval>;

  /** Modo de la petición en vuelo, o `null` si no hay ninguna. */
  readonly loading = signal<QueryMode | null>(null);
  readonly busy = computed(() => this.loading() !== null);
  readonly elapsedMs = signal(0);

  readonly error = signal<ApiError | null>(null);
  readonly preview = signal<SqlPreviewResponse | null>(null);
  readonly result = signal<SqlExecutionResponse | null>(null);

  submit(rawQuestion: string, mode: QueryMode): void {
    const question = rawQuestion.trim();
    if (this.busy() || question.length === 0 || question.length > MAX_QUESTION_LENGTH) {
      return;
    }

    this.error.set(null);
    this.preview.set(null);
    this.result.set(null);
    this.loading.set(mode);
    this.startTimer();

    if (mode === 'preview') {
      this.subscription = this.api.preview(question).subscribe({
        next: (response) => this.preview.set(response),
        error: (error: ApiError) => this.finish(error),
        complete: () => this.finish(),
      });
    } else {
      this.subscription = this.api.ask(question).subscribe({
        next: (response) => this.result.set(response),
        error: (error: ApiError) => this.finish(error),
        complete: () => this.finish(),
      });
    }
  }

  /** Cancela la petición en curso (útil: el modelo local puede tardar). */
  cancel(): void {
    this.subscription?.unsubscribe();
    this.finish();
  }

  private finish(error?: ApiError): void {
    this.stopTimer();
    this.loading.set(null);
    if (error) {
      this.error.set(error);
    }
  }

  private startTimer(): void {
    this.stopTimer();
    const startedAt = performance.now();
    this.elapsedMs.set(0);
    this.timer = setInterval(() => this.elapsedMs.set(performance.now() - startedAt), 100);
  }

  private stopTimer(): void {
    if (this.timer !== undefined) {
      clearInterval(this.timer);
      this.timer = undefined;
    }
  }
}
