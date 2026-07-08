import { ChatMessage, ChatMessageRole, ICitation } from '../../core/services/dex-api.service';

/** The two roles we render in the conversation thread. */
export type ChatTurnRole = 'User' | 'Assistant';

/**
 * View model for a single turn in the thread. Mirrors the API's ChatMessage but also carries the
 * citations that arrive on the streaming `complete` event (the persisted history does not include them).
 */
export interface ThreadMessage {
  role: ChatTurnRole;
  content: string;
  createdAt?: Date;
  citations?: ICitation[];
}

/** Map an API ChatMessage (session history) to a thread view model. */
export function toThreadMessage(message: ChatMessage): ThreadMessage {
  return {
    role: message.role === ChatMessageRole.User ? 'User' : 'Assistant',
    content: message.content ?? '',
    createdAt: message.createdAt,
    citations: [],
  };
}

/** Starter prompts shown on the empty conversation canvas. */
export const EXAMPLE_PROMPTS: readonly string[] = [
  'What can you help me with?',
  'Summarise the onboarding process.',
  'How do I reset my password?',
];
