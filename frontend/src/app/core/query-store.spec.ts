import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SqlExecutionResponse } from './api.models';
import { QueryStore } from './query-store';

describe('QueryStore', () => {
  let store: QueryStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    store = TestBed.inject(QueryStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const execution: SqlExecutionResponse = {
    question: '¿Cuántos productos hay?',
    model: 'qwen3:4b',
    sql: 'SELECT COUNT(*) AS TotalProductos FROM Producto',
    answer: 'Hay 12 productos.',
    columns: ['TotalProductos'],
    rows: [[12]],
    truncated: false,
    executed: true,
  };

  it('executes a question and stores the result', () => {
    store.submit('  ¿Cuántos productos hay?  ', 'execute');
    expect(store.loading()).toBe('execute');

    const req = http.expectOne('/api/ai/sql');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ question: '¿Cuántos productos hay?' });
    req.flush(execution);

    expect(store.loading()).toBeNull();
    expect(store.result()).toEqual(execution);
    expect(store.error()).toBeNull();
  });

  it('requests only the SQL preview in preview mode', () => {
    store.submit('¿Cuántos clientes hay?', 'preview');

    http.expectOne('/api/ai/sql/preview').flush({
      question: '¿Cuántos clientes hay?',
      model: 'qwen3:4b',
      sql: 'SELECT COUNT(*) AS TotalClientes FROM Cliente',
      executed: false,
    });

    expect(store.preview()?.sql).toContain('Cliente');
    expect(store.result()).toBeNull();
  });

  it('maps API failures to a typed error and clears loading', () => {
    store.submit('¿Cuántos clientes hay?', 'execute');

    http
      .expectOne('/api/ai/sql')
      .flush(
        { title: 'No se pudo comunicar con Ollama', detail: 'Ollama respondió con HTTP 500.' },
        { status: 503, statusText: 'Service Unavailable' },
      );

    expect(store.loading()).toBeNull();
    expect(store.error()).toEqual(
      jasmine.objectContaining({ kind: 'service-unavailable', status: 503 }),
    );
  });

  it('clears the previous result when a new question starts', () => {
    store.submit('¿Cuántos productos hay?', 'execute');
    http.expectOne('/api/ai/sql').flush(execution);

    store.submit('¿Cuántos clientes hay?', 'execute');
    expect(store.result()).toBeNull();
    http.expectOne('/api/ai/sql');
    store.cancel();
  });

  it('cancels the in-flight request', () => {
    store.submit('¿Cuántos productos hay?', 'execute');
    const req = http.expectOne('/api/ai/sql');

    store.cancel();

    expect(req.cancelled).toBeTrue();
    expect(store.loading()).toBeNull();
    expect(store.error()).toBeNull();
  });

  it('ignores empty, too long, or concurrent submissions', () => {
    store.submit('   ', 'execute');
    store.submit('x'.repeat(501), 'execute');
    http.expectNone('/api/ai/sql');
    expect(store.loading()).toBeNull();

    store.submit('¿Cuántos productos hay?', 'execute');
    store.submit('otra pregunta', 'execute');
    expect(http.match('/api/ai/sql').length).toBe(1);
    store.cancel();
  });
});
