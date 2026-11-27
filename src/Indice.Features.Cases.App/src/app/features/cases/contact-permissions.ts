import { ContactPermission } from 'src/app/core/services/cases-api.service';

export interface ContactPermissionOption {
  value: ContactPermission;
  /** Translation key of the name of the permission. */
  label: string;
  /** Translation key of what the permission adds to the one before it. */
  hint: string;
}

/** What the owner or a co-owner of a case can do as a customer, from the least to the most. */
export const CONTACT_PERMISSIONS: ContactPermissionOption[] = [
  { value: ContactPermission.Reader, label: 'contactPermissions.reader.label', hint: 'contactPermissions.reader.hint' },
  { value: ContactPermission.Contributor, label: 'contactPermissions.contributor.label', hint: 'contactPermissions.contributor.hint' },
  { value: ContactPermission.Owner, label: 'contactPermissions.owner.label', hint: 'contactPermissions.owner.hint' }
];

/** Translation key of the name of a permission. A contact without one is a reader, as on the server. */
export function contactPermissionLabel(permission: ContactPermission | undefined): string {
  return (CONTACT_PERMISSIONS.find(option => option.value === permission) ?? CONTACT_PERMISSIONS[0]).label;
}

/** Badge classes of a permission; the same scale as the access levels of the access rules. */
export function contactPermissionClass(permission: ContactPermission | undefined): string {
  switch (permission) {
    case ContactPermission.Owner: return 'badge-secondary';
    case ContactPermission.Contributor: return 'badge-primary badge-soft';
    default: return 'badge-ghost';
  }
}
