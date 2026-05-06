import { Component, inject, OnInit, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { VehicleService, Vehicle, CreateVehicleRequest, UpdateVehiclePriceRequest, UpdateVehicleAvailabilityRequest } from './vehicle.service';

@Component({
  selector: 'app-vehicle-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-lg-12">
          <div class="card shadow-sm rounded-4 border-0">
            <div class="card-body p-5">
              <div class="d-flex align-items-start justify-content-between gap-3 flex-column flex-md-row mb-4">
                <div>
                  <h2 class="h4 mb-1">Járművek kezelése</h2>
                  <p class="text-muted mb-0">Járművek hozzáadása, módosítása és kezelése.</p>
                </div>
                <div class="d-flex gap-2">
                  <button class="btn btn-primary" (click)="showAddForm = !showAddForm">
                    <i class="bi bi-plus-circle"></i> Új jármű hozzáadása
                  </button>
                  <a class="btn btn-outline-secondary" routerLink="/dashboard">Vissza a dashboardra</a>
                </div>
              </div>

              <!-- Add Vehicle Form -->
              <div *ngIf="showAddForm" class="card border-0 shadow-sm rounded-4 mb-4">
                <div class="card-body">
                  <h5 class="h5 mb-3">Új jármű hozzáadása</h5>
                  <form (ngSubmit)="addVehicle()" #addForm="ngForm">
                    <div class="row g-3">
                      <div class="col-md-4">
                        <label for="brand" class="form-label">Márka *</label>
                        <input type="text" class="form-control" id="brand" [(ngModel)]="newVehicle.brand" name="brand" required>
                      </div>
                      <div class="col-md-4">
                        <label for="model" class="form-label">Modell *</label>
                        <input type="text" class="form-control" id="model" [(ngModel)]="newVehicle.model" name="model" required>
                      </div>
                      <div class="col-md-4">
                        <label for="dailyPrice" class="form-label">Napi ár (Ft) *</label>
                        <input type="number" class="form-control" id="dailyPrice" [(ngModel)]="newVehicle.dailyPrice" name="dailyPrice" min="1" required>
                      </div>
                      <div class="col-12">
                        <label for="images" class="form-label">Képek *</label>
                        <input type="file" class="form-control" id="images" (change)="onFileSelected($event)" accept="image/*" multiple required>
                        <div class="form-text">Legalább egy képfájl feltöltése kötelező (JPG, PNG, GIF, stb.)</div>
                      </div>
                      <div class="col-12">
                        <button type="submit" class="btn btn-success me-2" [disabled]="!addForm.form.valid || !selectedFiles || selectedFiles.length === 0">
                          <i class="bi bi-check-circle"></i> Hozzáadás
                        </button>
                        <button type="button" class="btn btn-outline-secondary" (click)="cancelAdd()">
                          <i class="bi bi-x-circle"></i> Mégse
                        </button>
                      </div>
                    </div>
                  </form>
                </div>
              </div>

              <!-- Loading State -->
              <div *ngIf="loading()" class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                  <span class="visually-hidden">Betöltés...</span>
                </div>
              </div>

              <!-- Error State -->
              <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>

              <!-- Vehicles List -->
              <div *ngIf="!loading() && !error()" class="card border-0 shadow-sm rounded-4">
                <div class="card-body">
                  <div class="d-flex align-items-center justify-content-between mb-3 gap-3 flex-column flex-md-row">
                    <div>
                      <h5 class="h5 mb-1">Járművek listája</h5>
                      <p class="text-muted mb-0">Összesen {{ vehicles().length }} jármű</p>
                    </div>
                    <button class="btn btn-outline-secondary btn-sm" (click)="loadVehicles()">
                      <i class="bi bi-arrow-clockwise"></i> Frissítés
                    </button>
                  </div>

                  <div *ngIf="vehicles().length === 0" class="text-center py-4">
                    <p class="text-muted">Még nincs jármű hozzáadva.</p>
                  </div>

                  <div *ngIf="vehicles().length > 0" class="row g-4">
                    <div *ngFor="let vehicle of vehicles()" class="col-lg-6 col-xl-4">
                      <div class="card h-100 border-0 shadow-sm rounded-4">
                        <div class="card-body d-flex flex-column">
                          <!-- Vehicle Images -->
                          <div class="text-center mb-3">
                            <div *ngIf="vehicle.imageFileIds && vehicle.imageFileIds.length > 0" class="position-relative">
                              <img [src]="getVehicleImageUrl(vehicle.imageFileIds[0])"
                                   alt="{{ vehicle.brand }} {{ vehicle.model }}"
                                   class="img-fluid rounded-3"
                                   style="height: 150px; object-fit: cover; width: 100%;">
                              <div *ngIf="vehicle.imageFileIds.length > 1" class="position-absolute top-0 end-0 bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 30px; height: 30px; font-size: 12px;">
                                {{ vehicle.imageFileIds.length }}
                              </div>
                            </div>
                            <div *ngIf="!vehicle.imageFileIds || vehicle.imageFileIds.length === 0"
                                 class="bg-light rounded-3 d-flex align-items-center justify-content-center"
                                 style="height: 150px;">
                              <i class="bi bi-image text-muted" style="font-size: 2rem;"></i>
                            </div>
                          </div>

                          <!-- Vehicle Info -->
                          <h6 class="card-title">{{ vehicle.brand }} {{ vehicle.model }}</h6>
                          <div class="mb-3">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                              <span class="text-muted">Napi ár:</span>
                              <span class="fw-bold">{{ vehicle.dailyPrice | number:'1.0-0' }} Ft</span>
                            </div>
                            <div class="d-flex justify-content-between align-items-center">
                              <span class="text-muted">Elérhető:</span>
                              <span [class]="vehicle.isAvailable ? 'text-success' : 'text-danger'">
                                <i [class]="vehicle.isAvailable ? 'bi bi-check-circle' : 'bi bi-x-circle'"></i>
                                {{ vehicle.isAvailable ? 'Igen' : 'Nem' }}
                              </span>
                            </div>
                          </div>

                          <!-- Action Buttons -->
                          <div class="mt-auto">
                            <div class="d-flex gap-1 flex-wrap mb-2">
                              <button class="btn btn-outline-primary btn-sm"
                                      (click)="editPrice(vehicle)"
                                      data-bs-toggle="modal"
                                      data-bs-target="#editPriceModal">
                                <i class="bi bi-cash"></i> Ár
                              </button>
                              <button class="btn btn-outline-secondary btn-sm"
                                      [class]="vehicle.isAvailable ? 'btn-outline-warning' : 'btn-outline-success'"
                                      (click)="toggleAvailability(vehicle)">
                                <i [class]="vehicle.isAvailable ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                {{ vehicle.isAvailable ? 'Letilt' : 'Engedélyez' }}
                              </button>
                            </div>
                            <div class="d-flex gap-1 flex-wrap">
                              <button class="btn btn-outline-info btn-sm"
                                      (click)="openImageModal(vehicle)">
                                <i class="bi bi-images"></i> Képek ({{ vehicle.imageFileIds.length || 0 }})
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

<!-- Image Management Modal -->
<div class="modal fade" id="imageModal" tabindex="-1" data-bs-container="body">
  <div class="modal-dialog modal-lg">
    <div class="modal-content rounded-4 border-0">
      <div class="modal-header">
        <h5 class="modal-title">Képek kezelése - {{ selectedVehicleForImages?.brand }} {{ selectedVehicleForImages?.model }}</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <div *ngIf="selectedVehicleForImages">
          <!-- Current Images -->
          <div *ngIf="selectedVehicleForImages.imageFileIds && selectedVehicleForImages.imageFileIds.length > 0" class="mb-4">
            <h6>Jelenlegi képek:</h6>
            <div class="row g-3">
              <div *ngFor="let imageId of selectedVehicleForImages.imageFileIds" class="col-md-4">
                <div class="card">
                  <img [src]="getVehicleImageUrl(imageId)" class="card-img-top" alt="Vehicle image" style="height: 150px; object-fit: cover;">
                  <div class="card-body p-2">
                    <button class="btn btn-outline-danger btn-sm w-100" (click)="removeImage(imageId)">
                      <i class="bi bi-trash"></i> Törlés
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Add New Images -->
          <div>
            <h6>Új képek hozzáadása:</h6>
            <div class="mb-3">
              <input type="file" class="form-control" id="newImages" (change)="onNewImagesSelected($event)" accept="image/*" multiple>
              <div class="form-text">Több kép is kiválasztható egyszerre.</div>
            </div>
            <button class="btn btn-primary" (click)="addImages()" [disabled]="!newImages || newImages.length === 0">
              <i class="bi bi-plus-circle"></i> Képek hozzáadása
            </button>
          </div>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Bezárás</button>
      </div>
    </div>
  </div>
</div>
<!-- Edit Price Modal -->
<div class="modal fade" id="editPriceModal" tabindex="-1">
  <div class="modal-dialog">
    <div class="modal-content rounded-4 border-0">
      <div class="modal-header">
        <h5 class="modal-title">Ár módosítása</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <div *ngIf="editingVehicle">
          <p class="mb-3">
            <strong>{{ editingVehicle.brand }} {{ editingVehicle.model }}</strong>
          </p>
          <div class="mb-3">
            <label for="newPrice" class="form-label">Új napi ár (Ft)</label>
            <input type="number" class="form-control" id="newPrice" [(ngModel)]="newPrice" min="1" required>
          </div>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Mégse</button>
        <button type="button" class="btn btn-primary" (click)="updatePrice()" data-bs-dismiss="modal" [disabled]="!newPrice || newPrice <= 0">
          <i class="bi bi-check-circle"></i> Mentés
        </button>
      </div>
    </div>
  </div>
</div>
  `,
  styles: [`
    .card {
      transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
    }
    .card:hover {
      
      box-shadow: 0 4px 12px rgba(0,0,0,0.1) !important;
    }
      :host {
    display: block;
    position: static !important;
    transform: none !important;
}
  `]
})
export class VehicleAdminComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private readonly cdr = inject(ChangeDetectorRef);
  vehicles = signal<Vehicle[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  showAddForm = false;
  newVehicle: CreateVehicleRequest = { brand: '', model: '', dailyPrice: 0 };
  selectedFiles: File[] | null = null;

  editingVehicle: Vehicle | null = null;
  newPrice = 0;

  selectedVehicleForImages: Vehicle | null = null;
  newImages: File[] | null = null;

  ngOnInit() {
    this.loadVehicles();
  }

  loadVehicles() {
    this.loading.set(true);
    this.error.set(null);

    this.vehicleService.getAllVehicles().pipe(
      finalize(() =>{ 
        this.loading.set(false)
        this.cdr.detectChanges();
        console.log('Vehicles loaded successfully', this.loading.toString);
      })
    ).subscribe({
      next: (vehicles) => {
        this.vehicles.set(vehicles);
        console.log('Loaded vehicles:', vehicles);
      },
      error: (err) => {
        this.error.set('Hiba történt a járművek betöltésekor.');
        console.error('Error loading vehicles:', err);
      }
    });
  }

  onFileSelected(event: any) {
    const files = event.target.files;
    if (files && files.length > 0) {
      this.selectedFiles = Array.from(files) as File[];
    } else {
      this.selectedFiles = null;
    }
  }

  addVehicle() {
    if (!this.selectedFiles || this.selectedFiles.length === 0) return;

    this.loading.set(true);
    this.vehicleService.createVehicle(this.newVehicle, this.selectedFiles).pipe(
      finalize(() => {
        this.loading.set(false)
        this.cdr.detectChanges();
        console.log('Vehicle added successfully');
      })
    ).subscribe({
      next: () => {
        this.showAddForm = false;
        this.newVehicle = { brand: '', model: '', dailyPrice: 0 };
        this.selectedFiles = null;
        this.loadVehicles();
      },
      error: (err) => {
        this.error.set('Hiba történt a jármű hozzáadásakor.');
        console.error('Error adding vehicle:', err);
      }
    });
  }

  cancelAdd() {
    this.showAddForm = false;
    this.newVehicle = { brand: '', model: '', dailyPrice: 0 };
    this.selectedFiles = null;
  }

  editPrice(vehicle: Vehicle) {
    this.editingVehicle = vehicle;
    this.newPrice = vehicle.dailyPrice;
  }

  updatePrice() {
    if (!this.editingVehicle || !this.newPrice || this.newPrice <= 0) return;

    this.vehicleService.updateVehiclePrice(this.editingVehicle.id, { dailyPrice: this.newPrice }).subscribe({
      next: () => {
        this.loadVehicles();
        this.editingVehicle = null;
        this.newPrice = 0;
      },
      error: (err) => {
        this.error.set('Hiba történt az ár módosításakor.');
        console.error('Error updating price:', err);
      }
    });
  }

  toggleAvailability(vehicle: Vehicle) {
    const newAvailability = !vehicle.isAvailable;
    this.vehicleService.updateVehicleAvailability(vehicle.id, { isAvailable: newAvailability }).subscribe({
      next: () => {
        this.loadVehicles();
      },
      error: (err) => {
        this.error.set('Hiba történt az elérhetőség módosításakor.');
        console.error('Error updating availability:', err);
      }
    });
  }

  getVehicleImageUrl(imageId: string): string {
    return this.vehicleService.getVehicleImage(imageId);
  }

  openImageModal(vehicle: Vehicle) {
    this.selectedVehicleForImages = vehicle;
    this.newImages = null;
    // Trigger modal manually since data-bs-toggle might not work
    const modal = document.getElementById('imageModal');
    if (modal) {
      const bsModal = new (window as any).bootstrap.Modal(modal);
      bsModal.show();
    }
  }

  onNewImagesSelected(event: any) {
    const files = event.target.files;
    if (files) {
      this.newImages = Array.from(files) as File[];
    }
  }

  addImages() {
    if (!this.selectedVehicleForImages || !this.newImages || this.newImages.length === 0) return;

    this.vehicleService.addVehicleImages(this.selectedVehicleForImages.id, this.newImages).subscribe({
      next: () => {
        this.loadVehicles();
        this.newImages = null;
        // Clear file input
        const input = document.getElementById('newImages') as HTMLInputElement;
        if (input) input.value = '';
      },
      error: (err) => {
        this.error.set('Hiba történt a képek hozzáadásakor.');
        console.error('Error adding images:', err);
      }
    });
  }

  removeImage(imageId: string) {
    if (!this.selectedVehicleForImages) return;

    if (confirm('Biztosan törli ezt a képet?')) {
      this.vehicleService.removeVehicleImage(this.selectedVehicleForImages.id, imageId).subscribe({
        next: () => {
          this.loadVehicles();
        },
        error: (err) => {
          this.error.set('Hiba történt a kép törlésekor.');
          console.error('Error removing image:', err);
        }
      });
    }
  }
}