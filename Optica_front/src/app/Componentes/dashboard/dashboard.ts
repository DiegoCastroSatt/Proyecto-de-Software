import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
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
  protected cargando = true;
  protected error = false;
  protected clientes = 0;
  protected productosBajoStock = 0;
  protected pedidosPendientes = 0;
  protected ventasHoy = 0;
  protected ultimaActualizacion = new Date();

  constructor(private readonly http: HttpClient) {}

  ngOnInit(): void {
    this.actualizarResumen();
  }

  protected actualizarResumen(): void {
    this.cargando = true;
    this.error = false;

    this.http.get<DashboardSummary>('http://localhost:8080/api/Dashboard/summary')
      .pipe(catchError(() => of(null)))
      .subscribe({
      next: summary => {
        if (summary === null) {
          this.error = true;
          this.cargando = false;
          return;
        }

        this.clientes = summary.customerCount;
        this.productosBajoStock = summary.lowStockProductCount;
        this.pedidosPendientes = summary.pendingOrderCount;
        this.ventasHoy = summary.todaySales;
        this.ultimaActualizacion = new Date(summary.updatedAt);
        this.cargando = false;
      },
      error: () => {
        this.error = true;
        this.cargando = false;
      }
    });
  }

}
