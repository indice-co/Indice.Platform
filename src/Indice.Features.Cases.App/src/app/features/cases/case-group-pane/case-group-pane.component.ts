import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideFolder, LucideTriangleAlert } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { Case, CasesApiService, UpdateCaseGroupRequest } from 'src/app/core/services/cases-api.service';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** Side pane (administrators): the group of a case. The group takes part in the access rules. */
@Component({
  selector: 'app-case-group-pane',
  imports: [TranslatePipe, SidePaneLayoutComponent, LucideFolder, LucideTriangleAlert],
  template: `
    <app-side-pane-layout [title]="'caseDetails.groupId' | translate" [subtitle]="'caseGroup.subtitle' | translate">
      @if (case()) {
        <form id="case-group-form" class="space-y-4" (submit)="$event.preventDefault(); save()">
          <label class="input w-full">
            <svg lucideFolder class="size-4 opacity-60"></svg>
            <input type="text" class="docket grow" name="groupId" maxlength="128" [value]="groupId()"
              (input)="groupId.set($any($event.target).value)" [attr.aria-label]="'caseDetails.groupId' | translate" />
          </label>
          <div role="alert" class="alert alert-warning alert-soft text-sm">
            <svg lucideTriangleAlert class="size-4 shrink-0"></svg>
            <span>{{ 'caseGroup.accessWarning' | translate }}</span>
          </div>
        </form>
      } @else {
        <div class="skeleton h-11 w-full"></div>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'common.cancel' | translate }}</button>
        <button type="submit" form="case-group-form" class="btn btn-primary" [disabled]="!changed() || saving()">
          @if (saving()) { <span class="loading loading-spinner loading-xs"></span> }
          {{ 'common.save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class CaseGroupPaneComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _caseDetails = inject(CaseDetailsService);
  private readonly _toast = inject(ToastService);
  protected readonly pane = inject(SidePaneService);

  /** The case whose group is edited (`case-group;caseId=X`). */
  public readonly caseId = input.required<string>();

  protected readonly case = signal<Case | undefined>(undefined);
  protected readonly groupId = signal('');
  protected readonly changed = computed(() => !!this.case() && this.groupId().trim() !== (this.case()!.groupId ?? ''));
  protected readonly saving = signal(false);

  constructor() {
    effect(() => {
      const caseId = this.caseId();
      untracked(() => {
        this.case.set(undefined);
        this._api.getCaseById(caseId).subscribe(caseModel => {
          this.case.set(caseModel);
          this.groupId.set(caseModel.groupId ?? '');
        });
      });
    });
  }

  protected save(): void {
    if (!this.changed()) {
      return;
    }
    this.saving.set(true);
    this._api.updateCaseGroup(this.caseId(), new UpdateCaseGroupRequest({ groupId: this.groupId().trim() || undefined })).subscribe({
      next: () => {
        this._toast.success('toasts.success.title', 'caseGroup.saved');
        this._caseDetails.notifyCaseChanged(this.caseId());
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
