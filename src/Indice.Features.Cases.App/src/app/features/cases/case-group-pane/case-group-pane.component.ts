import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideFolder, LucideTriangleAlert } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { CasesApiService, UpdateCaseGroupRequest } from 'src/app/core/services/cases-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** Side pane (administrators): the group of a case. The group takes part in the access rules. */
@Component({
  selector: 'app-case-group-pane',
  imports: [TranslatePipe, EmptyStateComponent, SidePaneLayoutComponent, LucideFolder, LucideTriangleAlert],
  template: `
    <app-side-pane-layout [title]="'caseDetails.groupId' | translate" [subtitle]="'caseGroup.subtitle' | translate">
      @if (case.hasValue()) {
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
      } @else if (case.error()) {
        <app-empty-state [title]="'caseDetails.loadError.title' | translate" [message]="'caseDetails.loadError.message' | translate">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="case.reload()">{{ 'common.retry' | translate }}</button>
        </app-empty-state>
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

  protected readonly case = rxResource({
    params: () => this.caseId(),
    stream: ({ params }) => this._api.getCaseById(params)
  });
  /** The edited group; the stored one again whenever the case (re)loads. */
  protected readonly groupId = linkedSignal(() => this.case.hasValue() ? this.case.value().groupId ?? '' : '');
  protected readonly changed = computed(() => this.case.hasValue() && this.groupId().trim() !== (this.case.value().groupId ?? ''));
  protected readonly saving = signal(false);

  protected async save(): Promise<void> {
    if (!this.changed() || this.saving()) {
      return;
    }
    const caseId = this.caseId();
    this.saving.set(true);
    try {
      await firstValueFrom(this._api.updateCaseGroup(caseId, new UpdateCaseGroupRequest({ groupId: this.groupId().trim() || undefined })));
      this._toast.success('toasts.success.title', 'caseGroup.saved');
      this._caseDetails.notifyCaseChanged(caseId);
      this.pane.close();
    } catch {
      // the problem details interceptor reports the failure
    } finally {
      this.saving.set(false);
    }
  }
}
