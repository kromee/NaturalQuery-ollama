import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { StatusBar } from './status-bar';

describe('StatusBar', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatusBar],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  function states(root: HTMLElement): string[] {
    return Array.from(root.querySelectorAll('li[data-state]')).map(
      (li) => li.getAttribute('data-state') ?? '',
    );
  }

  it('marks API and database as ok and shows the model', async () => {
    const fixture = TestBed.createComponent(StatusBar);
    fixture.detectChanges();

    http.expectOne('/health').flush({ status: 'ok', databaseConfigured: true, ollamaModel: 'qwen3:4b' });
    http.expectOne('/health/database').flush({ status: 'ok', database: 'LocalAiDemo' });
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(states(el)).toEqual(['ok', 'ok']);
    expect(el.textContent).toContain('Modelo: qwen3:4b');
  });

  it('warns when the database is not configured and skips the database check', async () => {
    const fixture = TestBed.createComponent(StatusBar);
    fixture.detectChanges();

    http.expectOne('/health').flush({ status: 'ok', databaseConfigured: false, ollamaModel: 'qwen3:4b' });
    http.expectNone('/health/database');
    await fixture.whenStable();

    expect(states(fixture.nativeElement)).toEqual(['ok', 'warn']);
  });

  it('marks the database as down when its health check fails', async () => {
    const fixture = TestBed.createComponent(StatusBar);
    fixture.detectChanges();

    http.expectOne('/health').flush({ status: 'ok', databaseConfigured: true, ollamaModel: null });
    http
      .expectOne('/health/database')
      .flush({ title: 'No se pudo conectar' }, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();

    expect(states(fixture.nativeElement)).toEqual(['ok', 'down']);
  });

  it('marks everything as down when the API is unreachable', async () => {
    const fixture = TestBed.createComponent(StatusBar);
    fixture.detectChanges();

    http.expectOne('/health').error(new ProgressEvent('error'));
    await fixture.whenStable();

    expect(states(fixture.nativeElement)).toEqual(['down', 'down']);
  });
});
