import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService } from './core/api.service';
import { ApiError, HealthResponse } from './core/api.models';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(ApiService);

  protected readonly health = signal<HealthResponse | null>(null);
  protected readonly error = signal<ApiError | null>(null);

  ngOnInit(): void {
    this.api.health().subscribe({
      next: (health) => this.health.set(health),
      error: (error: ApiError) => this.error.set(error),
    });
  }
}
