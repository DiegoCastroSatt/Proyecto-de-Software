import { Component } from '@angular/core';
import { ReservaComponent } from './Componentes/reserva/reserva';

@Component({
  selector: 'app-root',
  imports: [ReservaComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
