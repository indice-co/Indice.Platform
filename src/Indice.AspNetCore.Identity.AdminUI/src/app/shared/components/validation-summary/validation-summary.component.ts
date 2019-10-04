import { Input, Component } from '@angular/core';

import { ValidationProblemDetails } from 'src/app/core/services/identity-api.service';

@Component({
    selector: 'app-validation-summary',
    templateUrl: './validation-summary.component.html'
})
export class ValidationSummaryComponent {
    @Input() public problemDetails: ValidationProblemDetails;

    public get validationErrors() {
        const messages: string[] = [];
        const errors = this.problemDetails && this.problemDetails.errors;
        for (const property in errors) {
            if (property) {
                const propertyMessages = errors[property];
                propertyMessages.forEach((message: string) => {
                    messages.push(message);
                });
            }
        }
        return messages;
    }

    public clear(): void {
        this.problemDetails = null;
    }
}
