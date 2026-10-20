import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer, JsonFormsOutlet } from '@jsonforms/angular';
import { Categorization, getData, JsonFormsState, mapStateToLayoutProps, OwnPropsOfRenderer } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, arrayIndicesOf, evaluateCondition, isShownForChannel } from '../jsonforms-utilities';

let nextId = 0;

/** `Categorization` (ajsf `tabs`): one tab per `Category`. Arrow keys move between tabs. */
@Component({
    selector: 'app-jf-categorization',
    imports: [JsonFormsOutlet],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        <div role="tablist" class="tabs tabs-border jf-tabs" (keydown)="onKeydown($event)">
          @for (tab of tabs; track $index) {
            <button
              type="button"
              role="tab"
              class="tab"
              [id]="tabId($index)"
              [class.tab-active]="$index === selected"
              [attr.aria-selected]="$index === selected"
              [attr.aria-controls]="panelId($index)"
              [attr.tabindex]="$index === selected ? 0 : -1"
              [innerHTML]="tab.label"
              (click)="selected = $index"></button>
          }
        </div>
        @for (tab of tabs; track $index) {
          <div role="tabpanel" class="jf-tab-panel" [id]="panelId($index)" [attr.aria-labelledby]="tabId($index)" [hidden]="$index !== selected">
            <jsonforms-outlet [renderProps]="tab.props"></jsonforms-outlet>
          </div>
        }
      </div>
    }`,
    host: { '[class.jf-hidden]': '!shown' },
    changeDetection: ChangeDetectionStrategy.Eager
})
export class CategorizationRendererComponent extends JsonFormsBaseRenderer<Categorization> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    private readonly uid = `jf-tabs-${nextId++}`;
    options: any = {};
    tabs: { label: string, props: OwnPropsOfRenderer }[] = [];
    selected = 0;
    shown = true;
    wrapperClass = '';

    ngOnInit(): void {
        this.options = this.uischema.options ?? {};
        this.wrapperClass = addClasses('jf-categorization', 'form-group', 'schema-form-tabs', this.options.htmlClass);
        this.tabs = (this.uischema.elements ?? []).map(category => ({
            label: typeof category.label === 'string' ? category.label : '',
            props: { uischema: category, schema: this.schema, path: this.path }
        }));
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            const { visible } = mapStateToLayoutProps(state, { uischema: this.uischema, schema: this.schema, path: this.path });
            this.shown = visible
                && isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)
                && evaluateCondition(this.options.condition, getData(state), arrayIndicesOf(this.path));
        }));
    }

    tabId(index: number): string {
        return `${this.uid}-tab-${index}`;
    }

    panelId(index: number): string {
        return `${this.uid}-panel-${index}`;
    }

    onKeydown(event: KeyboardEvent): void {
        const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
        if (!step || this.tabs.length === 0) {
            return;
        }
        event.preventDefault();
        this.selected = (this.selected + step + this.tabs.length) % this.tabs.length;
        (document.getElementById(this.tabId(this.selected)) as HTMLElement | null)?.focus();
    }
}
