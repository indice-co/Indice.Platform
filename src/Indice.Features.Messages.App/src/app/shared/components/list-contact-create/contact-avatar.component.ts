import { Component, computed, input } from '@angular/core';

/** Display name of a contact: full name, else first + last name, else email. */
export function contactDisplayName(contact: { fullName?: string; firstName?: string; lastName?: string; email?: string } | null | undefined): string {
  if (!contact) {
    return '';
  }
  return contact.fullName?.trim() || `${contact.firstName ?? ''} ${contact.lastName ?? ''}`.trim() || contact.email?.trim() || '';
}

/** Round avatar with the initials of a name (or a placeholder when there is no name). */
@Component({
  selector: 'app-contact-avatar',
  host: { class: 'avatar avatar-placeholder shrink-0' },
  template: `
    <div class="rounded-full bg-primary/10 font-semibold text-primary" [class]="sizeClass()">
      <span aria-hidden="true">{{ initials() }}</span>
    </div>
  `
})
export class ContactAvatarComponent {
  public readonly name = input<string | undefined>('');
  public readonly size = input<'sm' | 'md' | 'lg'>('md');

  protected readonly initials = computed(() => {
    const words = (this.name() ?? '').replace(/@.*$/, '').split(/[\s._-]+/).filter(Boolean);
    if (!words.length) {
      return '?';
    }
    const letters = words.length === 1 ? words[0].slice(0, 2) : words[0][0] + words[words.length - 1][0];
    return letters.toLocaleUpperCase();
  });

  protected readonly sizeClass = computed(() => ({ sm: 'size-8 text-xs', md: 'size-10 text-sm', lg: 'size-14 text-lg' })[this.size()]);
}
