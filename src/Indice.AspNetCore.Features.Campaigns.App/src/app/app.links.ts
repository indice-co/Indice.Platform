import { ExternalNavLink, IAppLinks, NavLink } from "@indice/ng-components";

export class AppLinks implements IAppLinks {
    constructor() { }

    public public: NavLink[] = [];
    public profileActions: NavLink[] = [];

    public main: NavLink[] = [
        new NavLink('Αρχική', '/', true),
        new NavLink('Campaigns', '/app/campaigns', true)
    ];

    public profile: NavLink[] = [
        new NavLink('Προφίλ', '/profile', true),
        new NavLink('Ρυθμίσεις', '/settings', false),
        new NavLink('Αποσύνδεση', '/logout', false)
    ];

    public legal: NavLink[] = [
        new ExternalNavLink('Ιδιωτικό Απόρρητο', '/privacy'),
        new ExternalNavLink('Όροι χρήσης', '/terms'),
        new ExternalNavLink('Επικοινωνία', '/contact')
    ];

    public brand: NavLink[] = [
        new ExternalNavLink('Indice', 'https://www.indice.gr')
    ];
}
