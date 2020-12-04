import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { AppComponent } from './app.component';
import { ShutdownComponent } from './shared/components/shutdown/shutdown.component';

const routes: Routes = [
  { path: 'app', component: AppComponent },
  { path: 'login', component: LoginComponent },
  { path: 'shutdown', component: ShutdownComponent },
  { path: '', redirectTo: 'app', pathMatch: 'full' },
  { path: '**', redirectTo: 'app', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { relativeLinkResolution: 'legacy' })],
  exports: [RouterModule]
})

export class AppRoutingModule { }
