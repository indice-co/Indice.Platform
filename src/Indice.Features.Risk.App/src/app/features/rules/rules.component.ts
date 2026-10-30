import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { LucideListChecks, LucidePencil, LucideRefreshCw, LucideSearch, LucideTriangleAlert } from '@lucide/angular';
import { map } from 'rxjs';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Scope used with {@link RefreshBus} when rule options change. */
export const RULES_SCOPE = 'rules';

@Component({
  selector: 'app-rules',
  imports: [RouterLink, PageHeaderComponent, EmptyStateComponent, LucideListChecks, LucidePencil, LucideRefreshCw, LucideSearch, LucideTriangleAlert],
  template: `
    <app-page-header title="Rules" subtitle="Rules registered with the risk engine. Enable, describe and tune each one."
      [count]="rules.hasValue() ? rules.value().length : null">
      @if (rules.hasValue()) {
        <p class="mt-2 flex items-center gap-3 text-sm text-base-content/70">
          <span><span class="font-semibold text-base-content tabular">{{ enabledCount() }}</span> enabled</span>
          <span class="text-base-content/30">·</span>
          <span><span class="font-semibold text-base-content tabular">{{ rules.value().length - enabledCount() }}</span> disabled</span>
        </p>
      }
    </app-page-header>

    <section class="panel">
      <div class="flex items-center gap-2 border-b border-base-300 p-4">
        <label class="input input-sm w-full max-w-xs">
          <svg lucideSearch class="size-4 opacity-60"></svg>
          <input type="search" class="grow" placeholder="Find a rule" [value]="term()" (input)="term.set($any($event.target).value)" />
        </label>
        <button type="button" class="btn btn-sm btn-ghost btn-square ml-auto" aria-label="Refresh" title="Refresh" (click)="rules.reload()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="rules.isLoading()"></svg>
        </button>
      </div>

      @if (rules.error()) {
        <app-empty-state title="Rules could not be loaded" message="The service did not respond as expected. Try again in a moment.">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="rules.reload()">Try again</button>
        </app-empty-state>
      } @else if (!rules.hasValue()) {
        <div class="space-y-3 p-4">
          @for (row of [1, 2, 3, 4]; track row) { <div class="skeleton h-12 w-full"></div> }
        </div>
      } @else if (visible().length === 0) {
        <app-empty-state title="No rules" [message]="term() ? 'No rule matches “' + term() + '”.' : 'No rules are registered with the risk engine.'">
          <svg icon lucideListChecks class="size-6"></svg>
        </app-empty-state>
      } @else {
        <ul class="divide-y divide-base-300">
          @for (rule of visible(); track rule.name) {
            <li class="flex items-start gap-4 px-4 py-4 sm:items-center">
              <span class="mt-1.5 size-2.5 shrink-0 rounded-full sm:mt-0" [class]="rule.enabled ? 'bg-success' : 'bg-base-300'"
                [attr.aria-label]="rule.enabled ? 'Enabled' : 'Disabled'"></span>
              <div class="min-w-0 flex-1">
                <div class="flex flex-wrap items-center gap-x-2 gap-y-1">
                  <p class="font-semibold">{{ rule.friendlyName || rule.name }}</p>
                  @if (rule.enabled) {
                    <span class="badge badge-soft badge-success badge-sm">Enabled</span>
                  } @else {
                    <span class="badge badge-ghost badge-sm">Disabled</span>
                  }
                </div>
                <p class="font-mono text-xs text-base-content/55">{{ rule.name }}</p>
                @if (rule.description) {
                  <p class="mt-1 max-w-3xl text-sm text-base-content/75">{{ rule.description }}</p>
                }
              </div>
              <a class="btn btn-sm btn-ghost gap-1.5" [attr.aria-label]="'Configure ' + (rule.friendlyName || rule.name)"
                [routerLink]="pane.link(['rule', rule.name!])">
                <svg lucidePencil class="size-4"></svg><span class="hidden sm:inline">Configure</span>
              </a>
            </li>
          }
        </ul>
      }
    </section>
  `
})
export class RulesComponent {
  private readonly _api = inject(RiskApiService);
  protected readonly pane = inject(SidePaneService);
  protected readonly term = signal('');

  protected readonly rules = rxResource({
    stream: () => this._api.getRiskRules().pipe(map(result => result.items ?? []))
  });
  protected readonly enabledCount = computed(() => this.rules.hasValue() ? this.rules.value().filter(r => r.enabled).length : 0);
  protected readonly visible = computed(() => {
    const term = this.term().trim().toLowerCase();
    const rules = this.rules.hasValue() ? this.rules.value() : [];
    return [...rules]
      .filter(r => !term || [r.name, r.friendlyName, r.description].some(text => text?.toLowerCase().includes(term)))
      .sort((a, b) => Number(!!b.enabled) - Number(!!a.enabled) || (a.friendlyName || a.name || '').localeCompare(b.friendlyName || b.name || ''));
  });

  constructor() {
    inject(RefreshBus).on(RULES_SCOPE).pipe(takeUntilDestroyed()).subscribe(() => this.rules.reload());
  }
}
