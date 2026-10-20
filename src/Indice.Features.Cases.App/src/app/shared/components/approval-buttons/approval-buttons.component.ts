import { Component, OnInit, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideCheck, LucideX } from '@lucide/angular';
import { Approval, ApprovalRequest, CasesApiService } from 'src/app/core/services/cases-api.service';
import { RadioButtonsListComponent, RadioOption } from 'src/app/shared/components/radio-buttons-list/radio-buttons-list.component';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';

/** The approve / reject decision on a case awaiting approval. */
@Component({
  selector: 'app-approval-buttons',
  imports: [TranslatePipe, RadioButtonsListComponent, LucideCheck, LucideX],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h3 class="font-semibold">{{ 'modals.approval.title' | translate }}</h3>
        <div class="join" role="radiogroup" [attr.aria-label]="'modals.approval.title' | translate">
          <button type="button" role="radio" class="join-item btn btn-sm" [attr.aria-checked]="approving()"
            [class]="approving() ? 'btn-success' : 'btn-ghost'" (click)="setApproving(true)">
            <svg lucideCheck class="size-4"></svg>{{ 'modals.approval.willApprove' | translate }}
          </button>
          <button type="button" role="radio" class="join-item btn btn-sm" [attr.aria-checked]="!approving()"
            [class]="!approving() ? 'btn-error' : 'btn-ghost'" (click)="setApproving(false)">
            <svg lucideX class="size-4"></svg>{{ 'modals.approval.willReject' | translate }}
          </button>
        </div>
      </div>

      @if (!approving() && rejectionOptions().length) {
        <app-radio-buttons-list name="rejection-reason" [title]="'modals.approval.rejectionReason' | translate"
          [options]="rejectionOptions()" [value]="selectedRejectReason()" (valueChange)="onOptionChange($event)" />
      }

      @if ((caseTypeConfig()?.commentsEnabled ?? true) && approving()) {
        <label class="block">
          <span class="mb-1.5 block text-sm font-medium">{{ 'common.comment' | translate }}</span>
          <textarea class="textarea w-full" rows="3" [placeholder]="'modals.approval.commentPlaceholder' | translate"
            [value]="comment() ?? ''" (input)="comment.set($any($event.target).value)"></textarea>
        </label>
      }

      <div class="flex justify-end">
        @if (approving()) {
          <button type="button" class="btn btn-success" [title]="'modals.approval.approveTooltip' | translate"
            [disabled]="!formValid() || submitting()" (click)="approveCase()">
            @if (submitting()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideCheck class="size-4"></svg> }
            {{ 'modals.approval.approve' | translate }}
          </button>
        } @else {
          <button type="button" class="btn btn-error" [title]="'modals.approval.rejectTooltip' | translate"
            [disabled]="submitting()" (click)="rejectCase()">
            @if (submitting()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideX class="size-4"></svg> }
            {{ 'modals.approval.reject' | translate }}
          </button>
        }
      </div>
    </div>
  `
})
export class ApprovalButtonsComponent implements OnInit {
  private readonly _toaster = inject(TranslatedToasterService);
  private readonly _api = inject(CasesApiService);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);

  public readonly caseId = input<string | undefined>();
  public readonly formValid = input(false);
  public readonly formUnSavedChanges = input(false);
  /** Ask for confirmation before approving. */
  public readonly showWarningModal = input<boolean | undefined>(false);
  public readonly caseTypeConfig = input<any>({});

  protected readonly approving = signal(true);
  protected readonly submitting = signal(false);
  protected readonly comment = signal<string | undefined>(undefined);
  protected readonly rejectionOptions = signal<RadioOption[]>([]);
  protected readonly selectedRejectReason = signal<string>('');

  public ngOnInit(): void {
    this._api.getCaseRejectReasons(this.caseId()!).subscribe(reasons => {
      const options = reasons.map(r => ({ text: r.value!, value: r.key! }));
      this.rejectionOptions.set(options);
      this.selectedRejectReason.set(options[0]?.value ?? '');
    });
  }

  protected setApproving(approving: boolean): void {
    if (this.approving() === approving) {
      return;
    }
    this.approving.set(approving);
    // a rejection carries the selected reason as its comment
    this.comment.set(approving ? undefined : this.selectedRejectReason());
  }

  protected onOptionChange(value: string): void {
    this.selectedRejectReason.set(value);
    this.comment.set(value);
  }

  protected approveCase(): void {
    if (!this.formValid()) {
      return;
    }
    if (this.formUnSavedChanges()) {
      this._toaster.show(ToastType.Warning, 'toasts.unsavedChanges.title', 'toasts.unsavedChanges.body', 5000);
      return;
    }
    if (!this.showWarningModal()) {
      this.caseDecision(Approval.Approve);
      return;
    }
    this._dialog.confirm({
      title: 'caseDetails.approveWarning.title',
      message: 'caseDetails.approveWarning.description',
      confirmLabel: 'common.yes',
      cancelLabel: 'common.no'
    }).subscribe(confirmed => {
      if (confirmed) {
        this.caseDecision(Approval.Approve);
      }
    });
  }

  protected rejectCase(): void {
    this.caseDecision(Approval.Reject);
  }

  private caseDecision(action: Approval): void {
    this.submitting.set(true);
    this._api.submitApproval(this.caseId()!, new ApprovalRequest({ action, comment: this.comment() })).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, undefined, action === Approval.Approve ? 'toasts.caseApproved' : 'toasts.caseRejected', 5000);
        this._router.navigate(['/cases']);
      },
      error: () => {
        this._toaster.show(ToastType.Error, 'toasts.caseDecisionError.title', 'toasts.caseDecisionError.body', 5000);
        this._router.navigate(['/cases']);
      }
    });
  }
}
