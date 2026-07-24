/** A primary navigation entry rendered in the top bar. `icon` is SVG path data (24×24, stroked). */
export interface NavItem {
  label: string;
  path: string;
  exact: boolean;
  icon: string;
}

/**
 * Global navigation. Mirrors the Indice `NavLink` information architecture (label + route + icon +
 * active state).
 */
export const NAV_ITEMS: readonly NavItem[] = [
  {
    label: 'Chat',
    path: '/',
    exact: true,
    icon: 'M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z',
  },
  {
    label: 'Profile',
    path: '/profile',
    exact: false,
    icon: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM5 20a7 7 0 0 1 14 0',
  },
];
