import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CasePartial } from 'src/app/core/services/cases-api.service';

/** Cases sharing the case's external correlation key. */
@Component({
  selector: 'app-case-related',
  imports: [DatePipe, RouterLink, NgIcon, TranslatePipe],
  template: `
    <ul class="max-h-64 divide-y divide-base-200 overflow-y-auto">
      @for (relatedCase of relatedCases(); track relatedCase.id) {
        <li>
          <a class="group flex items-center gap-3 px-5 py-2.5 transition-colors hover:bg-base-200" [routerLink]="['/cases', relatedCase.id, 'details']">
            <span class="min-w-0 flex-1">
              <span class="block truncate font-medium">{{ relatedCase.checkpointType?.code }}</span>
              <span class="block truncate text-xs text-quiet">
                <span class="ref">{{ relatedCase.caseType?.code }}</span>,
                <time class="kbd-ref">{{ relatedCase.createdByWhen | date: 'dd/MM/yyyy, HH:mm' }}</time>
              </span>
            </span>
            <ng-icon name="lucideArrowRight" size="1rem" class="text-quiet transition-transform group-hover:translate-x-0.5"
                     [attr.aria-label]="'caseDetails.related.open' | translate" />
          </a>
        </li>
      }
    </ul>
  `
})
export class RelatedCasesComponent {
  public readonly relatedCases = input<CasePartial[]>([]);
}
