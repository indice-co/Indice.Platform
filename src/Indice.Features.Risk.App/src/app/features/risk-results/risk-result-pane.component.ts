import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { LucideActivity, LucideTriangleAlert } from '@lucide/angular';
import { map } from 'rxjs';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { JsonBlockComponent } from 'src/app/ui/json/json-block.component';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { RiskLevelBadgeComponent } from 'src/app/ui/risk/risk-level-badge.component';
import { SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';

/** Side pane with an evaluation's overall verdict, the result of every rule that ran and the evaluated data. */
@Component({
  selector: 'app-risk-result-pane',
  imports: [
    DatePipe, DecimalPipe, SidePaneLayoutComponent, JsonBlockComponent, EmptyStateComponent, RiskLevelBadgeComponent, LucideActivity,
    LucideTriangleAlert
  ],
  template: `
    @let result = resource.hasValue() ? resource.value() : null;
    <app-side-pane-layout eyebrow="Risk result" [title]="result?.name || 'Result details'"
      [subtitle]="result ? ((result.createdAt | date: 'EEEE, dd MMM yyyy · HH:mm:ss') ?? '') : ''">
      @if (result?.eventId) {
        <div header class="mt-4">
          <button type="button" class="btn btn-sm btn-soft btn-primary gap-1.5" (click)="showEvent(result!.eventId!)">
            <svg lucideActivity class="size-4"></svg>Show the evaluated event
          </button>
        </div>
      }

      @if (resource.isLoading()) {
        <div class="space-y-3">
          @for (row of [1, 2, 3, 4, 5]; track row) { <div class="skeleton h-8 w-full"></div> }
        </div>
      } @else if (!result) {
        <app-empty-state title="Result not found" message="It may have been removed, or the link is no longer valid.">
          <svg icon lucideTriangleAlert class="size-6"></svg>
        </app-empty-state>
      } @else {
        <div class="space-y-6">
          <div class="grid grid-cols-3 divide-x divide-base-300 rounded-box border border-base-300">
            <div class="p-4">
              <p class="text-xs font-medium text-base-content/60">Risk level</p>
              <app-risk-level-badge class="mt-2" size="md" [level]="result.riskLevel" />
            </div>
            <div class="p-4">
              <p class="text-xs font-medium text-base-content/60">Score</p>
              <p class="mt-1 text-2xl font-semibold tabular">{{ result.riskScore ?? '—' }}</p>
            </div>
            <div class="p-4">
              <p class="text-xs font-medium text-base-content/60">Rules executed</p>
              <p class="mt-1 text-2xl font-semibold tabular">{{ result.numberOfRulesExecuted ?? '—' }}</p>
            </div>
          </div>

          <div>
            <h3 class="mb-2 text-sm font-semibold">Rule results</h3>
            @if (result.results?.length) {
              <ul class="divide-y divide-base-300 rounded-box border border-base-300">
                @for (rule of result.results; track $index) {
                  <li class="flex items-start gap-3 px-4 py-3">
                    <div class="min-w-0 flex-1">
                      <p class="font-medium break-words">{{ rule.ruleName || '—' }}</p>
                      @if (rule.reason) { <p class="mt-0.5 text-sm text-base-content/70">{{ rule.reason }}</p> }
                    </div>
                    <div class="flex shrink-0 items-center gap-2">
                      <span class="text-sm font-semibold tabular">{{ rule.riskScore ?? '—' }}</span>
                      <app-risk-level-badge [level]="rule.riskLevel" />
                    </div>
                  </li>
                }
              </ul>
            } @else {
              <p class="rounded-field border border-dashed border-base-300 px-4 py-3 text-sm text-base-content/50">No rule produced a result.</p>
            }
          </div>

          <dl class="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
            @for (field of [
              { label: 'Type', value: result.type },
              { label: 'Amount', value: result.amount != null ? (result.amount | number: '1.2-2') : null },
              { label: 'Subject ID', value: result.subjectId, mono: true },
              { label: 'IP address', value: result.ipAddress, mono: true },
              { label: 'Event ID', value: result.eventId, mono: true },
              { label: 'Result ID', value: result.id, mono: true }
            ]; track field.label) {
              <div class="min-w-0">
                <dt class="text-xs font-medium text-base-content/60">{{ field.label }}</dt>
                <dd class="mt-0.5 break-all" [class.font-mono]="field.mono" [class.text-sm]="field.mono">{{ field.value || '—' }}</dd>
              </div>
            }
          </dl>

          <div>
            <h3 class="mb-2 text-sm font-semibold">Data</h3>
            <app-json-block [value]="result.data" />
          </div>
        </div>
      }
    </app-side-pane-layout>
  `
})
export class RiskResultPaneComponent {
  private readonly _api = inject(RiskApiService);
  private readonly _router = inject(Router);
  /** Route param. */
  public readonly id = input.required<string>();

  protected readonly resource = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this._api
      .getRiskResults([new FilterClause('id', id).toApiTerm()], 1, 1)
      .pipe(map(result => result.items?.[0] ?? null))
  });

  protected showEvent(eventId: string): void {
    this._router.navigate([{ outlets: { primary: 'risk-events', rightpane: null } }], { queryParams: { filter: `id::eq::${eventId}` } });
  }
}
