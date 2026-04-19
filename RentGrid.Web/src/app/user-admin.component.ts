import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AdminUserService, AdminUserDto } from './admin-user.service';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-user-admin',
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
                  <h2 class="h4 mb-1">Felhasználók kezelése</h2>
                  <p class="text-muted mb-0">Engedélyezze vagy tiltsa le a felhasználói fiókokat.</p>
                </div>
                <div class="d-flex gap-2 flex-wrap">
                  <a class="btn btn-outline-secondary" routerLink="/dashboard">Vissza az admin panelhez</a>
                  <button class="btn btn-primary" type="button" (click)="loadUsers()">
                    <i class="bi bi-arrow-clockwise"></i> Frissítés
                  </button>
                </div>
              </div>

              <div *ngIf="error()" class="alert alert-danger alert-dismissible fade show" role="alert">
                {{ error() }}
                <button type="button" class="btn-close" (click)="error.set('')"></button>
              </div>

              <div *ngIf="success()" class="alert alert-success alert-dismissible fade show" role="alert">
                {{ success() }}
                <button type="button" class="btn-close" (click)="success.set('')"></button>
              </div>

              <div *ngIf="loading()" class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                  <span class="visually-hidden">Betöltés...</span>
                </div>
              </div>

              <div *ngIf="!loading() && users().length > 0" class="table-responsive">
                <table class="table table-hover mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>ID</th>
                      <th>Név</th>
                      <th>E-mail</th>
                      <th>Szerepkör</th>
                      <th>Állapot</th>
                      <th>Intézkedések</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let user of users()">
                      <td>{{ user.id }}</td>
                      <td>
                        {{ user.name }}
                        <span *ngIf="user.id === currentUserId()" class="badge text-bg-info ms-2">Ön</span>
                      </td>
                      <td>{{ user.email }}</td>
                      <td>
                        <span [ngClass]="{
                          'badge': true,
                          'text-bg-danger': user.role === 'Admin',
                          'text-bg-primary': user.role !== 'Admin'
                        }">
                          {{ user.role === 'Admin' ? 'Admin' : 'Vásárló' }}
                        </span>
                      </td>
                      <td>
                        <span [ngClass]="{
                          'badge': true,
                          'text-bg-success': user.isActive,
                          'text-bg-secondary': !user.isActive
                        }">
                          {{ user.isActive ? 'Aktív' : 'Inaktív' }}
                        </span>
                      </td>
                      <td>
                        <button
                          [ngClass]="{
                            'btn': true,
                            'btn-sm': true,
                            'btn-danger': user.isActive,
                            'btn-success': !user.isActive
                          }"
                          type="button"
                          (click)="toggleUserActive(user)"
                          [disabled]="togglingUserId() === user.id || user.id === currentUserId()"
                        >
                          <span *ngIf="togglingUserId() !== user.id">
                            {{ user.isActive ? 'Letiltás' : 'Engedélyezés' }}
                          </span>
                          <span *ngIf="togglingUserId() === user.id">
                            <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                            Feldolgozás...
                          </span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <div *ngIf="!loading() && users().length === 0" class="alert alert-info">
                Nincsenek felhasználók az adatbázisban.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .table-responsive {
      border-radius: 1rem;
      overflow: hidden;
      box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    }
    .table { margin-bottom: 0; }
    .badge { font-size: 0.75rem; padding: 0.375rem 0.75rem; }
  `]
})
export class UserAdminComponent implements OnInit {
  private readonly adminUserService = inject(AdminUserService);
  private readonly authService = inject(AuthService);

  users = signal<AdminUserDto[]>([]);
  loading = signal(false);
  error = signal('');
  success = signal('');
  togglingUserId = signal<number | null>(null);
  
  // Külön signal az aktuális user ID-nek a könnyebb kezelhetőségért
  currentUserId = signal<number | null>(null);

  ngOnInit(): void {
    // Beállítjuk az aktuális user ID-t az authService-ből
    this.currentUserId.set(this.authService.currentUserId);
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.error.set('');
    this.success.set('');

    this.adminUserService.getAllUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Hiba a felhasználók betöltésekor: ' + (err.error?.message || err.message));
        this.loading.set(false);
      }
    });
  }

  toggleUserActive(user: AdminUserDto): void {
    // Biztonsági ellenőrzés kódból is
    if (user.id === this.currentUserId()) {
      this.error.set('Saját magát nem tilthatja le!');
      return;
    }

    this.togglingUserId.set(user.id);
    this.error.set('');
    this.success.set('');

    this.adminUserService.toggleUserActive(user.id).subscribe({
      next: (updatedUser) => {
        this.users.update(current =>
          current.map(u => u.id === updatedUser.id ? updatedUser : u)
        );
        this.success.set(updatedUser.isActive ? 'Felhasználó engedélyezve.' : 'Felhasználó letiltva.');
        this.togglingUserId.set(null);
      },
      error: (err) => {
        this.error.set('Hiba a státusz módosításakor: ' + (err.error?.message || err.message));
        this.togglingUserId.set(null);
      }
    });
  }
}