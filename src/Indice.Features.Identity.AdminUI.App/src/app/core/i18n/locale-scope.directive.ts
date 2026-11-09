import { Directive } from '@angular/core';
import { provideCurrentLocale } from './app-languages.service';

/**
 * Gives the content `LOCALE_ID` of the language selected when the directive is created. Key it on the current language
 * (e.g. `@for (language of [languages.current()]; track language)`) so a language switch re-creates the content.
 */
@Directive({
  selector: '[appLocaleScope]',
  providers: [provideCurrentLocale()]
})
export class LocaleScopeDirective { }
