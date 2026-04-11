import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DashboardService, DashboardStats, AdminBooking } from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-lg-10">
          <div class="card shadow-sm rounded-4 border-0">
            <div class="card-body p-5">
              <div class="d-flex align-items-start justify-content-between gap-3 flex-column flex-md-row mb-4">
                <div>
                  <h2 class="h4 mb-1">Admin Dashboard</h2>
                  <p class="text-muted mb-0">Áttekintés a RentGrid kulcsfontosságú statisztikáiról.</p>
                </div>
                <a class="btn btn-outline-secondary" routerLink="/">Vissza a kezdőlapra</a>
              </div>

              <div *ngIf="loading" class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                  <span class="visually-hidden">Betöltés...</span>
                </div>
              </div>

              <div *ngIf="error" class="alert alert-danger">{{ error }}</div>

              <div *ngIf="stats && !loading">
                <div class="row g-4 mb-4">
                  <div class="col-sm-6 col-xl-3" *ngFor="let card of statCards">
                    <div class="card h-100 border-0 shadow-sm rounded-4">
                      <div class="card-body">
                        <h6 class="text-uppercase text-muted mb-3">{{ card.title }}</h6>
                        <div class="d-flex align-items-center justify-content-between">
                          <div>
                            <p class="h3 mb-0">{{ card.value }}</p>
                            <p class="text-muted mb-0">{{ card.subtitle }}</p>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="card border-0 shadow-sm rounded-4 mb-4">
                  <div class="card-body">
                    <h5 class="h5 mb-3">Legnépszerűbb jármű</h5>
                    <div *ngIf="stats.mostPopularVehicle; else noPopularVehicle">
                      <p class="mb-2"><strong>{{ stats.mostPopularVehicle.brand }} {{ stats.mostPopularVehicle.model }}</strong></p>
                      <p class="mb-1 text-muted">Foglalások száma: {{ stats.mostPopularVehicle.bookingCount }}</p>
                      <p class="text-muted mb-0">Javasolt további promóciós akció vagy készletkezelés ehhez a járműhöz.</p>
                    </div>
                    <ng-template #noPopularVehicle>
                      <p class="text-muted mb-0">Még nincs elég foglalás az adatok meghatározásához.</p>
                    </ng-template>
                  </div>
                </div>

                <div class="row g-4 mb-4">
                  <div class="col-sm-6 col-md-3">
                    <div class="card border-0 shadow-sm rounded-4 p-3 h-100">
                      <p class="text-uppercase text-muted mb-2">Összes admin foglalás</p>
                      <p class="h3 mb-1">{{ adminSummary().total }}</p>
                    </div>
                  </div>
                  <div class="col-sm-6 col-md-3">
                    <div class="card border-0 shadow-sm rounded-4 p-3 h-100">
                      <p class="text-uppercase text-muted mb-2">Pending</p>
                      <p class="h3 mb-1">{{ adminSummary().pending }}</p>
                    </div>
                  </div>
                  <div class="col-sm-6 col-md-3">
                    <div class="card border-0 shadow-sm rounded-4 p-3 h-100">
                      <p class="text-uppercase text-muted mb-2">Confirmed</p>
                      <p class="h3 mb-1">{{ adminSummary().confirmed }}</p>
                    </div>
                  </div>
                  <div class="col-sm-6 col-md-3">
                    <div class="card border-0 shadow-sm rounded-4 p-3 h-100">
                      <p class="text-uppercase text-muted mb-2">Cancelled</p>
                      <p class="h3 mb-1">{{ adminSummary().cancelled }}</p>
                    </div>
                  </div>
                </div>

                <div class="row gy-3 mb-4">
                  <div class="col-md-3">
                    <label class="form-label">Státusz szűrő</label>
                    <select class="form-select" [ngModel]="adminStatusFilter()" (ngModelChange)="adminStatusFilter.set($event); adminPage.set(1)">
                      <option value="All">Összes</option>
                      <option value="Pending">Pending</option>
                      <option value="Confirmed">Confirmed</option>
                      <option value="Cancelled">Cancelled</option>
                    </select>
                  </div>
                  <div class="col-md-3">
                    <label class="form-label">Keresés</label>
                    <input class="form-control" type="search" placeholder="Felhasználó vagy jármű" [ngModel]="adminSearchTerm()" (ngModelChange)="adminSearchTerm.set($event); adminPage.set(1)" />
                    <select class="form-select" [ngModel]="adminPageSize()" (ngModelChange)="adminPageSize.set($event); adminPage.set(1)">
                      <option [value]="5">5</option>
                      <option [value]="10">10</option>
                      <option [value]="25">25</option>
                      <option [value]="50">50</option>
                    </select>
                  </div>
                  <div class="col-md-3 d-flex align-items-end justify-content-md-end">
                    <button class="btn btn-outline-secondary w-100" type="button" (click)="loadAdminBookings()">Frissítés</button>
                  </div>
                </div>

                <div class="list-group rounded-4 overflow-hidden">
                  <div class="list-group-item bg-light border-0">
                    <h6 class="mb-1">Admin foglalások</h6>
                    <p class="text-muted mb-0">Itt kezelheti a felhasználói foglalások státuszát.</p>
                  </div>
                </div>

                <div class="card border-0 shadow-sm rounded-4 mt-4">
                  <div class="card-body">
                    <div class="d-flex align-items-center justify-content-between mb-3 gap-3 flex-column flex-md-row">
                      <div>
                        <h5 class="h5 mb-1">Foglalások kezelése</h5>
                        <p class="text-muted mb-0">Válasszon státuszt a foglalások gyors frissítéséhez.</p>
                      </div>
                      <button class="btn btn-outline-secondary btn-sm" type="button" (click)="loadAdminBookings()">Lista frissítése</button>
                    </div>

                    <div *ngIf="adminLoading" class="text-center py-4">
                      <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Betöltés...</span>
                      </div>
                    </div>

                    <div *ngIf="adminError" class="alert alert-danger">{{ adminError }}</div>

                    <div *ngIf="!adminLoading && !adminError && filteredAdminBookings().length === 0" class="alert alert-info">
                      Nincs megjeleníthető foglalás a keresés vagy a szűrő alapján.
                    </div>

                    <div *ngIf="!adminLoading && !adminError && filteredAdminBookings().length > 0" class="table-responsive">
                      <table class="table table-hover align-middle">
                        <thead class="table-light">
                          <tr>
                            <th>ID</th>
                            <th>Felhasználó</th>
                            <th>Jármű</th>
                            <th>Időszak</th>
                            <th>Összeg</th>
                            <th>Státusz</th>
                            <th>Módosítás</th>
                            <th>Részletek</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr *ngFor="let booking of paginatedAdminBookings()">
                            <td>{{ booking.id }}</td>
                            <td>
                              <div>{{ booking.userFullName }}</div>
                              <div class="text-muted small">{{ booking.userEmail }}</div>
                            </td>
                            <td>
                              <div>{{ booking.vehicleBrand }} {{ booking.vehicleModel }}</div>
                            </td>
                            <td>{{ booking.startDate | date:'yyyy.MM.dd' }} – {{ booking.endDate | date:'yyyy.MM.dd' }}</td>
                            <td>{{ booking.totalPrice | number:'1.0-0' }} Ft</td>
                            <td>
                              <span class="badge"
                                [class.bg-warning]="booking.status === 'Pending'"
                                [class.bg-success]="booking.status === 'Confirmed'"
                                [class.bg-danger]="booking.status === 'Cancelled'"
                              >
                                {{ booking.status }}
                              </span>
                            </td>
                            <td>
                              <div class="d-flex gap-2 flex-wrap align-items-center">
                                <select class="form-select form-select-sm" [(ngModel)]="editingStatus[booking.id]">
                                  <option *ngFor="let status of statusOptions" [value]="status">{{ status }}</option>
                                </select>
                                <button class="btn btn-primary btn-sm" type="button" [disabled]="adminActionLoading[booking.id] || editingStatus[booking.id] === booking.status" (click)="updateBookingStatus(booking.id)">
                                  {{ adminActionLoading[booking.id] ? 'Mentés...' : 'Mentés' }}
                                </button>
                              </div>
                            </td>
                            <td>
                              <button class="btn btn-outline-secondary btn-sm" type="button" (click)="showBookingDetails(booking)">Részletek</button>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>

                    <div class="d-flex align-items-center justify-content-between gap-3 flex-column flex-md-row mt-3">
                      <div class="text-muted">{{ filteredAdminBookings().length }} foglalás találat</div>
                      <div class="btn-group" role="group" aria-label="Oldal navigáció">
                        <button class="btn btn-outline-secondary btn-sm" type="button" [disabled]="adminPage() <= 1" (click)="goToPreviousPage()">Előző</button>
                        <button class="btn btn-outline-secondary btn-sm" type="button" [disabled]="adminPage() >= adminPageCount()" (click)="goToNextPage()">Következő</button>
                      </div>
                      <div class="text-muted">{{ adminPage() }} / {{ adminPageCount() }}</div>
                    </div>

                    <div *ngIf="selectedAdminBooking()" class="card border-0 shadow-sm rounded-4 mt-4">
                      <div class="card-body">
                        <div class="d-flex align-items-start justify-content-between gap-3 flex-column flex-md-row mb-3">
                          <div>
                            <h5 class="h5 mb-1">Foglalás részletei</h5>
                            <p class="text-muted mb-0">{{ selectedAdminBooking()?.userFullName }} ({{ selectedAdminBooking()?.userEmail }})</p>
                          </div>
                          <button class="btn btn-outline-secondary btn-sm" type="button" (click)="selectedAdminBooking.set(null)">Bezárás</button>
                        </div>
                        <div class="row g-3">
                          <div class="col-md-4">
                            <strong>Jármű:</strong>
                            <div>{{ selectedAdminBooking()?.vehicleBrand }} {{ selectedAdminBooking()?.vehicleModel }}</div>
                          </div>
                          <div class="col-md-4">
                            <strong>Foglalás időszaka:</strong>
                            <div>{{ selectedAdminBooking()?.startDate | date:'yyyy.MM.dd' }} – {{ selectedAdminBooking()?.endDate | date:'yyyy.MM.dd' }}</div>
                          </div>
                          <div class="col-md-4">
                            <strong>Összeg:</strong>
                            <div>{{ selectedAdminBooking()?.totalPrice | number:'1.0-0' }} Ft</div>
                          </div>
                        </div>
                        <div class="mt-3">
                          <strong>Extras:</strong>
                          <ul class="list-group list-group-flush rounded-4 overflow-hidden mt-2">
                            <li class="list-group-item p-2" *ngFor="let extra of selectedAdminBooking()?.extras">
                              {{ extra.name }} - {{ extra.price | number:'1.0-0' }} Ft
                            </li>
                            <li *ngIf="selectedAdminBooking()?.extras?.length === 0" class="list-group-item p-2 text-muted">Nincs kiválasztott extra szolgáltatás.</li>
                          </ul>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);

  protected stats: DashboardStats | null = null;
  protected loading = false;
  protected error: string | null = null;

  protected adminBookings: AdminBooking[] = [];
  protected adminLoading = false;
  protected adminError: string | null = null;
  protected adminActionLoading: Record<number, boolean> = {};
  protected editingStatus: Record<number, string> = {};
  protected adminStatusFilter = signal<'All' | 'Pending' | 'Confirmed' | 'Cancelled'>('All');
  protected adminSearchTerm = signal('');
  protected adminPage = signal(1);
  protected adminPageSize = signal(10);
  protected selectedAdminBooking = signal<AdminBooking | null>(null);
  protected readonly adminSummary = computed(() => ({
    total: this.adminBookings.length,
    pending: this.adminBookings.filter(b => b.status === 'Pending').length,
    confirmed: this.adminBookings.filter(b => b.status === 'Confirmed').length,
    cancelled: this.adminBookings.filter(b => b.status === 'Cancelled').length
  }));
  protected readonly filteredAdminBookings = computed(() => {
    const term = this.adminSearchTerm().trim().toLowerCase();
    return this.adminBookings.filter((booking) => {
      const matchesStatus = this.adminStatusFilter() === 'All' || booking.status === this.adminStatusFilter();
      const query = `${booking.userFullName} ${booking.userEmail} ${booking.vehicleBrand} ${booking.vehicleModel}`.toLowerCase();
      const matchesSearch = !term || query.includes(term);
      return matchesStatus && matchesSearch;
    });
  });
  protected readonly adminPageCount = computed(() => Math.max(1, Math.ceil(this.filteredAdminBookings().length / this.adminPageSize())));
  protected readonly paginatedAdminBookings = computed(() => {
    const page = this.adminPage();
    const size = this.adminPageSize();
    const all = this.filteredAdminBookings();
    const start = (page - 1) * size;
    return all.slice(start, start + size);
  });
  protected readonly statusOptions = ['Pending', 'Confirmed', 'Cancelled'];

  protected statCards = [
    { title: 'Összbevétel', value: '0 Ft', subtitle: 'Minden foglalás bevétele' },
    { title: 'Regisztrált felhasználók', value: '0', subtitle: 'Összes felhasználó' },
    { title: 'Aktív foglalások', value: '0', subtitle: 'Most aktív időszakban' }
  ];

  ngOnInit(): void {
    this.loadStats();
    this.loadAdminBookings();
  }

  private loadStats(): void {
    this.loading = true;
    this.error = null;

    this.dashboardService.getStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.statCards = [
          { title: 'Összbevétel', value: `${data.totalRevenue.toLocaleString('hu-HU')} Ft`, subtitle: 'Minden foglalás bevétele' },
          { title: 'Regisztrált felhasználók', value: data.registeredUserCount.toString(), subtitle: 'Összes felhasználó' },
          { title: 'Aktív foglalások', value: data.activeBookingCount.toString(), subtitle: 'Most aktív időszakban' }
        ];
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Hiba történt a dashboard statisztikák betöltésekor.';
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  protected loadAdminBookings(): void {
    this.adminLoading = true;
    this.adminError = null;

    this.dashboardService.getAdminBookings().subscribe({
      next: (data) => {
        this.adminBookings = data;
        this.editingStatus = data.reduce((result, booking) => ({
          ...result,
          [booking.id]: booking.status
        }), {} as Record<number, string>);
      },
      error: (err) => {
        this.adminError = err?.error?.message ?? 'Hiba történt az admin foglalások betöltésekor.';
      },
      complete: () => {
        this.adminLoading = false;
      }
    });
  }

  protected updateBookingStatus(bookingId: number): void {
    const status = this.editingStatus[bookingId];
    this.adminActionLoading[bookingId] = true;

    this.dashboardService.updateBookingStatus(bookingId, status).subscribe({
      next: () => {
        this.loadAdminBookings();
      },
      error: (err) => {
        window.alert(err?.error?.message ?? 'Hiba történt a státusz mentésekor.');
        this.adminActionLoading[bookingId] = false;
      },
      complete: () => {
        this.adminActionLoading[bookingId] = false;
      }
    });
  }

  protected goToPreviousPage(): void {
    this.adminPage.update((current) => Math.max(1, current - 1));
  }

  protected goToNextPage(): void {
    this.adminPage.update((current) => Math.min(this.adminPageCount(), current + 1));
  }

  protected showBookingDetails(booking: AdminBooking): void {
    this.selectedAdminBooking.set(booking);
  }
}
