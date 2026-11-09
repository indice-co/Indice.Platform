import { Component, OnInit, inject } from '@angular/core';
import { AuthService } from '@indice/ng-auth';

/** Invisible silent-renew endpoint loaded in a hidden iframe by oidc-client-ts. */
@Component({
  selector: 'app-auth-renew',
  template: '',
})
export class AuthRenewComponent implements OnInit {
  private readonly auth = inject(AuthService);

  ngOnInit(): void {
    this.auth.signinSilentCallback().subscribe({ error: () => undefined });
  }
}
