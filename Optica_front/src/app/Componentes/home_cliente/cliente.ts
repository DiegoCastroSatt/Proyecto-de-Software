import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AutenticacionClienteService } from '../login-cliente/autenticacion-cliente.service';

@Component({
  selector: 'app-cliente',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './cliente.html',
  styleUrl: './cliente.css'
})
export class ClienteLayoutComponent {
  protected readonly authService = inject(AutenticacionClienteService);
}
