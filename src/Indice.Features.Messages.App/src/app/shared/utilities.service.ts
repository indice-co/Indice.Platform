import { Injectable } from '@angular/core';

import { ValidationProblemDetails } from '../core/services/messages-api.service';

@Injectable({
  providedIn: 'root'
})
export class UtilitiesService {
  constructor() { }

  public isValidJson(value: string): boolean {
    if (!value || value === '') {
      return true;
    }
    try {
      JSON.parse(value);
      return true;
    } catch {
      return false;
    }
  }

  public getValidationErrors(problemDetails: ValidationProblemDetails): string[] {
    const errors = this._getValidationErrors(problemDetails);
    if (errors.length === 0 && problemDetails.title) {
      return new Array<string>(1).fill(problemDetails.title);
    }
    return errors;
  }

  private _getValidationErrors(problemDetails: ValidationProblemDetails): string[] {
    const errorMessages: string[] = [];
    const errors = problemDetails && problemDetails.errors;
    for (const property in errors) {
      if (property) {
        const propertyMessages = errors[property];
        propertyMessages.forEach((message: string) => errorMessages.push(message));
      } else {
        Object.values(errors || {}).forEach((messages: string[]) => {
          messages.forEach((message: string) => messages.push(message));
        });
      }
    }
    return errorMessages;
  }
}
