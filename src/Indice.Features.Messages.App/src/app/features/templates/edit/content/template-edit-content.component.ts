import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideSave } from '@lucide/angular';
import { MessageContent, Template } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { TemplateContentEditorComponent } from '../../content-editor/template-content-editor.component';
import { TemplateEditStore } from '../template-edit-store.service';

@Component({
  selector: 'app-template-content-edit',
  imports: [TranslatePipe, TemplateContentEditorComponent, LucideSave],
  host: { '(document:keydown.control.s)': 'onSaveShortcut($event)' },
  template: `
    <div class="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h2 class="font-semibold">{{ 'Templates.ContentLabel' | translate }}</h2>
        <p class="text-sm text-base-content/70">{{ 'Templates.ContentDescription' | translate }}</p>
      </div>
      <button type="button" class="btn btn-primary btn-sm" [disabled]="!template() || saving() || editor()?.form?.invalid" (click)="save()">
        @if (saving()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideSave class="size-4"></svg> }
        {{ 'Templates.Save' | translate }}
        <kbd class="kbd kbd-xs hidden opacity-70 sm:inline-flex">Ctrl S</kbd>
      </button>
    </div>

    @if (template()) {
      <app-template-content-editor [content]="content()" [data]="data()" />
    } @else {
      <div class="space-y-4">
        <div class="skeleton h-20 w-full"></div>
        <div class="skeleton h-96 w-full"></div>
      </div>
    }
  `
})
export class TemplateContentEditComponent implements OnInit {
  private readonly _store = inject(TemplateEditStore);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _route = inject(ActivatedRoute);

  protected readonly editor = viewChild(TemplateContentEditorComponent);
  protected readonly saving = signal(false);
  // A snapshot of the template, taken once: saving reloads the store, which must not reset the editor.
  protected readonly template = signal<Template | undefined>(undefined);
  protected readonly content = signal<{ [key: string]: MessageContent } | undefined>(undefined);
  protected readonly data = signal<any>({});

  public ngOnInit(): void {
    const id = this._route.parent?.snapshot.paramMap.get('templateId');
    if (!id) {
      return;
    }
    this._store.getTemplate(id).subscribe(template => {
      this.content.set(template.content);
      this.data.set(template.data ?? {});
      this.template.set(template);
    });
  }

  protected onSaveShortcut(event: Event): void {
    event.preventDefault();
    this.save();
  }

  protected save(): void {
    const editor = this.editor();
    const template = this.template();
    if (!editor || !template || this.saving()) {
      return;
    }
    if (editor.form.invalid) {
      editor.markAllAsTouched();
      return;
    }
    const { content, data } = editor.getValue();
    this.saving.set(true);
    this._store.updateTemplate(template.id!, new Template({ ...template, content, data })).subscribe({
      next: () => {
        this.saving.set(false);
        this.template.set(new Template({ ...template, content, data }));
        editor.form.markAsPristine();
        this._toaster.show(ToastType.Success, 'Templates.ContentUpdateSuccessTitle', 'Templates.ContentUpdateSuccessMessage', undefined, { title: template.name });
      },
      error: () => this.saving.set(false)
    });
  }
}
