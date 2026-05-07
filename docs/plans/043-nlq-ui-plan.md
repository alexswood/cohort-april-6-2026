# Plan: 043 - Natural Language Query Assistant UI

## Overview

Integrate the RAG-powered NLQ backend into the React frontend with a conversational query interface on the dashboard.

## What Already Exists

- `src/shared/components/LoadingSpinner.tsx` — already implemented, matches the task spec exactly, **no changes needed**
- `src/api/client.ts` — Axios client with CSRF support, base URL at `localhost:5295/api`
- `src/shared/utils/formatters.ts` — exports `formatCurrency` and `formatDate` used in `QueryAssistant`
- `src/shared/contexts/ToastContext` — assumed to exist (used by auth feature); verify `showError` is exported
- `src/routes/dashboard.tsx` — exists with a 3-column grid layout; needs to be replaced with the task's 2-column layout

## What Needs to Be Created

### 1. `src/features/intelligence/api.ts`
TypeScript interfaces and Axios call for the `/query/ask` endpoint.

```
QueryRequest    { query: string }
TransactionDto  { id, date, description, amount, balance?, category?, labels?, importedAt, account }
QueryResponse   { answer: string, amount?: number, transactions?: TransactionDto[] }

intelligenceApi.askQuery(query) → POST /query/ask → QueryResponse
```

### 2. `src/features/intelligence/components/QueryAssistant.tsx`
Main conversational UI component:
- Text input + submit button (disabled while loading)
- Inline `LoadingSpinner` in input and button during fetch
- Suggested query chips shown when no response is present
- Response card with AI answer text + up to 3 transaction cards (with a "...and N more" overflow line)
- `showError` toast on catch

### 3. `src/features/intelligence/index.ts`
Barrel export for `QueryAssistant`, `intelligenceApi`, and types.

### 4. `src/routes/dashboard.tsx` (update)
Replace current 3-column placeholder grid with the task's layout:
- Full-width `QueryAssistant` (lg:col-span-2) at the top
- "Transactions" card (link to `/transactions`)
- "Spending Summary" placeholder card

## Implementation Steps

| # | File | Action |
|---|------|--------|
| 1 | `src/features/intelligence/api.ts` | Create — types + `intelligenceApi` |
| 2 | `src/features/intelligence/components/QueryAssistant.tsx` | Create — full component |
| 3 | `src/features/intelligence/index.ts` | Create — barrel exports |
| 4 | `src/routes/dashboard.tsx` | Update — add `QueryAssistant`, adjust grid |

`LoadingSpinner` is already correct — **do not modify it**.

## Pre-Implementation Checks

Before writing code:
1. Confirm `src/shared/contexts/ToastContext.tsx` exports `useToast` with a `showError` function (same pattern as auth feature uses it).
2. Confirm the backend `/api/query/ask` endpoint accepts `{ query: string }` POST body and returns the `QueryResponse` shape (from task 042).

## Risk / Notes

- The `formatCurrency` formatter uses USD locale. If the app uses EUR in production data this will display wrong symbols — acceptable for now per task scope.
- The task spec shows `lg:col-span-2` for `QueryAssistant` inside a `grid-cols-1 lg:grid-cols-2` grid, meaning it spans the full width on large screens.
- The existing dashboard has a 3-column grid (`md:grid-cols-3`); the new layout switches to 2-column (`lg:grid-cols-2`) — this is a deliberate layout change per the task spec.
- No routing changes needed — the component lives on the existing `/` dashboard route.
