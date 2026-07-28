# Cases Workflow Implementation Guide

## Overview

The **Cases Workflow** is a multi-step OTP verification pipeline integrated into the Agents feature.
It retrieves case data from an external API, verifies user ownership of the case, then hands off to
an **LLM-powered `OtpAgent`** that autonomously drives the full send-and-validate flow using tools
discovered at runtime from the configured MCP server.

> **Key design choice**: There are no hardcoded OTP send or validate steps. The LLM manages the
> entire OTP conversation — asking the user, calling MCP tools, handling retries, and presenting
> the final case card — guided solely by the `CasesOtpAgent` prompt template.

---

## Workflow Steps

### 1. `CaseDataRetriever`
| | |
|---|---|
| **Input** | `CasesWorkflowState` (user message + conversation id + user identifier) |
| **Output** | `CaseRetrievalOutput` |
| **Responsibility** | Calls `IExternalCaseApiService` to retrieve case data as a flexible `JsonNode`. Extracts `CaseId`, `PhoneNumber`, and `Email`. |

### 2. `OwnershipVerifier`
| | |
|---|---|
| **Input** | `CaseRetrievalOutput` |
| **Output** | `OwnershipVerificationOutput` |
| **Responsibility** | Reads the `VerificationField` key from the `JsonNode`, masks the value (email, phone, SSN, card, etc.), and renders the `CasesOwnershipVerifier` prompt template to ask the user to confirm it. The field is configured per case type inside the prompt file — no code change needed. |

### 3. `UserInputValidator`
| | |
|---|---|
| **Input** | `OwnershipVerificationOutput` |
| **Output** | `UserInputValidationOutput` |
| **Responsibility** | Compares the user's answer against the actual case field value. Normalises input per field type (strips formatting from phones/SSNs, lowercases emails). Supports **up to 2 attempts**. |
| **Routing** | `IsValid = true` → `OtpAgent` &nbsp;/&nbsp; `IsValid = false` → `OwnershipVerificationFailureHandler` |

### 4. `OtpAgent` _(LLM-powered — terminal success step)_
| | |
|---|---|
| **Input** | `UserInputValidationOutput` |
| **Output** | `RagPipelineOutput` (streamed answer text) |
| **Responsibility** | Fetches all tools from the `"otp"` MCP server via `IMcpToolsRegistry.GetToolsAsync("otp")` and injects them into `ChatOptions.Tools`. Renders the `CasesOtpAgent` prompt with the phone/email/caseId already known from earlier steps. The LLM then drives the full multi-turn conversation: send OTP → ask for code → validate → present case card. Each token is streamed as an `AnswerDeltaEvent`. |

### 5. `OwnershipVerificationFailureHandler` _(terminal failure step)_
| | |
|---|---|
| **Input** | `UserInputValidationOutput` |
| **Output** | `ValidationFailureOutput` |
| **Responsibility** | Produces a structured failure record when ownership verification is exhausted after 2 attempts. |

---

## Workflow Diagram

```
CaseDataRetriever
    └─► OwnershipVerifier
            └─► UserInputValidator
                    │
                    ├─ [IsValid = true]  ──► OtpAgent                           ← terminal (success)
                    │                        LLM: send OTP → ask code → validate → show card
                    │
                    └─ [IsValid = false] ──► OwnershipVerificationFailureHandler ← terminal (failure)
```

---

## Models

| Record | Description |
|---|---|
| `CasesWorkflowState` | Workflow entry — user `ChatMessage`, `ConversationId`, `UserIdentifier` |
| `CaseRetrievalOutput` | `JsonNode CaseData`, `CaseId`, `PhoneNumber?`, `Email?`, `UserIdentifier` |
| `OwnershipVerificationOutput` | `VerificationFieldName`, `VerificationFieldValue` (unmasked), `VerificationPrompt` |
| `UserInputValidationOutput` | `IsValid`, `ErrorMessage?`, `ValidationAttempt`, `UserInput` |
| `ValidationFailureOutput` | `ErrorMessage`, `FailureStep`, `AttemptsExhausted` |
| `RagPipelineOutput` | `Answer` — fully accumulated streamed text from `OtpAgent` |

> `OtpSendOutput`, `OtpValidationOutput`, and `FinalCardOutput` no longer exist.
> The LLM manages the OTP flow internally; no intermediate records are needed.

---

## Configuration

### `appsettings.json`

```json
{
  "Dex": {
    "Mcp": {
      "Services": {
        "otp": {
          "Endpoint": "https://your-otp-mcp-server",
          "AuthenticationMethod": "bearer",
          "AuthenticationValue": "${OTP_BEARER_TOKEN}",
          "TimeoutMilliseconds": 30000,
          "MaxRetries": 1
        },
        "caseretrieval": {
          "Endpoint": "https://your-case-api-mcp-server",
          "AuthenticationMethod": "apikey",
          "AuthenticationValue": "${CASE_API_KEY}",
          "TimeoutMilliseconds": 30000,
          "MaxRetries": 1
        }
      }
    }
  }
}
```

