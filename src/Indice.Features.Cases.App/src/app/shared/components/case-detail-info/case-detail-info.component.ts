import { Component, Input } from "@angular/core";

@Component({
    selector: 'app-case-detail-info',
    templateUrl: './case-detail-info.component.html',
    standalone: false
})
export class CaseDetailInfoComponent {
    @Input() title: string = '';
    @Input() keyValuePairs: {[key: string]: string|undefined|null} | undefined;
}