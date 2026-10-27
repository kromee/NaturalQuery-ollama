import { Component, computed, inject, signal } from '@angular/core';
import { MAX_QUESTION_LENGTH } from '../core/api.models';
import { QueryMode, QueryStore } from '../core/query-store';

@Component({
  selector: 'app-question-form',
  templateUrl: './question-form.html',
  styleUrl: './question-form.css',
})
export class QuestionForm {
  protected readonly store = inject(QueryStore);

  protected readonly maxLength = MAX_QUESTION_LENGTH;
  protected readonly question = signal('');

  protected readonly tooLong = computed(() => this.question().length > MAX_QUESTION_LENGTH);
  protected readonly canSubmit = computed(
    () => this.question().trim().length > 0 && !this.tooLong() && !this.store.busy(),
  );

  protected readonly seconds = computed(() => (this.store.elapsedMs() / 1000).toFixed(1));

  protected onInput(event: Event): void {
    this.question.set((event.target as HTMLTextAreaElement).value);
  }

  /** Ctrl/Cmd + Enter consulta, igual que el botón principal. */
  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.run('execute');
    }
  }

  protected run(mode: QueryMode): void {
    if (this.canSubmit()) {
      this.store.submit(this.question(), mode);
    }
  }
}
