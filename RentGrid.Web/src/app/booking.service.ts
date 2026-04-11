import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface CreateBookingRequest {
  vehicleId: number;
  startDate: string;
  endDate: string;
  extraServiceIds: number[];
}

export interface BookingExtra {
  id: number;
  name: string;
  price: number;
}

export interface MyBooking {
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
  extras: BookingExtra[];
}

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5001/api/booking';

  createBooking(payload: CreateBookingRequest): Observable<void> {
    return this.http.post<void>(this.apiUrl, payload);
  }

  getMyBookings(): Observable<MyBooking[]> {
    return this.http.get<MyBooking[]>(`${this.apiUrl}/my-bookings`);
  }

  cancelBooking(bookingId: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${bookingId}/cancel`, null);
  }

  deleteBooking(bookingId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${bookingId}`);
  }
}
