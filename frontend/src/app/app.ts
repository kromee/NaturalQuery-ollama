import { Component, inject } from '@angular/core';
import { QueryStore } from './core/query-store';
import { QuestionForm } from './question-form/question-form';
import { StatusBar } from './status-bar/status-bar';

@Component({
  selector: 'app-root',
  imports: [StatusBar, QuestionForm],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly store = inject(QueryStore);
}
