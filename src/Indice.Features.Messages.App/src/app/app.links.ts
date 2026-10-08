import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { settings } from './core/models/settings';

export class AppLinks implements IAppLinks {
  constructor() { }

  public public: Observable<NavLink[]> = of([]);
  public profileActions: Observable<NavLink[]> = of([]);

  private _mainLInks = [
    new NavLink('Home', 'dashboard', false, false, 'ms-Icon ms-Icon--BIDashboard'),
    new NavLink('Campaigns', '/campaigns', false, false, 'ms-Icon ms-Icon--Communications'),
    new NavLink('Message Types', '/message-types', false, false, 'ms-Icon ms-Icon--SingleBookmark'),
    new NavLink('Distribution Lists', '/distribution-lists', false, false, 'ms-Icon ms-Icon--ContactList'),
    new NavLink('Contacts', '/contacts', false, false, 'ms-Icon ms-Icon--Contact'),
    new NavLink('Templates', '/templates', false, false, 'ms-Icon ms-Icon--CampaignTemplate'),
    new NavLink('Media', '/media', false, false, 'ms-Icon ms-Icon--PhotoVideoMedia'),
    new NavLink('Events', '/message-events', false, false, 'ms-Icon ms-Icon--SetAction'),
    new NavLink('Settings', '/settings', false, false, 'ms-Icon ms-Icon--Settings')
  ];
  public main: Observable<NavLink[]> = of(settings.enableMediaLibrary ? this._mainLInks : this._mainLInks.filter((l) => l.path !== '/media'));

  public profile: Observable<NavLink[]> = of([
    new NavLink('Logout', '/logout', false)
  ]);

  public legal: Observable<NavLink[]> = of([
    new ExternalNavLink('Privacy Policy', '/privacy'),
    new ExternalNavLink('Terms of Use', '/terms'),
    new ExternalNavLink('Contact', '/contact')
  ]);

  public brand: Observable<NavLink[]> = of([
    new ExternalNavLink('Indice', 'https://www.indice.gr')
  ]);
}
