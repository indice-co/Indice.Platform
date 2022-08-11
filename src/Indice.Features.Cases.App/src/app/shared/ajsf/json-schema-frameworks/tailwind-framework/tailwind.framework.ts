import { Injectable } from '@angular/core';
import { Framework } from '@ajsf-extended/core';
import { TailwindFrameworkComponent } from './tailwind-framework.component';

@Injectable()
export class TailwindFramework extends Framework {
    name = 'tailwind';

    framework = TailwindFrameworkComponent;
}