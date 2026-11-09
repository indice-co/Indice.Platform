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

/**
 * Human readable duration, e.g. `1 day 2 hr 5 min`. `format` turns a `Clients.Duration.*` translation key and a count
 * into text (usually `TranslateService.instant`).
 */
export function formatDuration(totalSeconds: number | null | undefined, format: (key: string, count: number) => string): string {
  if (totalSeconds == null || isNaN(totalSeconds) || totalSeconds <= 0) {
    return '';
  }
  const parts: string[] = [];
  const units: [number, string][] = [[86400, 'Clients.Duration.Day'], [3600, 'Clients.Duration.Hour'], [60, 'Clients.Duration.Minute'], [1, 'Clients.Duration.Second']];
  let rest = Math.floor(totalSeconds);
  for (const [size, key] of units) {
    const value = Math.floor(rest / size);
    if (value > 0) {
      parts.push(format(key === 'Clients.Duration.Day' && value > 1 ? 'Clients.Duration.Days' : key, value));
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

/** `name`, `flow` and `description` hold translation keys. */
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
    key: ClientType.SPA, name: 'Clients.Types.Spa.Name', flow: 'Clients.Types.Spa.Flow', icon: LucideAppWindow,
    description: ['Clients.Types.Spa.Description1', 'Clients.Types.Spa.Description2', 'Clients.Types.Spa.Description3'],
    steps: ['details', 'urls', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.WebApp, name: 'Clients.Types.WebApp.Name', flow: 'Clients.Types.WebApp.Flow', icon: LucideGlobe,
    description: ['Clients.Types.WebApp.Description1', 'Clients.Types.WebApp.Description2', 'Clients.Types.WebApp.Description3'],
    steps: ['details', 'urls', 'secrets', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.Native, name: 'Clients.Types.Native.Name', flow: 'Clients.Types.Native.Flow', icon: LucideMonitorSmartphone,
    description: ['Clients.Types.Native.Description1', 'Clients.Types.Native.Description2', 'Clients.Types.Native.Description3'],
    steps: ['details', 'urls', 'secrets', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.Machine, name: 'Clients.Types.Machine.Name', flow: 'Clients.Types.Machine.Flow', icon: LucideServer,
    description: ['Clients.Types.Machine.Description1', 'Clients.Types.Machine.Description2', 'Clients.Types.Machine.Description3'],
    steps: ['basic', 'api', 'secrets', 'summary']
  },
  {
    key: ClientType.Device, name: 'Clients.Types.Device.Name', flow: 'Clients.Types.Device.Flow', icon: LucideCpu,
    description: ['Clients.Types.Device.Description1', 'Clients.Types.Device.Description2'],
    steps: ['basic', 'identity', 'api', 'summary']
  },
  {
    key: ClientType.SPALegacy, name: 'Clients.Types.SpaLegacy.Name', flow: 'Clients.Types.SpaLegacy.Flow', icon: LucideTabletSmartphone,
    description: ['Clients.Types.SpaLegacy.Description1', 'Clients.Types.SpaLegacy.Description2', 'Clients.Types.SpaLegacy.Description3'],
    steps: ['details', 'urls', 'identity', 'api', 'summary']
  }
];

/** Translation keys of the wizard step titles. */
export const STEP_TITLES: Record<WizardStep, string> = {
  details: 'Common.Details',
  basic: 'Common.Details',
  urls: 'Clients.Tabs.Urls',
  secrets: 'Clients.Tabs.Secrets',
  identity: 'Clients.Resources.IdentityResources',
  api: 'Clients.Add.ApiAccess',
  summary: 'Clients.Add.Summary'
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
