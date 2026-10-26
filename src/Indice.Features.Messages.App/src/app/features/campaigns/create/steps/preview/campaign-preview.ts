import { Hyperlink, Period } from 'src/app/core/services/messages-api.service';

/** What the review step shows, collected from the previous wizard steps. */
export interface CampaignPreview {
  title?: string;
  type?: string;
  typeClassification?: string;
  period?: Period;
  actionLink?: Hyperlink;
  template?: string;
  /** `content` dictionary keys. */
  channels?: string[];
  /** Translation key of the chosen recipients option. */
  recipientsMode?: string;
  /** Distribution list name or number of recipients, depending on the mode. */
  recipientsDetail?: string;
  attachment?: string;
}
