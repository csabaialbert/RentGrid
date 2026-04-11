import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService, RegisterRequest } from './auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-md-6 col-lg-5">
          <div class="card border-0 shadow-lg rounded-4">
            <div class="card-body p-5">
              <h2 class="h3 mb-4 text-center">Regisztráció</h2>

              <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
                <div class="mb-3">
                  <label for="fullName" class="form-label">Teljes név</label>
                  <input
                    id="fullName"
                    class="form-control"
                    [class.is-invalid]="registerForm.controls.fullName.invalid && registerForm.controls.fullName.touched"
                    formControlName="fullName"
                  />
                  <div class="invalid-feedback" *ngIf="registerForm.controls.fullName.invalid && registerForm.controls.fullName.touched">
                    A név megadása kötelező.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="email" class="form-label">Email</label>
                  <input
                    id="email"
                    type="email"
                    class="form-control"
                    [class.is-invalid]="registerForm.controls.email.invalid && registerForm.controls.email.touched"
                    formControlName="email"
                  />
                  <div class="invalid-feedback" *ngIf="registerForm.controls.email.invalid && registerForm.controls.email.touched">
                    Kérjük, adjon meg érvényes email címet.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="password" class="form-label">Jelszó</label>
                  <input
                    id="password"
                    type="password"
                    class="form-control"
                    [class.is-invalid]="registerForm.controls.password.invalid && registerForm.controls.password.touched"
                    formControlName="password"
                  />
                  <div class="invalid-feedback" *ngIf="registerForm.controls.password.invalid && registerForm.controls.password.touched">
                    A jelszó legalább 6 karakter hosszú kell legyen.
                  </div>
                </div>

                <div *ngIf="error" class="alert alert-danger">{{ error }}</div>

                <button
                  class="btn btn-primary w-100 mb-3"
                  type="submit"
                  [disabled]="registerForm.invalid || loading"
                >
                  {{ loading ? 'Regisztráció...' : 'Regisztráció' }}
                </button>
              </form>

              <div class="text-center">
                <span class="text-muted">Már van fiókja?</span>
                <a routerLink="/login">Bejelentkezés</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  protected readonly authService = inject(AuthService);
  protected readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly registerForm = this.fb.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  protected loading = false;
  protected error: string | null = null;

  protected onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = null;

    const payload: RegisterRequest = this.registerForm.value as RegisterRequest;

    this.authService.register(payload).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err) => {
        this.error = err?.error?.message ?? 'Regisztráció közben hiba történt.';
        this.loading = false;
      }
    });
  }
}
