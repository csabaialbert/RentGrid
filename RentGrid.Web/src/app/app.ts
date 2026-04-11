import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { AuthService, CurrentUser } from './auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly currentUser = signal<CurrentUser | null>(null);
  protected readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');
  protected readonly displayName = computed(() => this.currentUser()?.email ?? '');
  protected readonly title = signal('RentGrid.Web');

  constructor() {
    this.authService.currentUser$.subscribe((user) => this.currentUser.set(user));
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
