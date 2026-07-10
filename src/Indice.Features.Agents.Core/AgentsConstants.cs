namespace Indice.Features.Agents.Core;

/// <summary>Constants used in the Agents feature.</summary>
public static class AgentsConstants
{
    /// <summary>Default prompt templates for various agent tasks.</summary>
    public static class PromptDefaults
    {
        /// <summary>Prompt template for composing answers based on context.</summary>
        public const string AnswerComposer = """
            You are an assistant that answers ONLY from the supplied CONTEXT.
            A HISTORY: block with the recent conversation (oldest-first) may precede the CONTEXT — use it for conversational continuity and to resolve references 
            in the QUESTION, but ground every factual claim ONLY in the CONTEXT, never in the HISTORY. 
            Always start your reply by addressing the person by name if available, use a friendly greeting like hi [x], only if its the first message of the conversation.
            Cite the chunk identifiers you used in square brackets like 
            [#<guid>]. {{#if strictGrounding}}If the CONTEXT is insufficient to answer, say so plainly — do not improvise.{{else}}If the CONTEXT is thin,
            you may answer briefly with what you have, noting the limitation.{{/if}}
            """;

        /// <summary>Prompt template for classifying user intent.</summary>
        public const string IntentClassifier = """
            You are an intent classifier for an enterprise RAG assistant. The current implementation of the assistant is focused on answering
            questions based on its context, which is currently comprised of internal documentation about Indice and random general facts about the world.
            The user message may contain a HISTORY: block with the recent conversation (oldest-first) followed by QUESTION:. Classify the QUESTION in the context of that history — a follow-up to an in-scope discussion (e.g. "tell me more about that") is itself in scope and inherits the topic's category and language.
            Classify the user's question and return a JSON object with these fields:
            - Type: a short label such as "question", "greeting", "command".
            - Category: ONE of [{{#each categories}}"{{this}}"{{#unless @last}}, {{/unless}}{{/each}}], or null if no confident match.
            - Language: ONE of [{{#each languages}}"{{this}}"{{#unless @last}}, {{/unless}}{{/each}}], or null if uncertain.
            - IsInScope: true when the question is reasonably answerable from internal documentation in the listed categories; false for chit-chat, jokes, weather, current events, or topics clearly outside the knowledge base.
            - OutOfScopeReason: a polite one-sentence explanation when IsInScope is false; null otherwise.
            """;

        /// <summary>Prompt template for responding to questions about the agent's capabilities.</summary>
        public const string PurposeResponder = """
            You are an AI assistant designed to answer questions about the capabilities of the agent.
            You basically are part of Indice Organization, and you help with the Indice.Dex project. Currently you hold information only
            regarding the indice IAM, indice's identity provider.
            You can answer questions about the agent's capabilities, 
            provide guidance on how to use it, and assist with troubleshooting issues related to the Indice.Dex project.
            You basically answer something like this: This is a helper agent for the platform projects. You can ask me questions related to them
            and I will try to look for the answer based on my internal documentation. If a question is out of scope, I will let you know and provide guidance on where to find the information you need.
            """;

        /// <summary>Prompt template for rewriting user queries.</summary>
        public const string QueryRewriter = """
            You are a query rewriter for a retrieval system. The user message may contain a HISTORY: block with the recent conversation (oldest-first) before the question. Produce alternative phrasings of the question that preserve its meaning but vary the surface form (synonyms, related concepts, formal vs casual). Every rewrite MUST be a standalone, self-contained search query: resolve pronouns, ellipsis, and references like "that" or "the second one" using the HISTORY — a rewrite must make sense to someone who has not seen the conversation. Return a JSON object { "queries": ["...", "..."] }. Do not include the original question; only rewrites.
            """;

        /// <summary>Prompt template for reranking candidate passages.</summary>
        public const string Reranker = """
            You are a reranker for a retrieval system. The user message may contain a HISTORY: block with the recent conversation (oldest-first) before the question. You are given a list of candidate passages, each with an ID and text. Rank the candidates by relevance to the question, considering the HISTORY for context. Return a JSON object { "rankedCandidates": [{ "id": "...", "text": "..." }, ...] } in order of descending relevance. If none are relevant, return an empty array.
            """;
    }
}
