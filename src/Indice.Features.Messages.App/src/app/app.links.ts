import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { settings } from './core/models/settings';

export class AppLinks implements IAppLinks {
  constructor() { }

  public public: Observable<NavLink[]> = of([]);
  public profileActions: Observable<NavLink[]> = of([]);

  private _mainLInks = [
    new NavLink('Αρχική', 'dashboard', false, false, 'ms-Icon ms-Icon--BIDashboard'),
    new NavLink('Καμπάνιες', '/campaigns', false, false, 'ms-Icon ms-Icon--Communications'),
    new NavLink('Τύποι Μηνυμάτων', '/message-types', false, false, 'ms-Icon ms-Icon--SingleBookmark'),
    new NavLink('Λίστες Διανομής', '/distribution-lists', false, false, 'ms-Icon ms-Icon--ContactList'),
    new NavLink('Επαφές', '/contacts', false, false, 'ms-Icon ms-Icon--Contact'),
    new NavLink('Πρότυπα', '/templates', false, false, 'ms-Icon ms-Icon--CampaignTemplate'),
    new NavLink('Αρχεία', '/media', false, false, 'ms-Icon ms-Icon--PhotoVideoMedia'),
    new NavLink('Ρυθμίσεις', '/settings', false, false, 'ms-Icon ms-Icon--Settings')
  ];
  public main: Observable<NavLink[]> = of(settings.enableMediaLibrary ? this._mainLInks : this._mainLInks.filter((l) => l.path !== '/media'));

  public profile: Observable<NavLink[]> = of([
    new NavLink('Αποσύνδεση', '/logout', false)
  ]);

  public legal: Observable<NavLink[]> = of([
    new ExternalNavLink('Ιδιωτικό Απόρρητο', '/privacy'),
    new ExternalNavLink('Όροι χρήσης', '/terms'),
    new ExternalNavLink('Επικοινωνία', '/contact')
  ]);

  public brand: Observable<NavLink[]> = of([
    new ExternalNavLink('Indice', 'https://www.indice.gr')
  ]);
}
