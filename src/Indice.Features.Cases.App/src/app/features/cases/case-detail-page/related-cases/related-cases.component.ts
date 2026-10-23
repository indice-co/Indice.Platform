import { Component, input } from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideChevronRight } from '@lucide/angular';
import { CasePartial } from 'src/app/core/services/cases-api.service';

/** Cases sharing this case's external correlation key. The open case is marked rather than linked. */
@Component({
  selector: 'app-case-related',
  imports: [DatePipe, NgTemplateOutlet, RouterLink, TranslatePipe, LucideChevronRight],
  host: { class: 'block' },
  template: `
    <section class="panel p-5">
      <h2 class="mb-2 text-sm font-semibold">{{ 'caseDetails.related.title' | translate }}</h2>
      <ul class="-mx-2 max-h-64 overflow-y-auto">
        @for (relatedCase of relatedCases(); track relatedCase.id) {
          <li>
            @if (relatedCase.id === currentCaseId()) {
              <div class="flex items-center gap-3 rounded-field px-2 py-2">
                <ng-container *ngTemplateOutlet="row; context: { $implicit: relatedCase }" />
                <span class="badge badge-sm badge-ghost">{{ 'caseDetails.related.current' | translate }}</span>
              </div>
            } @else {
              <a class="flex items-center gap-3 rounded-field px-2 py-2 hover:bg-base-200" [routerLink]="['/cases', relatedCase.id, 'details']">
                <ng-container *ngTemplateOutlet="row; context: { $implicit: relatedCase }" />
                <svg lucideChevronRight class="size-4 opacity-50"></svg>
              </a>
            }
          </li>
        }
      </ul>
    </section>

    <ng-template #row let-item>
      <span class="min-w-0 flex-1">
        <span class="block truncate text-sm font-semibold">{{ item.checkpointType?.code }}</span>
        <span class="block truncate text-xs text-base-content/65">
          {{ item.caseType?.code }}, {{ item.createdByWhen | date: 'dd/MM/yyyy, HH:mm' }}
        </span>
      </span>
    </ng-template>
  `
})
export class RelatedCasesComponent {
  public readonly relatedCases = input<CasePartial[]>([]);
  public readonly currentCaseId = input<string | undefined>();
}
