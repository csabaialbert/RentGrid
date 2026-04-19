import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { LoginComponent } from './login.component';
import { RegisterComponent } from './register.component';
import { MyBookingsComponent } from './my-bookings.component';
import { DashboardComponent } from './dashboard.component';
import { VehicleAdminComponent } from './vehicle-admin.component';
import { ExtrasAdminComponent } from './extras-admin.component';
import { UserAdminComponent } from './user-admin.component';
import { AuthGuard } from './auth.guard';
import { AdminGuard } from './admin.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent, pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'my-bookings', component: MyBookingsComponent, canActivate: [AuthGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AdminGuard] },
  { path: 'user-admin', component: UserAdminComponent, canActivate: [AdminGuard] },
  { path: 'vehicle-admin', component: VehicleAdminComponent, canActivate: [AdminGuard] },
  { path: 'extras-admin', component: ExtrasAdminComponent, canActivate: [AdminGuard] },
  { path: '**', redirectTo: '' }
];
