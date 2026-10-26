import { Component, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CasePartial } from 'src/app/core/services/cases-api.service';

/** Grid of the cases sharing this case's external correlation key. The open case is marked rather than linked. */
@Component({
  selector: 'app-case-related',
  imports: [DatePipe, RouterLink, TranslatePipe],
  host: { class: 'block' },
  template: `
    <div class="max-h-96 overflow-auto">
      <table class="table table-sm table-pin-rows">
        <thead>
          <tr class="bg-base-100 text-base-content/70">
            <th class="font-medium whitespace-nowrap">{{ 'cases.referenceNumber' | translate }}</th>
            <th class="font-medium whitespace-nowrap">{{ 'cases.caseType' | translate }}</th>
            <th class="font-medium whitespace-nowrap">{{ 'cases.checkpointType' | translate }}</th>
            <th class="font-medium whitespace-nowrap">{{ 'cases.assignedTo' | translate }}</th>
            <th class="font-medium whitespace-nowrap">{{ 'cases.submitDate' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (relatedCase of relatedCases(); track relatedCase.id) {
            @let current = relatedCase.id === currentCaseId();
            <tr [class]="current ? 'bg-base-200/60' : 'cursor-pointer hover:bg-base-200'" [attr.aria-current]="current ? 'page' : null"
              (click)="current || open(relatedCase)">
              <td class="whitespace-nowrap">
                @if (current) {
                  <span class="docket font-semibold">{{ relatedCase.referenceNumber ?? '-' }}</span>
                  <span class="badge badge-sm badge-ghost ml-2">{{ 'caseDetails.related.current' | translate }}</span>
                } @else {
                  <a class="docket font-semibold text-primary hover:underline" [routerLink]="['/cases', relatedCase.id, 'details']"
                    (click)="$event.stopPropagation()">{{ relatedCase.referenceNumber ?? '-' }}</a>
                }
              </td>
              <td class="whitespace-nowrap">{{ relatedCase.caseType?.title ?? relatedCase.caseType?.code ?? '-' }}</td>
              <td class="whitespace-nowrap">
                @if (relatedCase.checkpointType; as checkpoint) {
                  <span class="stamp" [class]="'stamp-' + (checkpoint.status?.toLowerCase() ?? 'neutral')">{{ checkpoint.title ?? checkpoint.code }}</span>
                } @else { - }
              </td>
              <td class="whitespace-nowrap">{{ relatedCase.assignedToName || ('caseDetails.unassigned' | translate) }}</td>
              <td class="whitespace-nowrap">
                <span class="docket text-base-content/80">{{ relatedCase.createdByWhen ? (relatedCase.createdByWhen | date: 'dd/MM/yyyy HH:mm') : '-' }}</span>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class RelatedCasesComponent {
  private readonly _router = inject(Router);

  public readonly relatedCases = input<CasePartial[]>([]);
  public readonly currentCaseId = input<string | undefined>();

  protected open(relatedCase: CasePartial): void {
    this._router.navigate(['/cases', relatedCase.id, 'details']);
  }
}
