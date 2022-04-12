import { Injectable } from '@angular/core';

import { ValidationProblemDetails } from '../core/services/campaigns-api.services';

@Injectable({
  providedIn: 'root'
})
export class UtilitiesService {
  constructor() { }

  public problemDetails?: ValidationProblemDetails;

  public get validationErrors() {
    const messages: string[] = [];
    const errors = this.problemDetails && this.problemDetails.errors;
    for (const property in errors) {
      if (property) {
        const propertyMessages = errors[property];
        propertyMessages.forEach((message: string) => {
          messages.push(message);
        });
      } else {
        Object.values(errors || {}).forEach(message => {
          message.forEach(x => {
            messages.push(x);
          });
        });
      }
    }
    return messages;
  }

  public getValidationProblemDetails(problemDetails: ValidationProblemDetails): string[] {
    this.problemDetails = problemDetails;
    if (this.validationErrors.length === 0 && this.problemDetails.title !== null && this.problemDetails.title !== undefined) {
      return new Array<string>(1).fill(this.problemDetails.title);
    }
    return this.validationErrors;
  }
}
