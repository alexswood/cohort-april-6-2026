# 022 React AI Enhancement UI — Commit Plan

## Goal
Build a multi-step upload wizard (Upload → Preview → Complete) that lets users review and control AI-generated transaction enhancements before applying them.

## Commits

### Commit 1 — Types
Update `src/BudgetTracker.Web/src/features/transactions/types.ts`:
- Add `TransactionEnhancement` interface
- Add `EnhanceImportRequest` interface
- Add `EnhanceImportResult` interface
- Update `ImportResult` with `importSessionHash: string` and `enhancements: TransactionEnhancement[]`

### Commit 2 — API method
Update `src/BudgetTracker.Web/src/features/transactions/api.ts`:
- Add `enhanceImport(request: EnhanceImportRequest): Promise<EnhanceImportResult>` method posting to `/transactions/import/enhance`
- Re-export new types so they're importable from `'../api'`

### Commit 3 — State & handlers
Update `src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx`:
- Add state: `currentStep` (`'upload' | 'preview' | 'complete'`), `currentPhase` (`'uploading' | 'parsing' | 'enhancing' | 'complete'`), `minConfidenceScore`, `enhanceResult`
- Replace `handleImport`: add phase tracking via progress %, transition to `'preview'` step on success
- Add `handleEnhance(applyEnhancements: boolean)`: calls `enhanceImport`, transitions to `'complete'`, auto-redirects after 4s
- Update `handleClearFile`: reset all new state

### Commit 4 — Step indicator
Add a 3-step visual progress indicator at the top of the return:
- Steps: Upload → Preview → Complete
- Active step highlighted in blue, completed steps show a checkmark in green
- Connector lines between steps reflect completion state

### Commit 5 — Upload step UI
Wrap existing drag-drop area and file details panel in `{currentStep === 'upload' && ...}`:
- Add AI Confidence Threshold dropdown (Low 30% / Medium 50% / High 70%) alongside Account Name field
- Update import button label to show phase-aware text (Uploading… / Parsing… / Enhancing…)
- Update progress bar label to match current phase

### Commit 6 — Preview step UI
Add `{currentStep === 'preview' && importResult && ...}` block:
- Header showing imported count and how many enhancements will apply at current threshold
- Scrollable list (max 15 shown) of enhancements with:
  - Original description
  - Enhanced description (when above threshold) with category badge
  - Confidence score pill (green ≥80%, yellow ≥50%, red below)
  - "Below threshold" message when filtered out
- Confidence threshold slider (0.3–0.9) that live-filters the list
- "Start Over", "Skip Enhancement", and "Apply Enhancements" buttons

### Commit 7 — Complete step UI
Add `{currentStep === 'complete' && ...}` block:
- Green checkmark icon
- Summary: enhanced count vs total, or "original descriptions kept" if skipped
- "View Transactions" button navigating to `/transactions`
