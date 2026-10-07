import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';
import { ClienteService, Cliente } from '../clientes/cliente.service';
import { HistorialGraduacionesService, RecetaHistorial } from './historial-graduaciones.service';

@Component({
  selector: 'app-historial-graduaciones',
  imports: [AutocompletadoRut, DatePipe, RouterLink],
  templateUrl: './historial-graduaciones.html',
  styleUrl: './historial-graduaciones.css',
})
export class HistorialGraduaciones implements OnInit {
  private readonly route = inject(ActivatedRoute);
  protected readonly verRecetas = this.route.snapshot.data['verRecetas'] === true;
  protected readonly apiBase = 'http://localhost:8080';

  private readonly clienteService = inject(ClienteService);
  protected readonly clientes = signal<Cliente[]>([]);
  protected readonly terminoCliente = signal('');
  protected readonly cargandoClientes = signal(false);
  protected readonly errorClientes = signal('');
  protected readonly clienteSeleccionado = signal<Cliente | null>(null);
  protected readonly clientesFiltrados = computed(() => {
    const termino = this.terminoCliente().trim().toLocaleLowerCase('es');
    const rutBuscado = termino.replace(/[.\-\s]/g, '');
    return this.clientes().filter(cliente => !termino
      || `${cliente.nombre} ${cliente.apellido}`.toLocaleLowerCase('es').includes(termino)
      || (rutBuscado.length > 0 && cliente.rut.replace(/[.\-\s]/g, '').toLowerCase().includes(rutBuscado)));
  });

  protected actualizarBusquedaCliente(termino: string): void {
    this.terminoCliente.set(termino);
    const rutNormalizado = termino.replace(/[.\-\s]/g, '').toUpperCase();
    const cliente = this.clientes().find(c => c.rut.replace(/[.\-\s]/g, '').toUpperCase() === rutNormalizado);
    if (cliente && cliente.rut !== this.rut()) this.seleccionarCliente(cliente);
  }

  protected seleccionarCliente(cliente: Cliente): void {
    if (this.isLoading()) return;
    this.clienteSeleccionado.set(cliente);
    this.rut.set(cliente.rut);
    this.recetas.set([]);
    this.buscarHistorial();
  }

  protected cargarClientes(): void {
    this.cargandoClientes.set(true);
    this.errorClientes.set('');
    this.clienteService.buscar().subscribe({
      next: clientes => { this.clientes.set(clientes); this.cargandoClientes.set(false); },
      error: () => { this.errorClientes.set('No se pudo cargar la lista de clientes.'); this.cargandoClientes.set(false); }
    });
  }

  ngOnInit(): void {
    if (this.verRecetas) this.cargarClientes();
    const rut = this.route.snapshot.queryParamMap.get('rut');
    if (rut) { this.rut.set(rut); this.buscarHistorial(); }
  }

  private readonly historialService = inject(HistorialGraduacionesService);

  protected readonly rut = signal('');
  protected readonly recetas = signal<RecetaHistorial[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly yaBusco = signal(false);

  protected buscarHistorial(): void {
    const rutValor = this.rut().trim();

    if (!rutValor) {
      this.errorMessage.set('Ingresa el RUT del cliente.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.yaBusco.set(true);

    this.historialService.obtenerHistorial(rutValor).subscribe({
      next: (resultado) => {
        const recetas = this.verRecetas
          ? [...resultado].sort((a, b) => b.fecha.localeCompare(a.fecha) || b.id - a.id).slice(0, 1)
          : resultado;
        this.recetas.set(recetas);
        this.isLoading.set(false);
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.recetas.set([]);
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudo obtener el historial. Inténtalo nuevamente.');
        this.isLoading.set(false);
      },
    });
  }
}