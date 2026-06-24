import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

/** Generic placeholder for routes that aren't built yet (Profile, Flow builder). Title comes from route data. */
@Component({
  selector: 'app-coming-soon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="dex-canvas flex h-full flex-col items-center justify-center gap-5 px-6 text-center">
      <img src="dex-logo.png" alt="Dex" class="size-20 opacity-90 drop-shadow-sm" />
      <div>
        <p class="font-mono text-xs uppercase tracking-[0.22em] text-primary">Coming soon</p>
        <h1 class="mt-2 text-3xl font-semibold tracking-tight text-base-content">{{ title }}</h1>
        <p class="mt-2 max-w-sm text-base-content/55">
          This area is on the Dex roadmap. Check back soon.
        </p>
      </div>
      <a routerLink="/" class="btn btn-primary btn-sm">Back to chat</a>
    </div>
  `,
})
export class ComingSoonComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly title =
    (this.route.snapshot.data['title'] as string | undefined) ?? 'Coming soon';
}
