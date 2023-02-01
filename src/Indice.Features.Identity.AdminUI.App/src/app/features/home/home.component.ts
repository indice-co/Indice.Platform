import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html'
})
export class HomeComponent {
  constructor(private router: Router) { }

  public goToLogin(): void {
    this.router.navigate(['app/dashboard']);
  }
}
