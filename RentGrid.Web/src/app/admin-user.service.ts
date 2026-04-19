import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface AdminUserDto {
  id: number;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminUserService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/user';

  getAllUsers(): Observable<AdminUserDto[]> {
    return this.http.get<AdminUserDto[]>(`${this.apiUrl}`);
  }

  toggleUserActive(userId: number): Observable<AdminUserDto> {
    return this.http.put<AdminUserDto>(`${this.apiUrl}/${userId}/toggle-active`, {});
  }
}
