import { Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Approval, ApprovalRequest, CasesApiService, RejectReason } from 'src/app/core/services/cases-api.service';
import { ConfirmDialogComponent, ConfirmDialogData } from 'src/app/ui/feedback/confirm-dialog.component';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';

/**
 * Approve or reject the case. Approving needs a valid, saved form and may ask for confirmation (case type
 * `boOptions.showWarningModal`, or until the PDF was downloaded); its comment is free text. Rejecting sends the chosen
 * reject reason as the comment. Either way the user returns to the case list.
 */
@Component({
  selector: 'app-case-decision',
  imports: [FormsModule, NgIcon, TranslatePipe],
  template: `
    <div class="join w-full" role="radiogroup" [attr.aria-label]="'modals.approval.title' | translate">
      <button type="button" role="radio" class="btn join-item flex-1" [class.btn-active]="!rejecting()" [attr.aria-checked]="!rejecting()"
              (click)="setRejecting(false)">
        <ng-icon name="lucideCircleCheck" size="1rem" /> {{ 'modals.approval.approve' | translate }}
      </button>
      <button type="button" role="radio" class="btn join-item flex-1" [class.btn-active]="rejecting()" [attr.aria-checked]="rejecting()"
              (click)="setRejecting(true)">
        <ng-icon name="lucideCircleX" size="1rem" /> {{ 'modals.approval.reject' | translate }}
      </button>
    </div>
    <p class="mt-2 text-[0.8125rem] text-quiet">{{ (rejecting() ? 'modals.approval.willReject' : 'modals.approval.willApprove') | translate }}</p>

    @if (rejecting()) {
      @if (rejectionOptions().length) {
        <fieldset class="mt-4">
          <legend class="mb-2 text-[0.8125rem] font-medium">{{ 'modals.approval.rejectionReason' | translate }}</legend>
          <div class="space-y-1.5">
            @for (option of rejectionOptions(); track option.key) {
              <label class="flex cursor-pointer items-start gap-3 rounded-field border px-3 py-2"
                     [class]="selectedRejectReason() === option.key ? 'border-primary bg-accent/50' : 'border-base-300 hover:bg-base-200/60'">
                <input type="radio" name="reject-reason" class="radio radio-primary radio-xs mt-1" [checked]="selectedRejectReason() === option.key"
                       (change)="selectRejectReason(option.key!)" />
                <span>{{ option.value }}</span>
              </label>
            }
          </div>
        </fieldset>
      }
    } @else if (commentsEnabled()) {
      <label class="mt-4 block">
        <span class="mb-1 block text-[0.8125rem] font-medium">{{ 'common.comment' | translate }}</span>
        <textarea class="textarea w-full" rows="3" name="comment" [placeholder]="'modals.approval.commentPlaceholder' | translate"
                  [ngModel]="comment()" (ngModelChange)="comment.set($event)"></textarea>
      </label>
    }

    <button type="button" class="btn mt-4 w-full" [class]="rejecting() ? 'btn-error' : 'btn-primary'"
            [disabled]="rejecting() ? buttonsDisabled() : (!formValid() || buttonsDisabled())"
            [title]="(rejecting() ? 'modals.approval.rejectTooltip' : 'modals.approval.approveTooltip') | translate"
            (click)="rejecting() ? rejectCase() : approveCase()">
      @if (buttonsDisabled()) { <span class="loading loading-spinner loading-xs"></span> }
      {{ (rejecting() ? 'modals.approval.rejectTooltip' : 'modals.approval.approveTooltip') | translate }}
    </button>
    @if (!rejecting() && !formValid()) {
      <p class="mt-2 text-[0.8125rem] text-quiet">{{ 'caseDetails.decision.formInvalid' | translate }}</p>
    }
  `
})
export class CaseDecisionComponent implements OnInit {
  private readonly api = inject(CasesApiService);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly toaster = inject(ToastService);

  public readonly caseId = input.required<string>();
  public readonly formValid = input(false);
  public readonly formUnSavedChanges = input(false);
  /** Ask for confirmation before approving. */
  public readonly showWarningModal = input(false);
  public readonly warningModalState = input<{ title?: string; description?: string } | undefined>();
  public readonly caseTypeConfig = input<any>({});

  protected readonly rejecting = signal(false);
  protected readonly buttonsDisabled = signal(false);
  protected readonly comment = signal<string | undefined>(undefined);
  protected readonly rejectionOptions = signal<RejectReason[]>([]);
  protected readonly selectedRejectReason = signal('');
  protected readonly commentsEnabled = computed(() => this.caseTypeConfig()?.commentsEnabled ?? true);

  public ngOnInit(): void {
    this.api.getCaseRejectReasons(this.caseId()).subscribe(reasons => {
      this.rejectionOptions.set(reasons);
      this.selectedRejectReason.set(reasons[0]?.key ?? '');
    });
  }

  protected setRejecting(rejecting: boolean): void {
    this.rejecting.set(rejecting);
    this.comment.set(rejecting ? this.selectedRejectReason() : undefined);
  }

  protected selectRejectReason(key: string): void {
    this.selectedRejectReason.set(key);
    this.comment.set(key);
  }

  protected approveCase(): void {
    if (!this.formValid()) {
      return;
    }
    if (this.formUnSavedChanges()) {
      this.toaster.show(ToastType.Warning, 'toasts.unsavedChanges.title', 'toasts.unsavedChanges.body', 5000);
      return;
    }
    if (!this.showWarningModal()) {
      this.decide(Approval.Approve);
      return;
    }
    const data: ConfirmDialogData = {
      title: this.warningModalState()?.title ?? '',
      message: this.warningModalState()?.description,
      confirmLabel: 'common.yes',
      cancelLabel: 'common.no',
      tone: 'warning'
    };
    this.dialogs.open<ConfirmDialogComponent, boolean>(ConfirmDialogComponent, { data }).closed.subscribe(confirmed => {
      if (confirmed === true) {
        this.decide(Approval.Approve);
      }
    });
  }

  protected rejectCase(): void {
    this.decide(Approval.Reject);
  }

  private decide(action: Approval): void {
    this.buttonsDisabled.set(true);
    this.api.submitApproval(this.caseId(), new ApprovalRequest({ action, comment: this.comment() })).subscribe({
      next: () => {
        this.toaster.show(ToastType.Success, undefined, action === Approval.Approve ? 'toasts.caseApproved' : 'toasts.caseRejected', 5000);
        this.router.navigate(['/cases']);
      },
      error: () => {
        this.toaster.show(ToastType.Error, 'toasts.caseDecisionError.title', 'toasts.caseDecisionError.body', 5000);
        this.router.navigate(['/cases']);
      }
    });
  }
}
