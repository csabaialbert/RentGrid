import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Vehicle {
  id: number;
  brand: string;
  model: string;
  dailyPrice: number;
  isAvailable: boolean;
  imageFileIds: string[];
}

export interface CreateVehicleRequest {
  brand: string;
  model: string;
  dailyPrice: number;
}

export interface UpdateVehiclePriceRequest {
  dailyPrice: number;
}

export interface UpdateVehicleAvailabilityRequest {
  isAvailable: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class VehicleService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5001/api/vehicle';

  getAllVehicles(minPrice?: number, maxPrice?: number, isAvailable?: boolean): Observable<Vehicle[]> {
    let params: any = {};
    if (minPrice !== undefined) params.minPrice = minPrice.toString();
    if (maxPrice !== undefined) params.maxPrice = maxPrice.toString();
    if (isAvailable !== undefined) params.isAvailable = isAvailable.toString();

    return this.http.get<Vehicle[]>(this.apiUrl, { params });
  }

  createVehicle(vehicleData: CreateVehicleRequest, images: File[]): Observable<Vehicle> {
    const formData = new FormData();
    formData.append('brand', vehicleData.brand);
    formData.append('model', vehicleData.model);
    formData.append('dailyPrice', vehicleData.dailyPrice.toString());
    images.forEach(image => formData.append('images', image));

    return this.http.post<Vehicle>(this.apiUrl, formData);
  }

  updateVehiclePrice(vehicleId: number, priceData: UpdateVehiclePriceRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${vehicleId}/price`, priceData);
  }

  updateVehicleAvailability(vehicleId: number, availabilityData: UpdateVehicleAvailabilityRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${vehicleId}/availability`, availabilityData);
  }

  getVehicleImage(imageId: string): string {
    return `${this.apiUrl}/image/${imageId}`;
  }

  addVehicleImages(vehicleId: number, images: File[]): Observable<{ imageIds: string[] }> {
    const formData = new FormData();
    images.forEach(image => formData.append('images', image));
    return this.http.post<{ imageIds: string[] }>(`${this.apiUrl}/${vehicleId}/images`, formData);
  }

  removeVehicleImage(vehicleId: number, imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${vehicleId}/images/${imageId}`);
  }
}
