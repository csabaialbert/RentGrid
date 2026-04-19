import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, map } from 'rxjs';

export interface AuthResponse {
  token: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface CurrentUser {
  token: string;
  email: string;
  role: string;
  id: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly authEndpoint = '/api/auth';
  private readonly tokenKey = 'rentgrid_token';
  private readonly currentUserSubject = new BehaviorSubject<CurrentUser | null>(
    this.getStoredUser()
  );

  readonly currentUser$ = this.currentUserSubject.asObservable();

  get token(): string | null {
    return this.currentUserSubject.value?.token ?? null;
  }

  get currentUserId(): number | null {
    return this.currentUserSubject.value?.id ?? null;
  }

  private getStoredToken(): string | null {
    return typeof window !== 'undefined' ? window.localStorage.getItem(this.tokenKey) : null;
  }

  private getStoredUser(): CurrentUser | null {
    const token = this.getStoredToken();
    return token ? this.parseJwtToken(token) : null;
  }

  private parseJwtToken(token: string): CurrentUser | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return null;
      }

      const decoded = this.base64UrlDecode(payload);
      const parsed = JSON.parse(decoded) as Record<string, unknown>;

      const email = (parsed['email'] ?? parsed['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ?? '') as string;
      const role = (parsed['role'] ?? parsed['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? '') as string;
      const sub = (parsed['sub'] ?? parsed['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '') as string;
      const id = parseInt(sub, 10);

      return {
        token,
        email: email ?? '',
        role: role ?? '',
        id: isNaN(id) ? 0 : id
      };
    } catch {
      return null;
    }
  }

  private base64UrlDecode(value: string): string {
    const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');

    if (typeof window !== 'undefined' && typeof window.atob === 'function') {
      return window.atob(padded);
    }

    return Buffer.from(padded, 'base64').toString('utf-8');
  }

  private storeToken(token: string): void {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(this.tokenKey, token);
    }

    const user = this.parseJwtToken(token);
    this.currentUserSubject.next(user);
  }

  private clearToken(): void {
    if (typeof window !== 'undefined') {
      window.localStorage.removeItem(this.tokenKey);
    }

    this.currentUserSubject.next(null);
  }

  login(payload: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.authEndpoint}/login`, payload).pipe(
      map((response) => {
        this.storeToken(response.token);
        return response;
      })
    );
  }

  register(payload: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.authEndpoint}/register`, payload).pipe(
      map((response) => {
        this.storeToken(response.token);
        return response;
      })
    );
  }

  logout() {
    this.clearToken();
  }
}
