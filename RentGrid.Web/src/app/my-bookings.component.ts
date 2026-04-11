import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BookingService, MyBooking } from './booking.service';

type BookingStatus = 'All' | 'Pending' | 'Confirmed' | 'Cancelled';

@Component({
  selector: 'app-my-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-lg-10">
          <div class="card shadow-sm rounded-4 border-0">
            <div class="card-body p-5">
              <div class="d-flex align-items-start justify-content-between mb-4 gap-3 flex-column flex-md-row">
                <div>
                  <h2 class="h4 mb-1">Saját foglalások</h2>
                  <p class="text-muted mb-0">Tekintse meg a saját járműfoglalásait és azok részleteit.</p>
                </div>
                <a class="btn btn-outline-primary" routerLink="/">Vissza a kezdőlapra</a>
              </div>

              <div class="row gy-3 mb-4">
                <div class="col-md-4">
                  <div class="bg-light rounded-4 p-4 h-100">
                    <p class="text-uppercase text-muted mb-2">Foglalások</p>
                    <p class="h3 mb-1">{{ totalBookings() }}</p>
                    <p class="text-muted mb-0">Összesen</p>
                  </div>
                </div>
                <div class="col-md-4">
                  <div class="bg-light rounded-4 p-4 h-100">
                    <p class="text-uppercase text-muted mb-2">Aktív foglalások</p>
                    <p class="h3 mb-1">{{ activeBookings() }}</p>
                    <p class="text-muted mb-0">Pending vagy Confirmed</p>
                  </div>
                </div>
                <div class="col-md-4">
                  <div class="bg-light rounded-4 p-4 h-100">
                    <p class="text-uppercase text-muted mb-2">Elköltött összeg</p>
                    <p class="h3 mb-1">{{ totalSpent() | number:'1.0-0' }} Ft</p>
                    <p class="text-muted mb-0">Összes foglalás alapján</p>
                  </div>
                </div>
              </div>

              <div class="row align-items-center mb-4">
                <div class="col-md-6 mb-3 mb-md-0">
                  <label class="form-label">Státusz szűrő</label>
                  <select class="form-select" [ngModel]="selectedStatus()" (ngModelChange)="selectedStatus.set($event)">
                    <option *ngFor="let status of statusOptions" [value]="status">{{ status }}</option>
                  </select>
                </div>
                <div class="col-md-6 text-md-end">
                  <button class="btn btn-outline-secondary" type="button" (click)="loadMyBookings()">Frissítés</button>
                </div>
              </div>

              <div *ngIf="loading()" class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                  <span class="visually-hidden">Betöltés...</span>
                </div>
              </div>

              <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>

              <div *ngIf="!loading() && !error()">
                <div *ngIf="filteredBookings().length === 0" class="alert alert-info">
                  Nincs megjeleníthető foglalás a kiválasztott szűréshez.
                </div>

                <div class="row g-4" *ngIf="filteredBookings().length > 0">
                  <div class="col-12" *ngFor="let booking of filteredBookings(); trackBy: trackByBooking">
                    <div class="card shadow-sm border-0 rounded-4">
                      <div class="card-body">
                        <div class="row gy-3 align-items-center">
                          <div class="col-md-3">
                            <div class="d-flex flex-column align-items-start gap-2">
                              <div class="fw-semibold">{{ booking.vehicleBrand }}</div>
                              <div class="text-muted">{{ booking.vehicleModel }}</div>
                              <div class="text-muted">{{ booking.vehicleDailyPrice }} Ft/nap</div>
                            </div>
                          </div>

                          <div class="col-md-6">
                            <p class="mb-1"><strong>Időszak:</strong> {{ booking.startDate | date:'yyyy.MM.dd' }} – {{ booking.endDate | date:'yyyy.MM.dd' }}</p>
                            <p class="mb-1"><strong>Összeg:</strong> {{ booking.totalPrice | number:'1.0-0' }} Ft</p>
                            <p class="mb-0"><strong>Státusz:</strong>
                              <span class="badge"
                                [class.bg-warning]="booking.status === 'Pending'"
                                [class.bg-success]="booking.status === 'Confirmed'"
                                [class.bg-danger]="booking.status === 'Cancelled'"
                              >
                                {{ booking.status }}
                              </span>
                            </p>
                          </div>

                          <div class="col-md-3 text-md-end">
                            <p class="mb-1 text-muted">Foglalás #{{ booking.id }}</p>
                            <p class="mb-0 text-muted">Extrák: {{ booking.extras.length }}</p>
                          </div>
                        </div>

                        <div class="row mt-4">
                          <div class="col-md-4">
                            <h6 class="mb-2">Extrák</h6>
                            <div *ngIf="booking.extras.length === 0" class="text-muted">Nincsenek kiválasztott extrák.</div>
                            <ul class="list-group list-group-flush" *ngIf="booking.extras.length > 0">
                              <li class="list-group-item px-0" *ngFor="let extra of booking.extras">
                                {{ extra.name }} (+{{ extra.price }} Ft)
                              </li>
                            </ul>
                          </div>
                          <div class="col-md-4">
                            <h6 class="mb-2">Jármű információ</h6>
                            <p class="mb-1"><strong>Jármű azonosító:</strong> {{ booking.vehicleId }}</p>
                            <p class="mb-0"><strong>Napok száma:</strong> {{ getDayCount(booking.startDate, booking.endDate) }}</p>
                          </div>
                          <div class="col-md-4 text-md-end">
                            <div class="d-flex flex-column align-items-end gap-2">
                              <button
                                class="btn btn-outline-danger btn-sm"
                                type="button"
                                [disabled]="actionLoading()[booking.id]"
                                *ngIf="booking.status !== 'Cancelled'"
                                (click)="cancelBooking(booking.id)"
                              >
                                {{ actionLoading()[booking.id] ? 'Lemondás...' : 'Foglalás lemondása' }}
                              </button>
                              <button
                                class="btn btn-outline-secondary btn-sm"
                                type="button"
                                [disabled]="actionLoading()[booking.id]"
                                *ngIf="booking.status === 'Cancelled'"
                                (click)="deleteBooking(booking.id)"
                              >
                                {{ actionLoading()[booking.id] ? 'Törlés...' : 'Foglalás törlése' }}
                              </button>
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
      </div>
    </div>
  `
})
export class MyBookingsComponent implements OnInit {
  private readonly bookingService = inject(BookingService);

  protected readonly statusOptions = ['All', 'Pending', 'Confirmed', 'Cancelled'] as const;
  protected readonly selectedStatus = signal<BookingStatus>('All');
  protected readonly bookings = signal<MyBooking[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly filteredBookings = computed(() => {
    const status = this.selectedStatus();
    const bookings = this.bookings();
    return status === 'All' ? bookings : bookings.filter(b => b.status === status);
  });

  protected readonly totalBookings = computed(() => this.bookings().length);
  protected readonly activeBookings = computed(() => this.bookings().filter(b => b.status === 'Pending' || b.status === 'Confirmed').length);
  protected readonly totalSpent = computed(() => this.bookings().reduce((sum, booking) => sum + booking.totalPrice, 0));
  protected readonly actionLoading = signal<Record<number, boolean>>({});

  ngOnInit(): void {
    this.loadMyBookings();
  }

  protected loadMyBookings(): void {
    this.loading.set(true);
    this.error.set(null);

    this.bookingService.getMyBookings().subscribe({
      next: (data) => {
        this.bookings.set(data);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Hiba történt a foglalások betöltésekor.');
      },
      complete: () => {
        this.loading.set(false);
      }
    });
  }

  protected trackByBooking(index: number, item: MyBooking): number {
    return item.id;
  }

  protected cancelBooking(bookingId: number): void {
    this.setActionLoading(bookingId, true);
    this.bookingService.cancelBooking(bookingId).subscribe({
      next: () => {
        this.loadMyBookings();
      },
      error: (err) => {
        window.alert(err?.error?.message ?? 'Hiba történt a foglalás lemondásakor.');
      },
      complete: () => {
        this.setActionLoading(bookingId, false);
      }
    });
  }

  protected deleteBooking(bookingId: number): void {
    if (!window.confirm('Biztosan törli a foglalást? Ez a művelet végleges.')) {
      return;
    }

    this.setActionLoading(bookingId, true);
    this.bookingService.deleteBooking(bookingId).subscribe({
      next: () => {
        this.loadMyBookings();
      },
      error: (err) => {
        window.alert(err?.error?.message ?? 'Hiba történt a foglalás törlésekor.');
      },
      complete: () => {
        this.setActionLoading(bookingId, false);
      }
    });
  }

  private setActionLoading(bookingId: number, loading: boolean): void {
    this.actionLoading.set({
      ...this.actionLoading(),
      [bookingId]: loading
    });
  }

  protected getDayCount(start: string, end: string): number {
    const startDate = new Date(start);
    const endDate = new Date(end);
    const diff = Math.ceil((endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24));
    return Math.max(0, diff);
  }
}
