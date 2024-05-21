import { Injectable } from '@angular/core';

import { NgbDateParserFormatter, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { UtilitiesService } from '../../core/services/utilities.services';

@Injectable()
export class NgbDateCustomParserFormatter extends NgbDateParserFormatter {
    constructor(private utilities: UtilitiesService) {
        super();
    }

    public parse(value: string): NgbDateStruct | null {
        if (value) {
            const dateParts = value.trim().split('-');
            if (dateParts.length === 1 && this.utilities.isNumber(dateParts[0])) {
                return {
                    day: null,
                    month: this.utilities.toInteger(dateParts[0]),
                    year: null
                };
            } else if (dateParts.length === 2 && this.utilities.isNumber(dateParts[0]) && this.utilities.isNumber(dateParts[1])) {
                return {
                    day: this.utilities.toInteger(dateParts[1]),
                    month: this.utilities.toInteger(dateParts[0]),
                    year: null
                };
            } else if (dateParts.length === 3 && this.utilities.isNumber(dateParts[0]) && this.utilities.isNumber(dateParts[1]) && this.utilities.isNumber(dateParts[2])) {
                return {
                    day: this.utilities.toInteger(dateParts[1]),
                    month: this.utilities.toInteger(dateParts[0]),
                    year: this.utilities.toInteger(dateParts[2])
                };
            }
        }
        return null;
    }

    public parseDate(value: Date): NgbDateStruct | null {
        if (!value)
            return null;

        let year = value.getFullYear();
        let month = (1 + value.getMonth()).toString().padStart(2, '0');
        let day = value.getDate().toString().padStart(2, '0');
        return this.parse(`${month}-${day}-${year}`);
    }

    public format(date: NgbDateStruct): string {
        return date ? `${this.utilities.isNumber(date.month) ? this.utilities.padNumber(date.month) : ''}-${this.utilities.isNumber(date.day) ? this.utilities.padNumber(date.day) : ''}-${date.year}` : '';
    }
}
