import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'app-public-shell',
    templateUrl: './public-shell.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class PublicShellComponent { }
