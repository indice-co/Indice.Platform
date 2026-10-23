import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { CaseTypeFormComponent } from '../case-type-form/case-type-form.component';
import { CaseTypeUpdateService } from '../case-type-update.service';

@Component({
  selector: 'app-case-type-create',
  imports: [TranslatePipe, PageHeaderComponent, CaseTypeFormComponent],
  template: `
    <app-page-header [title]="'caseTypes.create.title' | translate" />
    <app-case-type-form [busy]="busy()" (submitted)="onSubmit($event)" />
  `
})
export class CaseTypeCreateComponent {
  private readonly _caseTypes = inject(CaseTypeUpdateService);
  protected readonly busy = signal(false);

  protected onSubmit(request: ICaseTypeRequest): void {
    this.busy.set(true);
    this._caseTypes.onCreateSubmit(request).pipe(finalize(() => this.busy.set(false))).subscribe();
  }
}
