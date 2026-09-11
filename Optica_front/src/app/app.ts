import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ReservaComponent } from './components/reserva/reserva';
import { ClientesComponent } from './components/clientes/clientes';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, ReservaComponent, ClientesComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
  host: {
    'ngSkipHydration': 'true' 
  }
})
export class App {
  title = 'optica-web';
  pestanaActiva: 'reservas' | 'clientes' = 'clientes';
}