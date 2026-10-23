import { Component, OnInit, inject, input } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowLeft, LucideFileCode, LucideInfo } from '@lucide/angular';
import { MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { templateTypeBadge, templateTypeLabel } from '../template-type';
import { TemplateEditStore } from './template-edit-store.service';

@Component({
  selector: 'app-template-edit',
  imports: [RouterLink, RouterOutlet, TranslatePipe, PageHeaderComponent, RouteTabsComponent, MessageTypeBadgeComponent, LucideArrowLeft],
  template: `
    @let template = store.template();
    <app-page-header [title]="template?.name ?? ''">
      <a leading class="btn btn-ghost btn-xs -ml-2 mb-2" routerLink="/templates">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'Templates.Title' | translate }}
      </a>
      @if (template) {
        <div class="mt-2 flex flex-wrap items-center gap-2">
          <span class="badge badge-sm" [class]="typeBadge(template.type)">{{ typeLabel(template.type) | translate }}</span>
          @if (template.alias) {
            <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ template.alias }}</code>
          }
          <app-message-type-badge [name]="template.messageType?.name" [classification]="template.messageType?.classification" />
        </div>
      } @else {
        <div class="skeleton mt-2 h-5 w-48"></div>
      }
    </app-page-header>

    <app-route-tabs class="mb-6" [tabs]="tabs" />
    <router-outlet />
  `
})
export class TemplateEditComponent implements OnInit {
  protected readonly store = inject(TemplateEditStore);

  /** Bound from the `:templateId` route param. */
  public readonly templateId = input.required<string>();

  protected readonly typeLabel = templateTypeLabel;
  protected readonly typeBadge = templateTypeBadge;
  protected readonly tabs: RouteTab[] = [
    { label: 'Templates.BasicInformationNav', link: 'template-details', icon: LucideInfo },
    { label: 'Templates.ContentNav', link: 'template-content', icon: LucideFileCode }
  ];

  public ngOnInit(): void {
    this.store.load(this.templateId());
  }
}
