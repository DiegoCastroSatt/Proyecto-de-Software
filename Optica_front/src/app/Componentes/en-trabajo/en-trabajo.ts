import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-en-trabajo',
  templateUrl: './en-trabajo.html',
  styleUrl: './en-trabajo.css'
})
export class EnTrabajoComponent {
  protected readonly titulo = inject(ActivatedRoute).snapshot.data['titulo'] as string;
}
