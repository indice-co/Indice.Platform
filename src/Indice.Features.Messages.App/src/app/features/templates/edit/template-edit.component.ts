import { AfterViewChecked, ChangeDetectorRef, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { HeaderMetaItem, ViewLayoutComponent } from '@indice/ng-components';
import { Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from './template-edit-store.service';
import { AppLanguagesService } from '../../../shared/services/app-languages.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-template-edit',
  templateUrl: './template-edit.component.html'
})
export class TemplateEditComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
  private _templateId?: string;
  private $destroy = new Subject<void>();

  constructor(
    private _activatedRoute: ActivatedRoute,
    private _changeDetector: ChangeDetectorRef,
    private _templateStore: TemplateEditStore,
    private _lang: AppLanguagesService
  ) { }

  public submitInProgress = false;
  public template: Template | undefined;
  public metaItems: HeaderMetaItem[] = [];

  public ngOnInit(): void {
    this._templateId = this._activatedRoute.snapshot.params['templateId'];
    if (this._templateId) {
      this._templateStore.getTemplate(this._templateId!).subscribe((template: Template) => {
          this.template = template;
          this._lang.translateKey('Templates.TemplateTitle', { title: template.name })
            .pipe(takeUntil(this.$destroy))
            .subscribe(translated => {
              this._layout.title = translated || `Template - ${template.name}`;
            });
        });
    }
  }

  public ngAfterViewChecked(): void {
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {
    this.$destroy.next();
    this.$destroy.complete();
  }
}
