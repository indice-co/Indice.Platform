import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, RouterViewAction, ToastType, ViewAction } from '@indice/ng-components';
import { Observable, combineLatest, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { MessagesApiClient, Template, TemplateListItemResultSet } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { AppTranslatedToaster } from 'src/app/shared/services/app-translated-toaster';

@Component({
    selector: 'app-templates',
    templateUrl: './templates.component.html',
    standalone: false
})
export class TemplatesComponent extends BaseListComponent<Template> implements OnInit, OnDestroy {
  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _api: MessagesApiClient,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _modalService: ModalService,
    private _languages: AppLanguagesService
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'name';
    this.sortdir = 'asc';
    this.search = '';
    // Fallback uses translation key as initial label.
    this.sortOptions = [new MenuOption('Templates.SortNameOption', 'name')];
  }

  private _destroy$ = new Subject<void>();

  public newItemLink: string | null = null;
  public full = true;

  public override ngOnInit(): void {
    super.ngOnInit();
    this.actions.push(new RouterViewAction(Icons.Add, 'templates/add-template', null, null));

    // Reactive translation of sort option labels.
    const sortKeys = this.sortOptions.map(o => o.text);
    combineLatest(sortKeys.map(k => this._languages.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public loadItems(): Observable<IResultSet<Template> | null | undefined> {
    return this._api
      .getTemplates(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
      .pipe(map((result: TemplateListItemResultSet) => (result as IResultSet<Template>)));
  }

  public deleteConfirmation(template: Template): void {
    const titleKey = 'Templates.DeleteTemplateTitle';
    const messageKey = 'Templates.DeleteConfirmMessage';
    const params = { name: template.name };
    combineLatest([
      this._languages.translateKey(titleKey),
      this._languages.translateKey(messageKey, params)
    ]).pipe(takeUntil(this._destroy$))
      .subscribe(([title, message]) => {
        const modal = this._modalService.show(BasicModalComponent, {
          animated: true,
          initialState: {
            title: title || titleKey,
            message: message || messageKey,
            data: template
          },
          keyboard: true
        });
        modal.onHidden?.pipe(takeUntil(this._destroy$)).subscribe((response: any) => {
          if (response.result?.answer) {
            this._api.deleteTemplate(response.result.data.id).subscribe(() => {
              this._toaster.show(ToastType.Success, 'Templates.DeleteSuccessTitle', 'Templates.DeleteSuccessMessage', undefined, { name: response.result.data.name });
              this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates']));
            });
          }
        });
      });
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
}
