import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ExtraOption {
  id: number;
  name: string;
  price: number;
}

@Injectable({
  providedIn: 'root'
})
export class ExtraService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5001/api/extras';

  getExtras(): Observable<ExtraOption[]> {
    return this.http.get<ExtraOption[]>(this.apiUrl);
  }
}
