import { Component, Inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { APP_LANGUAGES, BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, RouterViewAction, ToastType, ViewAction } from '@indice/ng-components';
import { Observable, combineLatest, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { MessagesApiClient, Template, TemplateListItemResultSet, TemplateType } from 'src/app/core/services/messages-api.service';
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
    @Inject(APP_LANGUAGES) private _lang: AppLanguagesService

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

  public readonly templateTypeEnum = TemplateType;
  public selectedTypeFilter: TemplateType | undefined = undefined;
  public typeFilterOptions: MenuOption[] = [
    new MenuOption('Templates.AllTypes', undefined),
    new MenuOption('Templates.FullTemplate', TemplateType.Full),
    new MenuOption('Templates.PartialTemplate', TemplateType.Partial)
  ];
  public selectedTypeFilterOption: MenuOption = this.typeFilterOptions[0];

  public override ngOnInit(): void {
    super.ngOnInit();
    this.actions.push(new RouterViewAction(Icons.Add, 'templates/add-template', null, 'add a new template'));

    // Reactive translation of sort option labels.
    const sortKeys = this.sortOptions.map(o => o.text);
    combineLatest(sortKeys.map(k => this._lang.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });

    // Reactive translation of the type filter labels.
    const typeFilterKeys = this.typeFilterOptions.map(o => o.text);
    combineLatest(typeFilterKeys.map(k => this._lang.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.typeFilterOptions = this.typeFilterOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
        this.selectedTypeFilterOption = this.typeFilterOptions.find(o => o.value === this.selectedTypeFilter) ?? this.typeFilterOptions[0];
      });
  }

  public onTypeFilterChanged(option: MenuOption): void {
    this.selectedTypeFilterOption = option;
    this.selectedTypeFilter = option.value as TemplateType | undefined;
    this.page = 1;
    this.refresh();
  }

  public typeLabelKey(type: TemplateType | undefined): string {
    if (type === TemplateType.Partial) {
      return 'Templates.PartialTemplate';
    }
    if (type === TemplateType.Full) {
      return 'Templates.FullTemplate';
    }
    return 'Templates.EmptyValueIndicator';
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public loadItems(): Observable<IResultSet<Template> | null | undefined> {
    return this._api
      .getTemplates(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, undefined, undefined, this.selectedTypeFilter)
      .pipe(map((result: TemplateListItemResultSet) => (result as IResultSet<Template>)));
  }

  public deleteConfirmation(template: Template): void {
    const titleKey = 'Templates.DeleteTemplateTitle';
    const messageKey = 'Templates.DeleteConfirmMessage';
    const params = { name: template.name };
    combineLatest([
      this._lang.translateKey(titleKey),
      this._lang.translateKey(messageKey, params)
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
