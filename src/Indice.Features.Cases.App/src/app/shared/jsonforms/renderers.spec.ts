import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, reflectComponentType } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { JsonFormsModule } from '@jsonforms/angular';
import { provideIcons } from '@ng-icons/core';
import { lucideFilePlus2, lucideSave } from '@ng-icons/lucide';
import { provideTranslateService } from '@ngx-translate/core';
import { caseFormRenderers } from './renderers';

@Component({
    imports: [JsonFormsModule],
    template: `<jsonforms [schema]="schema" [uischema]="uischema" [data]="data" [renderers]="renderers" [readonly]="readonly" />`
})
class HostComponent {
    renderers = caseFormRenderers;
    readonly = false;
    data: any = { kind: 'b' };
    schema = {
        type: 'object',
        required: ['name'],
        properties: {
            name: { type: 'string', title: 'Name' },
            agree: { type: 'boolean', title: 'I agree' },
            kind: { type: 'string', enum: ['a', 'b'] }
        }
    };
    uischema = {
        type: 'VerticalLayout',
        elements: [
            { type: 'Control', scope: '#/properties/name' },
            { type: 'Control', scope: '#/properties/agree' },
            { type: 'Control', scope: '#/properties/kind', options: { htmlClass: 'custom-layout-class' } },
            { type: 'Submit' }
        ]
    };
}

describe('caseFormRenderers', () => {
    it('registers standalone renderer components', () => {
        expect(caseFormRenderers.length).toBeGreaterThan(0);
        for (const entry of caseFormRenderers) {
            expect(reflectComponentType(entry.renderer)?.isStandalone).toBe(true);
        }
    });

    describe('rendering a form', () => {
        beforeEach(() => {
            TestBed.configureTestingModule({
                imports: [HostComponent],
                providers: [
                    provideHttpClient(),
                    provideHttpClientTesting(),
                    provideTranslateService(),
                    provideIcons({ lucideSave, lucideFilePlus2 })
                ]
            });
        });

        it('draws labels, the required marker, layout classes and the submit button', () => {
            const fixture = TestBed.createComponent(HostComponent);
            fixture.detectChanges();
            const element: HTMLElement = fixture.nativeElement;

            const label = element.querySelector('label.jf-label');
            expect(label?.textContent).toContain('Name');
            expect(label?.querySelector('.jf-required')).not.toBeNull();
            expect(element.querySelector('input.input[type="text"]')).not.toBeNull();
            expect(element.querySelector('input.checkbox[type="checkbox"]')).not.toBeNull();
            const select = element.querySelector<HTMLSelectElement>('.custom-layout-class select.select');
            expect(select?.options.length).toBe(3);
            expect(select?.options[select.selectedIndex].textContent).toBe('b');
            expect(element.querySelector('.jf-submit button')).not.toBeNull();
        });

        it('shows read-only fields calmly and hides the submit button', () => {
            const fixture = TestBed.createComponent(HostComponent);
            fixture.componentInstance.readonly = true;
            fixture.detectChanges();
            const element: HTMLElement = fixture.nativeElement;

            expect(element.querySelector('input[type="text"]')?.classList).toContain('jf-readonly');
            expect(element.querySelector('select')?.disabled).toBe(true);
            expect(element.querySelector('.jf-submit button')).toBeNull();
        });
    });
});
