import { ChatMessage, ChatMessageContent, ChatMessagePart, ChatMessageRole, ICitation, IChatMessageContent } from '../../core/services/dex-api.service';

/** The two roles we render in the conversation thread. */
export type ChatTurnRole = 'User' | 'Assistant';

/**
 * View model for a single turn in the thread. Mirrors the API's ChatMessage but also carries the
 * citations that arrive on the streaming `complete` event (the persisted history does not include them).
 */
export interface ThreadMessage {
  role: ChatTurnRole;
  content: IChatMessageContent;
  createdAt?: Date;
  citations?: ICitation[];
}

/** Map an API ChatMessage (session history) to a thread view model. */
export function toThreadMessage(message: ChatMessage): ThreadMessage {
  return {
    role: message.role === ChatMessageRole.User ? 'User' : 'Assistant',
    content: message.content ?? new ChatMessageContent({ parts: [new ChatMessagePart({ value: '', contentType: 'text/markdown' }) ] }),
    createdAt: message.createdAt,
    citations: message.citations ?? [],
  };
}

/** Starter prompts shown on the empty conversation canvas. */
export const EXAMPLE_PROMPTS: readonly string[] = [
  'What can you help me with?',
  'Summarise the onboarding process.',
  'How do I reset my password?',
];
