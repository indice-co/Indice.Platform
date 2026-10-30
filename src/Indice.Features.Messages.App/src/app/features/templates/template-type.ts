import { TemplateType } from 'src/app/core/services/messages-api.service';

export const TEMPLATE_TYPES: TemplateType[] = [TemplateType.Full, TemplateType.Partial, TemplateType.Layout];

/** Translation key for a template type's display name. */
export function templateTypeLabel(type: TemplateType | string | undefined): string {
  switch (type) {
    case TemplateType.Full: return 'Templates.FullTemplate';
    case TemplateType.Partial: return 'Templates.PartialTemplate';
    case TemplateType.Layout: return 'Templates.LayoutTemplate';
    default: return 'Templates.EmptyValueIndicator';
  }
}

/** daisyUI badge modifier for a template type. */
export function templateTypeBadge(type: TemplateType | string | undefined): string {
  switch (type) {
    case TemplateType.Partial: return 'badge-info badge-soft';
    case TemplateType.Layout: return 'badge-accent badge-soft';
    default: return 'badge-primary badge-soft';
  }
}
