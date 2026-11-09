import { ChatMessageContent, ChatMessagePart, DexChatMessage, DexChatResponse, DexChatRole, ICitation, IChatMessageContent } from '../../core/services/dex-api.service';

/** The two roles we render in the conversation thread. */
export type ChatTurnRole = 'User' | 'Assistant';

/**
 * View model for a single turn in the thread. Mirrors the API's DexChatMessage but also carries the
 * citations that arrive as streaming tail patches (the persisted history does not include them).
 */
export interface ThreadMessage {
  messageId?: string;
  role: ChatTurnRole;
  content: IChatMessageContent;
  createdAt?: Date;
  citations?: ICitation[];
  /** User feedback on the assistant message: true liked, false disliked, null/undefined none. */
  liked?: boolean | null;
}

/** Sentinel id of a never-persisted message (e.g. the limit-blocked reply) — carries no likeable identity. */
const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/** Map an API DexChatMessage (session history) to a thread view model. */
export function toThreadMessage(message: DexChatMessage): ThreadMessage {
  return {
    messageId: message.messageId && message.messageId !== EMPTY_GUID ? message.messageId : undefined,
    role: message.role === DexChatRole.User ? 'User' : 'Assistant',
    content: message.content ?? new ChatMessageContent({ parts: [new ChatMessagePart({ value: '', contentType: 'text/markdown' }) ] }),
    createdAt: message.createdAt,
    citations: message.citations ?? [],
    liked: message.liked,
  };
}

/**
 * Project the DexChatResponse a stream is assembling into a thread view model — its first message
 * is the assistant answer. Returns `null` until the message skeleton patch has arrived.
 */
export function responseToThreadMessage(response: DexChatResponse | null): ThreadMessage | null {
  const message = response?.messages?.[0];
  return message ? toThreadMessage(message) : null;
}

/** Starter prompts shown on the empty conversation canvas. */
export const EXAMPLE_PROMPTS: readonly string[] = [
  'What can you help me with?',
  'Summarise the onboarding process.',
  'How do I reset my password?',
];
