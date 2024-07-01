import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, map, of } from 'rxjs';
import { settings } from './core/models/settings';
import { LangChangeEvent, TranslateService } from '@ngx-translate/core';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})

export class AppLinks implements IAppLinks {

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);
    public main: Observable<NavLink[]>;
    public mainLinks: NavLink[] = [];
    public profile: Observable<NavLink[]>;
    public legal: Observable<NavLink[]>;
    public brand: Observable<NavLink[]>;

    constructor(private _translate: TranslateService) {
        this.main = this.initializeMainLinks();
        this.profile = this.initializeProfileLinks();
        this.legal = this.initializeLegalLinks();
        this.brand = this.initializeBrandLinks();

        this._translate.onLangChange.subscribe( () => {
            this.updateLinks();
        });
    }

    private updateLinks() {
        this.mainLinks = [];
        this.main = this.initializeMainLinks();
        this.profile = this.initializeProfileLinks();
        this.legal = this.initializeLegalLinks();
        this.brand = this.initializeBrandLinks();
    }

    private initializeMainLinks(): Observable<NavLink[]> {
        const keys = [
            'general.home',
            'general.campaigns',
            'general.message-types',
            'general.distribution-lists',
            'general.templates',
            'general.files',
            'general.options'
        ];
        return this._translate.get(keys).pipe(
            map(translations => {
                this.mainLinks = [
                    new NavLink(translations[keys[0]], 'dashboard', false, false, Icons.Dashboard),
                    new NavLink(translations[keys[1]], '/campaigns', false, false, Icons.Messages),
                    new NavLink(translations[keys[2]], '/message-types', false, false, Icons.Details),
                    new NavLink(translations[keys[3]], '/distribution-lists', false, false, Icons.TilesView),
                    new NavLink(translations[keys[4]], '/templates', false, false, Icons.SendEmail),
                    new NavLink(translations[keys[5]], '/media', false, false, 'ms-Icon ms-Icon--Folder'),
                    new NavLink(translations[keys[6]], '/settings', false, false, 'ms-Icon ms-Icon--Settings')
                ];
                return settings.enableMediaLibrary ? this.mainLinks : this.mainLinks.filter(link => link.path !== '/media');
            })
        );
    }
   
    private initializeProfileLinks(): Observable<NavLink[]> {
        return this._translate.get('general.logout').pipe(
            map(translation => [new NavLink(translation, '/logout', false)])
        );
    }

    private initializeLegalLinks(): Observable<NavLink[]> {
        const keys = [
            'general.privacy-policy',
            'general.terms-conditions',
            'general.contact'
        ];
        return this._translate.get(keys).pipe(
            map(translations => [
                new ExternalNavLink(translations[keys[0]], '/privacy'),
                new ExternalNavLink(translations[keys[1]], '/terms'),
                new ExternalNavLink(translations[keys[2]], '/contact')
            ])
        );
    }

    private initializeBrandLinks(): Observable<NavLink[]> {
        return of([
            new ExternalNavLink('Indice', 'https://www.indice.gr')
        ]);
    }
}
