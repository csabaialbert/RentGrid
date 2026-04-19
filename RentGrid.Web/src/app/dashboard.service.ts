import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PopularVehicleDto {
  vehicleId: number;
  brand: string;
  model: string;
  bookingCount: number;
}

export interface DashboardStats {
  totalRevenue: number;
  registeredUserCount: number;
  activeBookingCount: number;
  mostPopularVehicle?: PopularVehicleDto | null;
}

export interface AdminBookingExtra {
  id: number;
  name: string;
  price: number;
}

export interface AdminBooking {
  id: number;
  vehicleId: number;
  vehicleBrand: string;
  vehicleModel: string;
  vehicleDailyPrice: number;
  vehicleImageFileId?: string | null;
  startDate: string;
  endDate: string;
  totalPrice: number;
  status: string;
  userEmail: string;
  userFullName: string;
  extras: AdminBookingExtra[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/dashboard';

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiUrl}/stats`);
  }

  getAdminBookings(): Observable<AdminBooking[]> {
    return this.http.get<AdminBooking[]>('/api/booking/admin');
  }

  updateBookingStatus(bookingId: number, status: string): Observable<void> {
    return this.http.patch<void>(`/api/booking/${bookingId}/status`, { status });
  }
}
