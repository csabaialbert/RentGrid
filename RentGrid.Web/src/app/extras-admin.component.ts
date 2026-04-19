import { Component, computed, inject, OnInit, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { ExtraOption, ExtraService } from './extra.service';

@Component({
  selector: 'app-extras-admin',
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
                  <h2 class="h4 mb-1">Extrák kezelése</h2>
                  <p class="text-muted mb-0">Új extrák felvétele, meglévők szerkesztése és inaktiválása.</p>
                </div>
                <div class="d-flex gap-2 flex-wrap">
                  <button class="btn btn-outline-secondary" (click)="loadExtras()">
                    <i class="bi bi-arrow-clockwise"></i> Frissítés
                  </button>
                  <a class="btn btn-outline-secondary" routerLink="/dashboard">Vissza a dashboardra</a>
                </div>
              </div>

              <div *ngIf="error()" class="alert alert-danger">{{ error() }}</div>
              <div *ngIf="loading()" class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                  <span class="visually-hidden">Betöltés...</span>
                </div>
              </div>

              <div class="card border-0 shadow-sm rounded-4 mb-4">
                <div class="card-body">
                  <h5 class="h5 mb-3">Új extra hozzáadása</h5>
                  <div class="row g-3">
                    <div class="col-md-5">
                      <label class="form-label">Név</label>
                      <input type="text" class="form-control" [(ngModel)]="newExtraName" placeholder="Pl. GPS" />
                    </div>
                    <div class="col-md-4">
                      <label class="form-label">Ár (Ft)</label>
                      <input type="number" class="form-control" [(ngModel)]="newExtraPrice" min="1" placeholder="1500" />
                    </div>
                    <div class="col-md-3 d-flex align-items-end">
                      <button class="btn btn-success w-100" (click)="createExtra()" [disabled]="!canCreateExtra()">
                        <i class="bi bi-plus-circle"></i> Hozzáadás
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <div class="card border-0 shadow-sm rounded-4">
                <div class="card-body">
                  <div class="d-flex align-items-center justify-content-between mb-3 flex-column flex-md-row gap-3">
                    <div>
                      <h5 class="h5 mb-1">Extrák listája</h5>
                      <p class="text-muted mb-0">Összesen {{ extras().length }} extra</p>
                    </div>
                  </div>

                  <div *ngIf="extras().length === 0" class="text-center py-4">
                    <p class="text-muted mb-0">Még nincs extra rögzítve.</p>
                  </div>

                  <div *ngIf="extras().length > 0" class="table-responsive">
                    <table class="table table-hover align-middle">
                      <thead>
                        <tr>
                          <th>Név</th>
                          <th>Ár (Ft)</th>
                          <th>Státusz</th>
                          <th class="text-end">Műveletek</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr *ngFor="let extra of extras(); trackBy: trackById">
                          <td *ngIf="editingExtraId() !== extra.id">{{ extra.name }}</td>
                          <td *ngIf="editingExtraId() === extra.id">
                            <input type="text" class="form-control" [(ngModel)]="editingName" />
                          </td>
                          <td *ngIf="editingExtraId() !== extra.id">{{ extra.price | number:'1.0-0' }}</td>
                          <td *ngIf="editingExtraId() === extra.id">
                            <input type="number" class="form-control" min="1" [(ngModel)]="editingPrice" />
                          </td>
                          <td>
                            <span [class]="extra.isActive ? 'badge bg-success' : 'badge bg-secondary'">
                              {{ extra.isActive ? 'Aktív' : 'Inaktív' }}
                            </span>
                          </td>
                          <td class="text-end">
                            <div class="d-flex gap-2 flex-wrap justify-content-end">
                              <button *ngIf="editingExtraId() !== extra.id" class="btn btn-outline-primary btn-sm" type="button" (click)="startEdit(extra)">
                                <i class="bi bi-pencil"></i> Szerkeszt
                              </button>
                              <button *ngIf="editingExtraId() === extra.id" class="btn btn-success btn-sm" type="button" (click)="saveEdit()" [disabled]="!canSaveEdit()">
                                <i class="bi bi-check-lg"></i> Mentés
                              </button>
                              <button *ngIf="editingExtraId() === extra.id" class="btn btn-outline-secondary btn-sm" type="button" (click)="cancelEdit()">
                                <i class="bi bi-x-lg"></i> Mégse
                              </button>
                              <button class="btn btn-outline-{{ extra.isActive ? 'warning' : 'success' }} btn-sm" type="button" (click)="toggleActive(extra)">
                                <i class="bi bi-{{ extra.isActive ? 'slash-circle' : 'check-circle' }}"></i>
                                {{ extra.isActive ? 'Inaktiválás' : 'Aktiválás' }}
                              </button>
                            </div>
                          </td>
                        </tr>
                      </tbody>
                    </table>
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
export class ExtrasAdminComponent implements OnInit {
  private readonly extraService = inject(ExtraService);
  private readonly cdr = inject(ChangeDetectorRef);

  extras = signal<ExtraOption[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  newExtraName = '';
  newExtraPrice = 0;

  editingExtraId = signal<number | null>(null);
  editingName = signal('');
  editingPrice = signal(0);

  protected readonly canCreateExtra = computed(() => {
    return this.newExtraName.trim().length > 0 && this.newExtraPrice > 0;
  });

  protected readonly canSaveEdit = computed(() => {
    return this.editingExtraId() !== null && this.editingName().trim().length > 0 && this.editingPrice() > 0;
  });

  ngOnInit(): void {
    this.loadExtras();
  }

  loadExtras(): void {
    this.loading.set(true);
    this.error.set(null);

    this.extraService.getExtras(true).pipe(
      finalize(() => {
        this.loading.set(false);
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (items) => this.extras.set(items),
      error: (err) => {
        this.error.set('Hiba történt az extrák betöltésekor.');
        console.error(err);
      }
    });
  }

  startEdit(extra: ExtraOption): void {
    this.editingExtraId.set(extra.id);
    this.editingName.set(extra.name);
    this.editingPrice.set(extra.price);
  }

  cancelEdit(): void {
    this.editingExtraId.set(null);
    this.editingName.set('');
    this.editingPrice.set(0);
  }

  saveEdit(): void {
    const extraId = this.editingExtraId();
    if (extraId === null || !this.canSaveEdit()) {
      return;
    }

    this.loading.set(true);
    this.extraService.updateExtra(extraId, {
      name: this.editingName().trim(),
      price: this.editingPrice()
    }).pipe(
      finalize(() => {
        this.loading.set(false);
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: () => {
        this.cancelEdit();
        this.loadExtras();
      },
      error: (err) => {
        this.error.set('Hiba történt az extra mentésekor.');
        console.error(err);
      }
    });
  }

  createExtra(): void {
    if (!this.canCreateExtra()) {
      return;
    }

    this.loading.set(true);
    this.extraService.createExtra({
      name: this.newExtraName.trim(),
      price: this.newExtraPrice
    }).pipe(
      finalize(() => {
        this.loading.set(false);
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: () => {
        this.newExtraName = '';
        this.newExtraPrice = 0;
        this.loadExtras();
      },
      error: (err) => {
        this.error.set('Hiba történt az extra hozzáadásakor.');
        console.error(err);
      }
    });
  }

  toggleActive(extra: ExtraOption): void {
    const newState = !extra.isActive;
    this.loading.set(true);
    this.extraService.setExtraActive(extra.id, newState).pipe(
      finalize(() => {
        this.loading.set(false);
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: () => this.loadExtras(),
      error: (err) => {
        this.error.set('Hiba történt az extra állapotának módosításakor.');
        console.error(err);
      }
    });
  }

  trackById(_: number, item: ExtraOption): number {
    return item.id;
  }
}
