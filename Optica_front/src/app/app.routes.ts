import { Routes } from '@angular/router';
import { ProductoComponent } from './registrar-producto/registrar-producto';

export const routes: Routes = [
	{ path: '', component: ProductoComponent },
	{ path: '**', redirectTo: '' }
];
