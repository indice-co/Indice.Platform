import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { SignInLocationSet } from 'src/app/core/services/identity-api.service';

interface MapPoint {
    key: string;
    countryCode: string;
    displayName: string;
    count: number;
    left: string;
    top: string;
    size: string;
    color: string;
    title: string;
}

@Component({
    selector: 'app-sign-in-locations-map',
    templateUrl: './sign-in-locations-map.component.html',
    styleUrls: ['./sign-in-locations-map.component.scss'],
    standalone: false
})
export class SignInLocationsMapComponent implements OnChanges {
    @Input() locations: SignInLocationSet;

    public points: MapPoint[] = [];

    private static readonly ROBINSON_X = [1, 0.9986, 0.9954, 0.99, 0.9822, 0.973, 0.96, 0.9427, 0.9216, 0.8962, 0.8679, 0.835, 0.7986, 0.7597, 0.7186, 0.6732, 0.6213, 0.5722, 0.5322];
    private static readonly ROBINSON_Y = [0, 0.062, 0.124, 0.186, 0.248, 0.31, 0.372, 0.434, 0.4958, 0.5571, 0.6176, 0.6769, 0.7346, 0.7903, 0.8435, 0.8936, 0.9394, 0.9761, 1];

    public ngOnChanges(changes: SimpleChanges): void {
        if (changes.locations) {
            this.rebuildPoints();
        }
    }

    public trackByPoint(_index: number, point: MapPoint): string {
        return point.key;
    }

    private rebuildPoints(): void {
        const items = this.locations?.items ?? [];
        if (!items.length) {
            this.points = [];
            return;
        }

        const maxCount = Math.max(...items.map(item => item.count || 0), 1);

        this.points = items
            .map((item, index) => {
                const coordinates = this.parseCoordinates(item.location);
                if (!coordinates) {
                    return undefined;
                }

                const projected = this.projectRobinson(coordinates.latitude, coordinates.longitude);
                const intensity = Math.max(0, Math.min(1, (item.count || 0) / maxCount));
                const diameter = 6 + (Math.sqrt(intensity) * 20);
                const lightness = Math.round(68 - (intensity * 30));

                return {
                    key: `${index}-${item.countryCode}-${item.displayName}`,
                    countryCode: item.countryCode,
                    displayName: item.displayName,
                    count: item.count,
                    left: `${projected.x}%`,
                    top: `${projected.y}%`,
                    size: `${diameter}px`,
                    color: `hsl(214 90% ${lightness}%)`,
                    title: `${item.displayName} (${item.countryCode}) • ${item.count}`
                } as MapPoint;
            })
            .filter((point): point is MapPoint => !!point)
            .sort((a, b) => b.count - a.count);
    }

    private parseCoordinates(value: string): { latitude: number; longitude: number } | undefined {
        if (!value) {
            return undefined;
        }

        const [latitudeRaw, longitudeRaw] = value.split(',').map(x => x.trim());
        const latitude = Number(latitudeRaw);
        const longitude = Number(longitudeRaw);

        if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
            return undefined;
        }

        return {
            latitude: Math.max(-90, Math.min(90, latitude)),
            longitude: this.normalizeLongitude(longitude)
        };
    }

    private projectRobinson(latitude: number, longitude: number): { x: number; y: number } {
        const absLat = Math.abs(latitude);
        const index = Math.min(Math.floor(absLat / 5), SignInLocationsMapComponent.ROBINSON_X.length - 1);
        const nextIndex = Math.min(index + 1, SignInLocationsMapComponent.ROBINSON_X.length - 1);
        const t = (absLat - (index * 5)) / 5;

        const xCoeff = this.interpolate(SignInLocationsMapComponent.ROBINSON_X[index], SignInLocationsMapComponent.ROBINSON_X[nextIndex], t);
        const yCoeff = this.interpolate(SignInLocationsMapComponent.ROBINSON_Y[index], SignInLocationsMapComponent.ROBINSON_Y[nextIndex], t);

        const xNormalized = (longitude / 180) * xCoeff;
        const yNormalized = (latitude >= 0 ? -1 : 1) * yCoeff;

        const x = 50 + (xNormalized * 47);
        const y = 50 + (yNormalized * 48.5);

        return {
            x: Math.max(0.5, Math.min(99.5, x)),
            y: Math.max(0.5, Math.min(99.5, y))
        };
    }

    private interpolate(a: number, b: number, t: number): number {
        return a + ((b - a) * t);
    }

    private normalizeLongitude(longitude: number): number {
        let normalized = longitude;
        while (normalized > 180) {
            normalized -= 360;
        }
        while (normalized < -180) {
            normalized += 360;
        }
        return normalized;
    }
}
