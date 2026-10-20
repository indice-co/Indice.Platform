import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { CaseTypeFormComponent } from '../case-type-form/case-type-form.component';
import { CaseTypeUpdateService } from '../case-type-update.service';

@Component({
  selector: 'app-case-type-create',
  imports: [RouterLink, NgIcon, TranslatePipe, PageHeaderComponent, CaseTypeFormComponent],
  template: `
    <div class="page max-w-5xl">
      <app-page-header [title]="'caseTypes.create.title' | translate">
        <a eyebrow routerLink="/case-types" class="link link-hover mb-1 inline-flex items-center gap-1 text-[0.8125rem] text-quiet">
          <ng-icon name="lucideArrowLeft" size="0.9rem" /> {{ 'nav.caseTypes' | translate }}
        </a>
      </app-page-header>
      <app-case-type-form [data]="data" (submitted)="onSubmit($event)" />
    </div>
  `
})
export class CaseTypeCreateComponent {
  private readonly caseTypesService = inject(CaseTypeUpdateService);
  protected readonly data: ICaseTypeRequest = {};

  protected onSubmit(event: ICaseTypeRequest): void {
    this.caseTypesService.onCreateSubmit(event ?? this.data);
  }
}
