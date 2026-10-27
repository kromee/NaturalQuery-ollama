import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, catchError, interval, map, of, startWith, switchMap } from 'rxjs';
import { ApiService } from '../core/api.service';

/** `warn` = configurado a medias (p. ej. API sin cadena de conexión). */
type Light = 'checking' | 'ok' | 'warn' | 'down';

interface Snapshot {
  api: Light;
  db: Light;
  model: string | null;
}

const REFRESH_MS = 30_000;

@Component({
  selector: 'app-status-bar',
  templateUrl: './status-bar.html',
  styleUrl: './status-bar.css',
})
export class StatusBar {
  private readonly client = inject(ApiService);

  protected readonly api = signal<Light>('checking');
  protected readonly db = signal<Light>('checking');
  protected readonly model = signal<string | null>(null);

  protected readonly labels: Record<Light, string> = {
    checking: 'comprobando…',
    ok: 'conectado',
    warn: 'sin configurar',
    down: 'sin conexión',
  };

  constructor() {
    interval(REFRESH_MS)
      .pipe(
        startWith(0),
        switchMap(() => this.check()),
        takeUntilDestroyed(),
      )
      .subscribe((snapshot) => {
        this.api.set(snapshot.api);
        this.db.set(snapshot.db);
        this.model.set(snapshot.model);
      });
  }

  private check(): Observable<Snapshot> {
    return this.client.health().pipe(
      switchMap((health) => {
        const model = health.ollamaModel;
        if (!health.databaseConfigured) {
          return of<Snapshot>({ api: 'ok', db: 'warn', model });
        }
        return this.client.databaseHealth().pipe(
          map((): Snapshot => ({ api: 'ok', db: 'ok', model })),
          catchError(() => of<Snapshot>({ api: 'ok', db: 'down', model })),
        );
      }),
      catchError(() => of<Snapshot>({ api: 'down', db: 'down', model: null })),
    );
  }
}
