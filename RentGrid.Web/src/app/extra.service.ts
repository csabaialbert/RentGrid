import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ExtraOption {
  id: number;
  name: string;
  price: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ExtraService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/extras';

  getExtras(includeInactive = false): Observable<ExtraOption[]> {
    const params: Record<string, string> = {};
    if (includeInactive) {
      params['includeInactive'] = 'true';
    }
    return this.http.get<ExtraOption[]>(this.apiUrl, { params });
  }

  createExtra(extraData: { name: string; price: number }): Observable<ExtraOption> {
    return this.http.post<ExtraOption>(this.apiUrl, extraData);
  }

  updateExtra(extraId: number, extraData: { name: string; price: number }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${extraId}`, extraData);
  }

  setExtraActive(extraId: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${extraId}/activation`, { isActive });
  }
}
