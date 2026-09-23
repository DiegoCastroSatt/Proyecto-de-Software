import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-cliente',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './cliente.html',
  styleUrl: './cliente.css'
})
export class ClienteLayoutComponent {}
