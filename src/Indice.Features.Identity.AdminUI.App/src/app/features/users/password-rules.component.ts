import { Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { LucideCheck, LucideCircle, LucideX } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, combineLatest, debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs';
import { IdentityApiService, PasswordRuleInfo, ValidatePasswordRequest } from 'src/app/core/services/identity-api.service';

/**
 * Live checklist of the password rules configured on the server, re-evaluated as the admin types. Rules come from
 * `account/validate-password`, so they include every registered password validator and are localized by the server.
 */
@Component({
  selector: 'app-password-rules',
  imports: [TranslatePipe, LucideCheck, LucideCircle, LucideX],
  template: `
    @if (rules().length) {
      <div class="rounded-box bg-base-200/60 px-3 py-2.5" aria-live="polite">
        <p class="mb-1.5 text-xs font-medium text-base-content/70">{{ 'Users.Password.Rules' | translate }}</p>
        <ul class="space-y-1 text-xs">
          @for (rule of rules(); track rule.code) {
            <li class="flex items-start gap-2" [class]="stateClass(rule)">
              @if (!typed()) {
                <svg lucideCircle class="mt-px size-3.5 shrink-0"></svg>
              } @else if (rule.isValid) {
                <svg lucideCheck class="mt-px size-3.5 shrink-0"></svg>
              } @else {
                <svg lucideX class="mt-px size-3.5 shrink-0"></svg>
              }
              <span>
                {{ rule.requirement || rule.description }}
                @if (typed()) {
                  <span class="sr-only">({{ (rule.isValid ? 'Users.Password.RuleMet' : 'Users.Password.RuleNotMet') | translate }})</span>
                }
              </span>
            </li>
          }
        </ul>
      </div>
    }
  `
})
export class PasswordRulesComponent {
  private readonly _api = inject(IdentityApiService);

  /** The password being typed. */
  public readonly password = input<string>('');
  /** Id of an existing user, so that user-specific validators (e.g. password history) can run. */
  public readonly userId = input<string | undefined>(undefined);
  /** User name of a user being created, for validators that compare the password with it. */
  public readonly userName = input<string | undefined>(undefined);

  protected readonly typed = computed(() => !!this.password());
  protected readonly rules = toSignal(
    combineLatest([toObservable(this.password), toObservable(this.userId), toObservable(this.userName)]).pipe(
      debounceTime(300),
      distinctUntilChanged(([a, b, c], [x, y, z]) => a === x && b === y && c === z),
      // A failed check (e.g. rate limited) keeps the last known result instead of hiding the list.
      switchMap(([password, userId, userName]) => this._api
        .validatePassword(new ValidatePasswordRequest({ password, token: userId || undefined, userName: userName || undefined }))
        .pipe(map(result => result.passwordRules ?? []), catchError(() => EMPTY)))
    ),
    { initialValue: [] as PasswordRuleInfo[] }
  );

  protected stateClass(rule: PasswordRuleInfo): string {
    if (!this.typed()) {
      return 'text-base-content/70';
    }
    return rule.isValid ? 'text-success' : 'text-error';
  }
}
