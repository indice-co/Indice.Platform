import {
  Component, Directive, ElementRef, Signal, TemplateRef, computed, contentChild, contentChildren, inject, input, linkedSignal, model, output,
  viewChild, viewChildren
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

let nextTabId = 0;

/** Context of the `appTabLabel` template. */
export interface TabLabelContext {
  $implicit: TabComponent;
}

/** Custom tab header: `<ng-template appTabLabel let-tab>…</ng-template>` inside an `<app-tab>`. */
@Directive({ selector: 'ng-template[appTabLabel]' })
export class TabLabelDirective {
  public readonly template = inject<TemplateRef<TabLabelContext>>(TemplateRef);

  public static ngTemplateContextGuard(_dir: TabLabelDirective, ctx: unknown): ctx is TabLabelContext {
    return true;
  }
}

/**
 * Lazy tab body: `<ng-template appTabContent>…</ng-template>` inside an `<app-tab>` is only instantiated once the tab
 * is first opened. Plain projected content is created up front, like any projected content.
 */
@Directive({ selector: 'ng-template[appTabContent]' })
export class TabContentDirective {
  public readonly template = inject<TemplateRef<unknown>>(TemplateRef);
}

/** A single tab of an `<app-tab-group>`. Renders nothing by itself; the group renders its header and body. */
@Component({
  selector: 'app-tab',
  template: `<ng-template><ng-content /></ng-template>`
})
export class TabComponent {
  private readonly _group = inject(TabGroupComponent, { optional: true });
  private readonly _autoId = `app-tab-${nextTabId++}`;
  private readonly _projected = viewChild.required(TemplateRef);
  private readonly _lazy = contentChild(TabContentDirective);

  /** Header text, used when no `appTabLabel` template is given. */
  public readonly label = input<string>();
  /** Unique id of the tab; also lets the group select it by id. Auto-generated when omitted. */
  public readonly id = input<string>();
  public readonly disabled = input(false);

  public readonly labelTemplate = contentChild(TabLabelDirective);
  public readonly tabId = computed(() => this.id() || this._autoId);
  public readonly content = computed<TemplateRef<unknown>>(() => this._lazy()?.template ?? this._projected());
  public readonly index: Signal<number> = computed(() => this._group?.tabs().indexOf(this) ?? -1);
  public readonly isActive: Signal<boolean> = computed(() => this._group?.activeTab() === this);
}

/**
 * Tabbed container. On narrow screens the tab headers collapse into a select.
 *
 * ```html
 * <app-tab-group [(selectedIndex)]="tab">
 *   <app-tab label="Details">…</app-tab>
 *   <app-tab id="history">
 *     <ng-template appTabLabel>History <span class="badge">3</span></ng-template>
 *     <ng-template appTabContent>…lazy…</ng-template>
 *   </app-tab>
 * </app-tab-group>
 * ```
 */
@Component({
  selector: 'app-tab-group',
  imports: [NgTemplateOutlet, TranslatePipe],
  host: { class: 'block' },
  template: `
    @if (showHeader()) {
      <div class="border-b border-base-300 p-3 sm:hidden">
        <select class="select select-sm w-full" [attr.aria-label]="'common.selectTab' | translate"
          (change)="select(+$any($event.target).value)">
          @for (tab of tabs(); track tab; let i = $index) {
            <option [value]="i" [selected]="tab === activeTab()" [disabled]="tab.disabled()">
              {{ tab.label() || defaultLabel(i) }}
            </option>
          }
        </select>
      </div>
      <div role="tablist" class="tabs tabs-border hidden flex-nowrap overflow-x-auto border-b border-base-300 px-2 sm:flex"
        (keydown)="onKeydown($event)">
        @for (tab of tabs(); track tab; let i = $index) {
          <button #tabButton type="button" role="tab" class="tab gap-2 whitespace-nowrap" [id]="tab.tabId() + '-label'"
            [class.tab-active]="tab === activeTab()" [attr.aria-selected]="tab === activeTab()"
            [attr.aria-controls]="tab.tabId() + '-panel'" [tabIndex]="tab === activeTab() ? 0 : -1" [disabled]="tab.disabled()"
            (click)="select(i)">
            @if (tab.labelTemplate(); as label) {
              <ng-container *ngTemplateOutlet="label.template; context: { $implicit: tab }" />
            } @else {
              {{ tab.label() || defaultLabel(i) }}
            }
          </button>
        }
      </div>
    }
    @for (tab of tabs(); track tab) {
      @if (tab === activeTab() || (preserveContent() && opened().has(tab))) {
        <div role="tabpanel" [id]="tab.tabId() + '-panel'" [attr.aria-labelledby]="showHeader() ? tab.tabId() + '-label' : null"
          [hidden]="tab !== activeTab()">
          <ng-container [ngTemplateOutlet]="tab.content()" />
        </div>
      }
    }
  `
})
export class TabGroupComponent {
  private readonly _buttons = viewChildren<ElementRef<HTMLButtonElement>>('tabButton');

  /** The tabs of the group, in document order. */
  public readonly tabs = contentChildren(TabComponent, { descendants: true });
  /** Index of the selected tab; supports two-way binding. */
  public readonly selectedIndex = model(0);
  /** Keep the bodies of tabs opened once in the DOM (hidden) so their state survives switching tabs. */
  public readonly preserveContent = input(true);
  /** Hide the headers while there is only one tab. */
  public readonly hideSingleTab = input(false);
  /** Emitted when the user selects another tab. */
  public readonly selectedTabChange = output<TabComponent>();

  /** The selected tab; falls back to the first enabled tab when the selected one is missing or disabled. */
  public readonly activeTab: Signal<TabComponent | undefined> = computed(() => {
    const tabs = this.tabs();
    const selected = tabs[this.selectedIndex()];
    return selected && !selected.disabled() ? selected : tabs.find(tab => !tab.disabled());
  });
  public readonly activeIndex: Signal<number> = computed(() => this.activeTab()?.index() ?? -1);

  protected readonly showHeader = computed(() => !this.hideSingleTab() || this.tabs().length > 1);
  /** Tabs opened at least once; their content stays rendered while `preserveContent` is on. */
  protected readonly opened = linkedSignal<{ tabs: readonly TabComponent[], active?: TabComponent }, ReadonlySet<TabComponent>>({
    source: () => ({ tabs: this.tabs(), active: this.activeTab() }),
    computation: ({ tabs, active }, previous) => {
      const opened = new Set([...(previous?.value ?? [])].filter(tab => tabs.includes(tab)));
      if (active) {
        opened.add(active);
      }
      return opened;
    }
  });

  /** Selects a tab by index or id. Disabled tabs are ignored. */
  public select(indexOrId: number | string): void {
    const tabs = this.tabs();
    const index = typeof indexOrId === 'number' ? indexOrId : tabs.findIndex(tab => tab.tabId() === indexOrId);
    const tab = tabs[index];
    if (!tab || tab.disabled() || tab === this.activeTab()) {
      return;
    }
    this.selectedIndex.set(index);
    this.selectedTabChange.emit(tab);
  }

  protected defaultLabel(index: number): string {
    return `tab-${index + 1}`;
  }

  /** Arrow keys, Home and End move between the enabled tabs (WAI-ARIA tabs pattern, automatic activation). */
  protected onKeydown(event: KeyboardEvent): void {
    const enabled = this.tabs().map((tab, index) => ({ tab, index })).filter(({ tab }) => !tab.disabled());
    if (!enabled.length) {
      return;
    }
    const position = Math.max(0, enabled.findIndex(({ tab }) => tab === this.activeTab()));
    let target: number;
    switch (event.key) {
      case 'ArrowRight': target = (position + 1) % enabled.length; break;
      case 'ArrowLeft': target = (position - 1 + enabled.length) % enabled.length; break;
      case 'Home': target = 0; break;
      case 'End': target = enabled.length - 1; break;
      default: return;
    }
    event.preventDefault();
    const { index } = enabled[target];
    this.select(index);
    this._buttons()[index]?.nativeElement.focus();
  }
}

/** Everything needed to build tabs, for a component's `imports`. */
export const TABS = [TabGroupComponent, TabComponent, TabLabelDirective, TabContentDirective] as const;
