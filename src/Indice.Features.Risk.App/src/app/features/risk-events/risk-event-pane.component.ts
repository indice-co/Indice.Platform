import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { LucideGauge, LucideTriangleAlert } from '@lucide/angular';
import { map } from 'rxjs';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { JsonBlockComponent } from 'src/app/ui/json/json-block.component';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';

/** Side pane with every recorded detail of a risk event, including its free-form data. */
@Component({
  selector: 'app-risk-event-pane',
  imports: [DatePipe, DecimalPipe, SidePaneLayoutComponent, JsonBlockComponent, EmptyStateComponent, LucideGauge, LucideTriangleAlert],
  template: `
    @let event = resource.hasValue() ? resource.value() : null;
    <app-side-pane-layout eyebrow="Risk event" [title]="event?.name || 'Event details'"
      [subtitle]="event ? ((event.createdAt | date: 'EEEE, dd MMM yyyy · HH:mm:ss') ?? '') : ''">
      @if (event?.subjectId) {
        <div header class="mt-4">
          <button type="button" class="btn btn-sm btn-soft btn-primary gap-1.5" (click)="showResults(event!.subjectId!)">
            <svg lucideGauge class="size-4"></svg>Risk results for this subject
          </button>
        </div>
      }

      @if (resource.isLoading()) {
        <div class="space-y-3">
          @for (row of [1, 2, 3, 4, 5]; track row) { <div class="skeleton h-8 w-full"></div> }
        </div>
      } @else if (!event) {
        <app-empty-state title="Event not found" message="It may have been removed, or the link is no longer valid.">
          <svg icon lucideTriangleAlert class="size-6"></svg>
        </app-empty-state>
      } @else {
        <div class="space-y-6">
          <dl class="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
            @for (field of [
              { label: 'Type', value: event.type },
              { label: 'Amount', value: event.amount != null ? (event.amount | number: '1.2-2') : null },
              { label: 'Subject ID', value: event.subjectId, mono: true },
              { label: 'IP address', value: event.ipAddress, mono: true },
              { label: 'Country', value: event.countryIsoCode },
              { label: 'Location', value: event.location },
              { label: 'Coordinates', value: event.coordinates, mono: true },
              { label: 'Source ID', value: event.sourceId, mono: true },
              { label: 'Source transaction ID', value: event.sourceTransId, mono: true },
              { label: 'Session ID', value: event.sessionId, mono: true },
              { label: 'Event ID', value: event.id, mono: true }
            ]; track field.label) {
              <div class="min-w-0">
                <dt class="text-xs font-medium text-base-content/60">{{ field.label }}</dt>
                <dd class="mt-0.5 break-all" [class.font-mono]="field.mono" [class.text-sm]="field.mono">{{ field.value || '—' }}</dd>
              </div>
            }
          </dl>

          <div>
            <h3 class="mb-2 text-sm font-semibold">Data</h3>
            <app-json-block [value]="event.data" />
          </div>
        </div>
      }
    </app-side-pane-layout>
  `
})
export class RiskEventPaneComponent {
  private readonly _api = inject(RiskApiService);
  private readonly _router = inject(Router);
  /** Route param. */
  public readonly id = input.required<string>();

  protected readonly resource = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this._api
      .getRiskEvents([new FilterClause('id', id).toApiTerm()], 1, 1)
      .pipe(map(result => result.items?.[0] ?? null))
  });

  protected showResults(subjectId: string): void {
    this._router.navigate([{ outlets: { primary: 'risk-results', rightpane: null } }], { queryParams: { filter: `subjectId::eq::${subjectId}` } });
  }
}
