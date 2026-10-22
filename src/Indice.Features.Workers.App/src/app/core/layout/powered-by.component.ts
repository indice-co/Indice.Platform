import { ChangeDetectionStrategy, Component } from '@angular/core';

import { settings } from '../models/settings';

/** Tiny branding footer: "Powered by Indice ♥ vX.Y.Z". Hosts control spacing/borders. */
@Component({
  selector: 'app-powered-by',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="text-center text-[0.68rem] text-base-content/45">
      Powered by
      <a
        href="https://www.indice.gr"
        target="_blank"
        rel="noopener"
        class="font-medium text-base-content/60 hover:text-primary"
      >Indice</a>
      <span title="love" class="text-error"> ♥ </span>
      <span class="font-mono">v{{ version }}</span>
    </div>
  `,
})
export class PoweredByComponent {
  protected readonly version = settings.version;
}
