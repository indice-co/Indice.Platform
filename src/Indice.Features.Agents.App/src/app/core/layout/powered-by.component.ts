import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucideHeart } from '@lucide/angular';

import { settings } from '../models/settings';

/** Tiny branding footer: "Powered by Indice ♥ vX.Y.Z". Hosts control spacing/borders. */
@Component({
  selector: 'app-powered-by',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideHeart],
  template: `
    <p class="flex items-center gap-1 px-3 text-xs opacity-55">
      Powered by <a class="link link-hover" href="https://www.indice.gr" target="_blank" rel="noopener">Indice</a>
      <svg lucideHeart class="size-3 fill-current text-error"></svg>
      <span class="ml-auto">v{{ version }}</span>
    </p>
  `,
})
export class PoweredByComponent {
  protected readonly version = settings.version;
}
