import { LucideAppWindow, LucideCpu, LucideGlobe, LucideIcon, LucideMonitorSmartphone, LucideServer, LucideTabletSmartphone } from '@lucide/angular';
import { ClientType } from 'src/app/core/services/identity-api.service';

/** Loose URL check kept from the previous UI: scheme optional, `localhost` or a dotted host, optional port and path. */
export const URL_PATTERN = /(?:^|\s)((https?:\/\/)?(?:localhost|[\w-]+(?:\.[\w-]+)+)(:\d+)?(\/\S*)?)/;

/** A valid CORS origin: scheme, host and optional port, nothing else. */
export const ORIGIN_PATTERN = /^https?:\/\/(?=.{1,254}(?::|$))(?:(?!\d|-)(?![a-z0-9\-]{1,62}-(?:\.|:|$))[a-z0-9\-]{1,63}\b(?!\.$)\.?)+(:\d+)?$/i;

export const DEVICE_CODE = 'urn:ietf:params:oauth:grant-type:device_code';

export const KNOWN_GRANT_TYPES = ['authorization_code', 'client_credentials', 'delegation', 'hybrid', 'implicit', 'password', 'refresh_token', DEVICE_CODE];

/** Cultures a client name and description can be translated to. The first one is stored on the client itself. */
export const CLIENT_CULTURES = ['EL', 'EN'];

export function hasAnyGrant(grantTypes: string[] | undefined, types: string[]): boolean {
  return !!grantTypes?.some(x => types.includes(x));
}

export function hasAllGrants(grantTypes: string[] | undefined, types: string[]): boolean {
  return types.every(x => grantTypes?.includes(x));
}

/** Clients that only talk machine to machine have no use for identity resources. */
export function isMachineOnly(grantTypes: string[] | undefined): boolean {
  return grantTypes?.length === 1 && grantTypes[0] === 'client_credentials';
}

/** Grant types that involve a browser redirect and therefore need application URLs. */
export function usesRedirects(grantTypes: string[] | undefined): boolean {
  return hasAnyGrant(grantTypes, ['authorization_code', 'password', 'hybrid', 'implicit', DEVICE_CODE]);
}

/** Human readable duration, e.g. `1 day 2 hr 5 min`. */
export function formatDuration(totalSeconds: number | null | undefined): string {
  if (totalSeconds == null || isNaN(totalSeconds) || totalSeconds <= 0) {
    return '';
  }
  const parts: string[] = [];
  const units: [number, string][] = [[86400, 'day'], [3600, 'hr'], [60, 'min'], [1, 'sec']];
  let rest = Math.floor(totalSeconds);
  for (const [size, label] of units) {
    const value = Math.floor(rest / size);
    if (value > 0) {
      parts.push(`${value} ${label}${label === 'day' && value > 1 ? 's' : ''}`);
      rest -= value * size;
    }
  }
  return parts.join(' ');
}

export function newGuid(): string {
  return crypto.randomUUID?.() ?? 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, char => {
    const random = Math.random() * 16 | 0;
    return (char === 'x' ? random : (random & 0x3 | 0x8)).toString(16);
  });
}

/** A random, url safe secret value. */
export function newSecret(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return btoa(String.fromCharCode(...bytes)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/** Saves a blob through a temporary download link. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export type WizardStep = 'details' | 'basic' | 'urls' | 'secrets' | 'identity' | 'api' | 'summary';

export interface ClientTypeOption {
  key: ClientType;
  name: string;
  flow: string;
  description: string[];
  icon: LucideIcon;
  steps: WizardStep[];
}

/** The application kinds offered by the "new client" wizard, with the wizard steps each one needs. */
export const CLIENT_TYPES: ClientTypeOption[] = [
  {
    key: ClientType.SPA, name: 'Single page app', flow: 'Authorization code + PKCE', icon: LucideAppWindow,
    description: ['A client side application running in a browser.', 'Cannot be trusted to keep secrets or use refresh tokens.', 'Tokens travel through the browser and are visible to the user.'],
    steps: ['details', 'urls', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.WebApp, name: 'Web app', flow: 'Hybrid flow with client authentication', icon: LucideGlobe,
    description: ['A server side application running on your infrastructure.', 'Can keep a secret and use refresh tokens.', 'Tokens are sent to a back-end server and are not visible to the user.'],
    steps: ['details', 'urls', 'secrets', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.Native, name: 'Native', flow: 'Authorization code + PKCE', icon: LucideMonitorSmartphone,
    description: ['A desktop or mobile application running on a user\'s device.', 'Cannot keep a secret but can use refresh tokens.', 'Token visibility depends on the browser and redirect URI.'],
    steps: ['details', 'urls', 'secrets', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.Machine, name: 'Machine', flow: 'Client credentials', icon: LucideServer,
    description: ['Machine to machine communication.', 'No users are involved.', 'Tokens are sent over a back channel.'],
    steps: ['basic', 'api', 'secrets', 'summary']
  },
  {
    key: ClientType.Device, name: 'Device', flow: 'Device flow with an external browser', icon: LucideCpu,
    description: ['An IoT application or another browserless or input constrained device.', 'Usually cannot keep a secret but can use refresh tokens.'],
    steps: ['basic', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.SPALegacy, name: 'Single page app (legacy)', flow: 'Implicit flow', icon: LucideTabletSmartphone,
    description: ['A browser application following older OAuth working group recommendations.', 'Cannot keep secrets or use refresh tokens.', 'Tokens travel through the browser and are visible to the user.'],
    steps: ['details', 'urls', 'identity', 'api', 'summary']
  }
];

export const STEP_TITLES: Record<WizardStep, string> = {
  details: 'Details',
  basic: 'Details',
  urls: 'URLs',
  secrets: 'Secrets',
  identity: 'Identity resources',
  api: 'API access',
  summary: 'Summary'
};

/**
 * Grant type combinations IdentityServer is known to support well, carried over from the previous UI.
 * Used to warn (not block) when adding a grant type that does not pair with the ones a client already has.
 */
const GRANT_TRANSITIONS: Record<string, string[]> = {
  authorization_code: ['client_credentials', 'password', 'custom'],
  client_credentials: ['authorization_code', 'implicit', 'password', 'hybrid', 'custom', DEVICE_CODE],
  hybrid: ['client_credentials', 'password', 'custom'],
  password: ['implicit', 'authorization_code', 'hybrid', 'client_credentials', 'custom'],
  implicit: ['client_credentials', 'password', 'custom'],
  [DEVICE_CODE]: ['client_credentials', 'custom']
};

/** True when `to` is a known good companion of every known grant type in `owned`. */
export function isRecommendedCombination(owned: string[], to: string): boolean {
  const target = KNOWN_GRANT_TYPES.includes(to) ? to : 'custom';
  return owned
    .filter(from => GRANT_TRANSITIONS[from])
    .every(from => GRANT_TRANSITIONS[from].includes(target) || ['refresh_token', 'delegation'].includes(to));
}
