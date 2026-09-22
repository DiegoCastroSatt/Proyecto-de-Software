import { Component } from '@angular/core';
import { BuscarProductoComponent } from '../buscar-producto/buscar-producto';

@Component({
  selector: 'app-buscar-producto-admin',
  imports: [BuscarProductoComponent],
  templateUrl: './buscar-producto-admin.html',
  styleUrl: './buscar-producto-admin.css',
})
export class BuscarProductoAdmin {}
