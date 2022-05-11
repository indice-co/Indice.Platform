import { ChangeDetectionStrategy, Component, OnChanges, OnInit, SimpleChanges, TemplateRef, ViewChild, ViewEncapsulation } from '@angular/core';

@Component({
    selector: 'lib-step',
    template: `
        <ng-template>
            <ng-content></ng-content>
        </ng-template>
    `,
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LibStepComponent implements OnInit, OnChanges {
    constructor() { }

    @ViewChild(TemplateRef, { static: true }) content!: TemplateRef<any>;

    public ngOnInit(): void { }

    public ngOnChanges(changes: SimpleChanges): void { }
}