Accepted `AuthenticationMethod` values: `"none"` | `"apikey"` | `"bearer"` | `"basic"`.

### Service Registration

```csharp
// Program.cs / Startup.cs
services
    .AddAgentsCore(configuration)
    .AddCasesWorkflow()
    .AddScoped<IExternalCaseApiService, YourCaseApiImplementation>();
```

`IMcpOtpService` is **not required** — OTP send/validate is performed by the LLM via MCP tools
fetched at runtime from the `"otp"` entry in `AgentsOptions.Mcp.Services`.

---

## Prompt Templates

Create the two files below in the location resolved by `IPromptTemplateRenderer`
(default: `Workflows/Prompts/`).

### `CasesOwnershipVerifier` — used by step 2

```
You are verifying ownership of a case. Ask the user to confirm the value of their {{fieldName}}.
The value on file ends with: {{maskedValue}}.
Do not reveal the full value. Accept only an exact match (case-insensitive).
```

### `CasesOtpAgent` — used by step 4

This is the prompt the user validated empirically. Customise the `purpose`, `subject`, and
`message` to match your MCP server's contract.

```
You are a helper agent for the Identity API. Your goal is to verify the user's identity
using a one-time password (OTP).

The user's contact details are already known:
  Phone : {{phoneNumber}}
  Email : {{email}}
  Case  : {{caseId}}

Use the available tools to complete the following steps in order:

1. Send an OTP to the phone number above.
     channel              : SMS
     purpose              : 'Velmar totp'
     message              : 'This is your {0} OTP code to sign in'
     subject              : 'Velmar auth'
     authenticationMethod : PhoneNumber
     emailTemplate        : null
     data                 : null
   If no phone number is available, fall back to the email address.

2. Ask the user to enter the code they received.

3. Validate the code using:
     authenticationMethod : PhoneNumber
     purpose              : 'Velmar totp'

4. If validation succeeds, present the user's case information for case {{caseId}}
   in a clear, friendly summary card.

5. If validation fails, inform the user politely and allow them to try again
   (maximum 2 attempts total before stopping).
```

---

## Service to Implement

Only one service requires a concrete implementation:

### `IExternalCaseApiService`

```csharp
public class YourCaseApiImplementation : IExternalCaseApiService
{
    public async ValueTask<JsonNode> RetrieveCaseAsync(
        string userInput,
        string userIdentifier,
        CancellationToken cancellationToken = default)
    {
        // Call your case data source.
        // The returned JsonNode MUST contain:
        //   "CaseId"            : string   (required)
        //   "PhoneNumber"       : string?  (primary OTP channel)
        //   "Email"             : string?  (fallback OTP channel)
        //   "VerificationField" : string   (key of the field the user must confirm, e.g. "LastName")
        //   [VerificationField] : string   (the actual value to compare against)
        // Any additional fields are forwarded as case details by OtpAgent.
    }
}
```

---

## MCP Tools Registry

`IMcpToolsRegistry` (singleton, implemented by `McpToolsRegistry`) is registered automatically
by `AddAgentsCore`. On first call it connects to the MCP server, fetches the tool manifest, and
caches it for the lifetime of the application. `OtpAgent` calls:

```csharp
var mcpTools = await _mcpToolsRegistry.GetToolsAsync("otp", cancellationToken);
```

The key `"otp"` must match an entry under `Dex:Mcp:Services` in configuration.

---

## Streaming

`OtpAgent` emits each token as an `AnswerDeltaEvent` via `context.AddEventAsync`. The streaming
runner in `Agents.Server` surfaces these as SSE `delta` frames. Non-streaming callers receive the
fully accumulated `RagPipelineOutput.Answer` once the step completes.

---

## Testing

```csharp
// Mock the external case API
services.AddScoped<IExternalCaseApiService>(_ => new FakeCaseApiService());

// Mock the MCP tool registry (return test AITool stubs)
services.AddSingleton<IMcpToolsRegistry>(_ => new FakeMcpToolsRegistry());
```

`FakeMcpToolsRegistry` can return `AIFunction`-based tools that simulate send/validate without
hitting a real MCP server, enabling deterministic integration tests.

---

## Next Steps

1. **Implement** `IExternalCaseApiService` to call your case data source
2. **Create** `CasesOwnershipVerifier.txt` prompt template
3. **Create** `CasesOtpAgent.txt` prompt template (tune `purpose`/`subject`/`message` to your MCP contract)
4. **Configure** `Dex:Mcp:Services:otp` and `Dex:Mcp:Services:caseretrieval` in `appsettings.json`
5. **Register** your `IExternalCaseApiService` implementation in DI
6. **Write tests** using `FakeCaseApiService` and `FakeMcpToolsRegistry`
