import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { SignInLocationSet } from 'src/app/core/services/identity-api.service';

interface MapPoint {
  key: string;
  left: string;
  top: string;
  size: string;
  opacity: number;
  title: string;
}

// Robinson projection coefficients, every 5 degrees of latitude.
const ROBINSON_X = [1, 0.9986, 0.9954, 0.99, 0.9822, 0.973, 0.96, 0.9427, 0.9216, 0.8962, 0.8679, 0.835, 0.7986, 0.7597, 0.7186, 0.6732, 0.6213, 0.5722, 0.5322];
const ROBINSON_Y = [0, 0.062, 0.124, 0.186, 0.248, 0.31, 0.372, 0.434, 0.4958, 0.5571, 0.6176, 0.6769, 0.7346, 0.7903, 0.8435, 0.8936, 0.9394, 0.9761, 1];

/**
 * Sign in locations plotted as bubbles on a Robinson world map, plus a per-country ranking.
 * The map outline is used as a CSS mask so it takes the theme's surface color.
 */
@Component({
  selector: 'app-sign-in-locations-map',
  imports: [DecimalPipe],
  host: { class: 'grid gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]' },
  template: `
    <div class="relative aspect-[2048/1039] w-full">
      <div class="absolute inset-0 bg-base-300"
        style="mask: url(assets/img/robinson-world-map.svg) center / contain no-repeat; -webkit-mask: url(assets/img/robinson-world-map.svg) center / contain no-repeat">
      </div>
      @for (point of points(); track point.key) {
        <span class="absolute -translate-x-1/2 -translate-y-1/2 rounded-full border border-primary bg-primary ring-2 ring-base-100/70"
          [style.left]="point.left" [style.top]="point.top" [style.width]="point.size" [style.height]="point.size"
          [style.opacity]="point.opacity" [attr.title]="point.title">
        </span>
      }
    </div>
    <ol class="space-y-2.5 self-start">
      @for (country of countries(); track country.countryCode) {
        <li class="space-y-1">
          <div class="flex items-center gap-2 text-sm">
            <span class="flag" [class]="'flag:' + country.countryCode" [attr.title]="country.countryCode"></span>
            <span class="min-w-0 flex-1 truncate">{{ country.displayName }}</span>
            <span class="font-medium tabular">{{ country.count | number }}</span>
          </div>
          <progress class="progress progress-primary h-1.5 w-full" [value]="country.count" [max]="maxCountry()"></progress>
        </li>
      }
    </ol>
  `
})
export class SignInLocationsMapComponent {
  public readonly locations = input<SignInLocationSet | undefined>();

  protected readonly countries = computed(() => (this.locations()?.countryLegend ?? []).slice(0, 8));
  protected readonly maxCountry = computed(() => Math.max(1, ...this.countries().map(c => c.count || 0)));
  protected readonly points = computed<MapPoint[]>(() => {
    const items = this.locations()?.items ?? [];
    const maxCount = Math.max(1, ...items.map(item => item.count || 0));
    return items
      .map((item, index) => {
        const coordinates = parseCoordinates(item.location);
        if (!coordinates) {
          return undefined;
        }
        const projected = projectRobinson(coordinates.latitude, coordinates.longitude);
        const intensity = Math.max(0, Math.min(1, (item.count || 0) / maxCount));
        return {
          key: `${index}-${item.countryCode}-${item.displayName}`,
          left: `${projected.x}%`,
          top: `${projected.y}%`,
          size: `${6 + Math.sqrt(intensity) * 20}px`,
          opacity: 0.35 + intensity * 0.5,
          title: `${item.displayName} (${item.countryCode}) · ${item.count}`
        };
      })
      .filter((point): point is MapPoint => !!point);
  });
}

function parseCoordinates(value: string | undefined): { latitude: number; longitude: number } | undefined {
  const [latitude, longitude] = (value ?? '').split(',').map(x => Number(x.trim()));
  if (!value || Number.isNaN(latitude) || Number.isNaN(longitude)) {
    return undefined;
  }
  return { latitude: Math.max(-90, Math.min(90, latitude)), longitude: ((longitude + 540) % 360) - 180 };
}

function projectRobinson(latitude: number, longitude: number): { x: number; y: number } {
  const absLat = Math.abs(latitude);
  const index = Math.min(Math.floor(absLat / 5), ROBINSON_X.length - 1);
  const nextIndex = Math.min(index + 1, ROBINSON_X.length - 1);
  const t = (absLat - index * 5) / 5;
  const xCoeff = ROBINSON_X[index] + (ROBINSON_X[nextIndex] - ROBINSON_X[index]) * t;
  const yCoeff = ROBINSON_Y[index] + (ROBINSON_Y[nextIndex] - ROBINSON_Y[index]) * t;
  const x = 50 + (longitude / 180) * xCoeff * 47;
  const y = 50 + (latitude >= 0 ? -1 : 1) * yCoeff * 48.5;
  return { x: Math.max(0.5, Math.min(99.5, x)), y: Math.max(0.5, Math.min(99.5, y)) };
}
