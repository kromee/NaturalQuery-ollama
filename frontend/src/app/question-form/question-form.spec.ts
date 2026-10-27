import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { QueryStore } from '../core/query-store';
import { QuestionForm } from './question-form';

describe('QuestionForm', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuestionForm],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    TestBed.inject(QueryStore).cancel();
    http.verify();
  });

  function setup() {
    const fixture = TestBed.createComponent(QuestionForm);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    const buttons = () => Array.from(el.querySelectorAll('button')) as HTMLButtonElement[];
    const type = async (value: string) => {
      textarea.value = value;
      textarea.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    return { fixture, el, textarea, buttons, type };
  }

  it('disables both buttons while the question is empty', () => {
    const { buttons } = setup();
    expect(buttons().map((b) => b.disabled)).toEqual([true, true]);
  });

  it('enables the buttons and updates the counter when typing', async () => {
    const { el, buttons, type } = setup();
    await type('¿Cuántos productos hay?');

    expect(buttons().map((b) => b.disabled)).toEqual([false, false]);
    expect(el.querySelector('.counter')?.textContent).toContain('23/500');
  });

  it('blocks submission and flags the counter beyond 500 characters', async () => {
    const { el, buttons, type } = setup();
    await type('x'.repeat(501));

    expect(buttons().every((b) => b.disabled)).toBeTrue();
    expect(el.querySelector('.counter.over')).toBeTruthy();
  });

  it('"Ver SQL" calls the preview endpoint', async () => {
    const { buttons, type } = setup();
    await type('¿Cuántos clientes hay?');

    buttons()[0].click();

    const req = http.expectOne('/api/ai/sql/preview');
    expect(req.request.body).toEqual({ question: '¿Cuántos clientes hay?' });
  });

  it('"Consultar" and Ctrl+Enter call the execute endpoint', async () => {
    const { fixture, textarea, buttons, type } = setup();
    await type('¿Cuántos clientes hay?');

    buttons()[1].click();
    http.expectOne('/api/ai/sql').flush({}, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();

    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', ctrlKey: true }));
    const req = http.expectOne('/api/ai/sql');
    expect(req.request.body).toEqual({ question: '¿Cuántos clientes hay?' });
  });

  it('shows a loading indicator with a cancel button while waiting', async () => {
    const { fixture, el, buttons, type } = setup();
    await type('¿Cuántos clientes hay?');

    buttons()[1].click();
    await fixture.whenStable();
    http.expectOne('/api/ai/sql');

    expect(el.querySelector('[role="status"]')?.textContent).toContain('Generando SQL');
    expect(buttons().find((b) => b.textContent?.includes('Cancelar'))).toBeTruthy();
  });
});
