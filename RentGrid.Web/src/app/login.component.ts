import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService, LoginRequest } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-md-6 col-lg-5">
          <div class="card border-0 shadow-lg rounded-4">
            <div class="card-body p-5">
              <h2 class="h3 mb-4 text-center">Bejelentkezés</h2>

              <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
                <div class="mb-3">
                  <label for="email" class="form-label">Email</label>
                  <input
                    id="email"
                    type="email"
                    class="form-control"
                    [class.is-invalid]="loginForm.controls.email.invalid && loginForm.controls.email.touched"
                    formControlName="email"
                  />
                  <div class="invalid-feedback" *ngIf="loginForm.controls.email.invalid && loginForm.controls.email.touched">
                    Kérjük, adjon meg érvényes email címet.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="password" class="form-label">Jelszó</label>
                  <input
                    id="password"
                    type="password"
                    class="form-control"
                    [class.is-invalid]="loginForm.controls.password.invalid && loginForm.controls.password.touched"
                    formControlName="password"
                  />
                  <div class="invalid-feedback" *ngIf="loginForm.controls.password.invalid && loginForm.controls.password.touched">
                    A jelszó legalább 6 karakter hosszú kell legyen.
                  </div>
                </div>

                <div *ngIf="error" class="alert alert-danger">{{ error }}</div>

                <button
                  class="btn btn-primary w-100 mb-3"
                  type="submit"
                  [disabled]="loginForm.invalid || loading"
                >
                  {{ loading ? 'Bejelentkezés...' : 'Bejelentkezés' }}
                </button>
              </form>

              <div class="text-center">
                <span class="text-muted">Még nincs fiókja?</span>
                <a routerLink="/register">Regisztráció</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  protected readonly authService = inject(AuthService);
  protected readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  protected loading = false;
  protected error: string | null = null;

  protected onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = null;

    const payload: LoginRequest = this.loginForm.value as LoginRequest;

    this.authService.login(payload).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err) => {
        this.error = err?.error?.message ?? 'Bejelentkezés közben hiba történt.';
        this.loading = false;
      }
    });
  }
}
