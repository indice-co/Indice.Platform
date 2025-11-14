import { Inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

/**
 * Custom loader that fetches translations from a backend API
 */
@Injectable()
export class ApiTranslateLoader implements TranslateLoader {
  private http: HttpClient;
  private baseUrl: string;
  protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

  constructor(@Inject(HttpClient) http: HttpClient, @Inject(String) baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;
  }


  //here I will set an api call that will Get to the backend and return the data in json format
  //for them to be rendered
  //because we dont want to have the mainting here - but in a resx file in the backend
  getTranslation(lang: string): Observable<Record<string, any>> {
    const url = this.baseUrl + `/messagesUiTranslation.${lang}.json`;
    return this.http.get<Record<string, any>>(url).pipe(
      catchError(err => {
        console.error(`Error loading language "${lang}"`, err);
        // Return empty object on failure (prevents crashes)
        return of({});
      })
    );
  }
}
