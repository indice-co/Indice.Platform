import { Component, inject, input, output, signal } from '@angular/core';
import { LucideWandSparkles } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';
import { GeneratePasswordRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';

/** Asks the server for a random password that satisfies the configured password rules and emits it through `generated`. */
@Component({
  selector: 'app-generate-password-button',
  imports: [LucideWandSparkles, TranslatePipe],
  host: { class: 'inline-flex' },
  template: `
    <button type="button" class="btn btn-ghost btn-xs btn-square" [disabled]="busy()"
      [attr.aria-label]="'Users.Password.Generate' | translate" [attr.title]="'Users.Password.Generate' | translate" (click)="generate()">
      @if (busy()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideWandSparkles class="size-4"></svg> }
    </button>
  `
})
export class GeneratePasswordButtonComponent {
  private readonly _api = inject(IdentityApiService);

  /** An existing user, so that user-specific validators (e.g. password history) are taken into account. */
  public readonly userId = input<string | null | undefined>();
  /** The user name of a user that is not created yet. */
  public readonly userName = input<string | null | undefined>();
  public readonly generated = output<string>();
  protected readonly busy = signal(false);

  protected generate(): void {
    this.busy.set(true);
    this._api.generatePassword(new GeneratePasswordRequest({ userId: this.userId() || undefined, userName: this.userName() || undefined })).subscribe({
      next: result => {
        this.busy.set(false);
        if (result?.password) {
          this.generated.emit(result.password);
        }
      },
      error: () => this.busy.set(false)
    });
  }
}
