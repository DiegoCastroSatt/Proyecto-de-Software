import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';

interface DashboardSummary {
  customerCount: number;
  lowStockProductCount: number;
  pendingOrderCount: number;
  todaySales: number;
  updatedAt: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {
  protected readonly cargando = signal(true);
  protected readonly error = signal(false);
  protected readonly clientes = signal(0);
  protected readonly productosBajoStock = signal(0);
  protected readonly pedidosPendientes = signal(0);
  protected readonly ventasHoy = signal(0);
  protected readonly ultimaActualizacion = signal(new Date());

  constructor(private readonly http: HttpClient) {}

  ngOnInit(): void {
    this.actualizarResumen();
  }

  protected actualizarResumen(): void {
    this.cargando.set(true);
    this.error.set(false);

    this.http.get<DashboardSummary>('http://localhost:8080/api/Dashboard/summary')
      .pipe(catchError(() => of(null)))
      .subscribe({
      next: summary => {
        if (summary === null) {
          this.error.set(true);
          this.cargando.set(false);
          return;
        }

        this.clientes.set(summary.customerCount);
        this.productosBajoStock.set(summary.lowStockProductCount);
        this.pedidosPendientes.set(summary.pendingOrderCount);
        this.ventasHoy.set(summary.todaySales);
        this.ultimaActualizacion.set(new Date(summary.updatedAt));
        this.cargando.set(false);
      },
      error: () => {
        this.error.set(true);
        this.cargando.set(false);
      }
    });
  }

}
