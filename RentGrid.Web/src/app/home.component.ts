import { Component, inject, OnInit, PLATFORM_ID, signal, computed } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from './auth.service';
import { BookingService, CreateBookingRequest } from './booking.service';
import { ExtraOption, ExtraService } from './extra.service';
import { VehicleService, Vehicle } from './vehicle.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="text-center mb-4">
        <h1 class="display-5">RentGrid</h1>
        <p class="lead mt-3">
          Üdvözöljük a RentGrid járműparkban. Válassza ki a következő autóját!
        </p>
      </div>

      @if (loading()) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Betöltés...</span>
          </div>
        </div>
      } @else if (error()) {
        <div class="alert alert-danger">{{ error() }}</div>
      } @else {
        <div class="row g-4">
          @for (vehicle of vehicles(); track vehicle.id) {
            <div class="col-12 col-sm-6 col-lg-4 col-xl-3">
              <div class="card h-100 shadow-sm">
                @if (vehicle.imageFileIds && vehicle.imageFileIds.length > 0) {
                  <img
                    [src]="'/api/vehicle/image/' + vehicle.imageFileIds[0]"
                    class="card-img-top"
                    alt="{{ vehicle.brand }} {{ vehicle.model }}"
                  />
                } @else {
                  <div class="bg-light d-flex align-items-center justify-content-center" style="height: 225px;">
                    <span class="text-muted">Nincs kép</span>
                  </div>
                }

                <div class="card-body d-flex flex-column">
                  <h5 class="card-title">{{ vehicle.brand }}</h5>
                  <p class="card-text mb-1">{{ vehicle.model }}</p>
                  <p class="card-text text-muted mb-3">Napi ár: {{ vehicle.dailyPrice }} Ft</p>

                  @if (isLoggedIn()) {
                    <button
                      type="button"
                      class="btn btn-outline-primary mt-auto"
                      (click)="showDetails(vehicle.id)"
                    >
                      Részletek
                    </button>
                  } @else {
                    <div class="mt-auto text-muted small">Jelentkezzen be a foglaláshoz.</div>
                  }
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>

    @if (selectedVehicle()) {
      <div class="modal fade show d-block" tabindex="-1" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-lg modal-dialog-centered" role="document">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Foglalás: {{ selectedVehicle()?.brand }} {{ selectedVehicle()?.model }}</h5>
              <button type="button" class="btn-close" aria-label="Bezárás" (click)="closeModal()"></button>
            </div>
            <div class="modal-body">
              @if (selectedVehicleImageIds().length > 0) {
                <div class="mb-4">
                  <img
                    [src]="selectedGalleryImageUrl()"
                    class="img-fluid rounded w-100 mb-3"
                    alt="{{ selectedVehicle()?.brand }} {{ selectedVehicle()?.model }}"
                  />
                  <div class="d-flex flex-wrap gap-2">
                    @for (imageId of selectedVehicleImageIds(); track imageId) {
                      <button
                        type="button"
                        class="btn p-0 border rounded overflow-hidden"
                        [class.border-primary]="selectedGalleryImageId() === imageId"
                        (click)="changeGalleryImage(imageId)"
                        style="width: 100px; height: 70px;"
                      >
                        <img
                          [src]="vehicleService.getVehicleImage(imageId)"
                          class="h-100 w-100"
                          style="object-fit: cover;"
                          alt="Kép"
                        />
                      </button>
                    }
                  </div>
                </div>
              } @else {
                <div class="mb-4 bg-light border rounded-3 p-4 text-center">
                  <p class="mb-0">Nincs kép ehhez a járműhöz.</p>
                </div>
              }

              <div class="row gy-3">
                <div class="col-md-6">
                  <strong>Jármű:</strong>
                  <p class="mb-1">{{ selectedVehicle()?.brand }} {{ selectedVehicle()?.model }}</p>
                  <p class="mb-1">Napi ár: {{ selectedVehicle()?.dailyPrice }} Ft</p>
                </div>
                <div class="col-md-6">
                  <label class="form-label">Kezdet</label>
                  <input
                    type="date"
                    class="form-control"
                    [ngModel]="bookingStart()"
                    (ngModelChange)="bookingStart.set($event)"
                  />
                </div>
                <div class="col-md-6">
                  <label class="form-label">Vége</label>
                  <input
                    type="date"
                    class="form-control"
                    [ngModel]="bookingEnd()"
                    (ngModelChange)="bookingEnd.set($event)"
                  />
                </div>
              </div>

              <div class="mt-4">
                <h6>Extrák</h6>
                <div class="row g-2">
                  @for (extra of extras(); track extra.id) {
                    <div class="col-12 col-md-6">
                      <div class="form-check border rounded-2 p-3">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          [id]="'extra-' + extra.id"
                          [value]="extra.id"
                          [checked]="selectedExtrasMap()[extra.id]"
                          (change)="toggleExtra(extra.id, $any($event.target).checked)"
                        />
                        <label class="form-check-label" [for]="'extra-' + extra.id">
                          {{ extra.name }} (+{{ extra.price }} Ft)
                        </label>
                      </div>
                    </div>
                  }
                </div>
              </div>

              <div class="mt-4">
                <h5>Végösszeg: {{ bookingTotal() }} Ft</h5>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" (click)="closeModal()">Mégse</button>
              <button type="button" class="btn btn-primary" (click)="confirmBooking()">Foglalás megerősítése</button>
            </div>
          </div>
        </div>
      </div>
      <div class="modal-backdrop fade show"></div>
    }
  `
})
export class HomeComponent implements OnInit {
  protected readonly vehicleService = inject(VehicleService);
  private readonly authService = inject(AuthService);
  private readonly extraService = inject(ExtraService);
  private readonly bookingService = inject(BookingService);
  private readonly platformId = inject(PLATFORM_ID);
  protected readonly isBrowser = isPlatformBrowser(this.platformId);

  protected readonly vehicles = signal<Vehicle[]>([]);
  protected readonly extras = signal<ExtraOption[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly isLoggedIn = signal(false);
  protected readonly selectedVehicle = signal<Vehicle | null>(null);
  protected readonly selectedGalleryImageId = signal<string | null>(null);
  protected readonly selectedVehicleImageIds = computed(() => this.selectedVehicle()?.imageFileIds ?? []);
  protected readonly selectedGalleryImageUrl = computed(() => {
    const imageId = this.selectedGalleryImageId() || this.selectedVehicleImageIds()[0];
    return imageId ? this.vehicleService.getVehicleImage(imageId) : '';
  });

  protected readonly bookingStart = signal('');
  protected readonly bookingEnd = signal('');
  protected readonly selectedExtrasMap = signal<Record<number, boolean>>({});

  protected readonly bookingTotal = computed(() => {
    const vehicle = this.selectedVehicle();
    if (!vehicle || !this.bookingStart() || !this.bookingEnd()) {
      return 0;
    }

    const start = new Date(this.bookingStart());
    const end = new Date(this.bookingEnd());
    if (isNaN(start.getTime()) || isNaN(end.getTime()) || end <= start) {
      return 0;
    }

    const days = Math.max(1, Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)));
    const selectedExtras = this.extras().filter(extra => this.selectedExtrasMap()[extra.id]);
    const extrasTotal = selectedExtras.reduce((sum, extra) => sum + extra.price, 0);

    return vehicle.dailyPrice * days + extrasTotal;
  });

  ngOnInit(): void {
    if (this.isBrowser) {
      this.loadVehicles();
      this.loadExtras();
      this.authService.currentUser$.subscribe((token) => this.isLoggedIn.set(!!token));
    } else {
      this.loading.set(false);
    }
  }

  private loadVehicles(): void {
    this.loading.set(true);
    this.error.set(null);

    this.vehicleService.getAllVehicles().subscribe({
      next: (data) => this.vehicles.set(data),
      error: () => this.error.set('Hiba történt a járművek betöltésekor.'),
      complete: () => this.loading.set(false)
    });
  }

  private loadExtras(): void {
    this.extraService.getExtras().subscribe({
      next: (items) => this.extras.set(items),
      error: () => {
        this.error.set('Hiba történt az extrák betöltésekor.');
      }
    });
  }

  protected showDetails(id: number): void {
    const vehicle = this.vehicles().find((item) => item.id === id);
    if (!vehicle) {
      return;
    }

    this.selectedVehicle.set(vehicle);
    this.selectedGalleryImageId.set(vehicle.imageFileIds?.[0] || null);
    this.bookingStart.set('');
    this.bookingEnd.set('');
    this.selectedExtrasMap.set({});
  }

  protected closeModal(): void {
    this.selectedVehicle.set(null);
    this.selectedGalleryImageId.set(null);
  }

  protected changeGalleryImage(imageId: string): void {
    this.selectedGalleryImageId.set(imageId);
  }

  protected toggleExtra(extraId: number, checked: boolean): void {
    this.selectedExtrasMap.set({
      ...this.selectedExtrasMap(),
      [extraId]: checked
    });
  }

  protected confirmBooking(): void {
    const vehicle = this.selectedVehicle();
    if (!vehicle) {
      return;
    }

    if (!this.bookingStart() || !this.bookingEnd()) {
      window.alert('Kérjük, adja meg a foglalás kezdő és végdátumát.');
      return;
    }

    const start = new Date(this.bookingStart());
    const end = new Date(this.bookingEnd());
    if (isNaN(start.getTime()) || isNaN(end.getTime()) || end <= start) {
      window.alert('A foglalási időszaknak érvényesnek kell lennie, és a végdátumnak később kell lennie.');
      return;
    }

    const selectedExtraIds = this.extras()
      .filter((extra) => this.selectedExtrasMap()[extra.id])
      .map((extra) => extra.id);

    const bookingPayload: CreateBookingRequest = {
      vehicleId: vehicle.id,
      startDate: this.bookingStart(),
      endDate: this.bookingEnd(),
      extraServiceIds: selectedExtraIds
    };

    this.bookingService.createBooking(bookingPayload).subscribe({
      next: () => {
        this.closeModal();
        window.alert('Foglalás sikeresen létrehozva.');
      },
      error: (err) => {
        window.alert(this.getBookingErrorMessage(err));
      }
    });
  }

  private getBookingErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (typeof error.error === 'string' && error.error.trim()) {
        return error.error;
      }

      if (typeof error.error === 'object' && error.error !== null) {
        return (
          (error.error as { detail?: string; title?: string }).detail ||
          (error.error as { detail?: string; title?: string }).title ||
          error.message
        );
      }

      return error.message || 'Hiba történt a foglalás létrehozásakor.';
    }

    return 'Hiba történt a foglalás létrehozásakor.';
  }
}
