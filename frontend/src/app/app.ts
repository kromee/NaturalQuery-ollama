import { Component, computed, inject, signal } from '@angular/core';
import { CellValue, SqlExecutionResponse } from './core/api.models';
import { QueryStore } from './core/query-store';
import { QuestionForm } from './question-form/question-form';
import { StatusBar } from './status-bar/status-bar';

interface ChartItem {
  label: string;
  value: number;
  width: number;
}

@Component({
  selector: 'app-root',
  imports: [StatusBar, QuestionForm],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly store = inject(QueryStore);
  protected readonly sqlDialogOpen = signal(false);

  protected readonly chartItems = computed<ChartItem[]>(() => {
    const result = this.store.result();
    if (!result) {
      return [];
    }

    const numericIndex = result.columns.findIndex((_, index) =>
      result.rows.some((row) => typeof row[index] === 'number'),
    );
    if (numericIndex < 0) {
      return [];
    }

    const labelIndex = result.columns.findIndex(
      (_, index) => index !== numericIndex && result.rows.some((row) => typeof row[index] === 'string'),
    );
    if (labelIndex < 0 || result.rows.length > 12) {
      return [];
    }

    const values = result.rows
      .map((row) => ({
        label: this.cellText(row[labelIndex]) || `Fila ${result.rows.indexOf(row) + 1}`,
        value: Number(row[numericIndex]),
      }))
      .filter((item) => Number.isFinite(item.value));
    const maxValue = Math.max(...values.map((item) => item.value), 1);

    return values.map((item) => ({ ...item, width: Math.max((item.value / maxValue) * 100, 2) }));
  });

  protected openSqlDialog(): void {
    this.sqlDialogOpen.set(true);
  }

  protected closeSqlDialog(): void {
    this.sqlDialogOpen.set(false);
  }

  protected copySql(sql: string): void {
    navigator.clipboard?.writeText(sql);
  }

  protected cellText(value: CellValue): string {
    if (value === null) {
      return '—';
    }
    if (typeof value === 'number') {
      return new Intl.NumberFormat('es-MX', { maximumFractionDigits: 2 }).format(value);
    }
    return String(value);
  }

  protected resultTitle(result: SqlExecutionResponse): string {
    return `${result.rows.length} resultado${result.rows.length === 1 ? '' : 's'}`;
  }
}
