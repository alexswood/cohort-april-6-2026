# Plan: 032 – Smart CSV Detection UI

## Goal

Enhance the import UI to surface detection method and confidence from the backend, guide users through detection-aware progress phases, and display clear success/error states that communicate how their CSV was analyzed.

The backend (`ImportResult`) already returns `DetectionMethod` (string: `"RuleBased"` | `"AI"`) and `DetectionConfidence` (double 0–1). The frontend just needs to consume and display these fields.

---

## Current State

| File | Status |
|------|--------|
| `src/BudgetTracker.Web/src/features/transactions/types.ts` | `ImportResult` missing `detectionMethod` and `detectionConfidence` |
| `src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx` | Progress phases: `uploading \| parsing \| enhancing \| complete` — no `detecting` phase |
| `src/BudgetTracker.Web/src/features/transactions/components/DetectionProgressIndicator.tsx` | Does not exist |
| `src/BudgetTracker.Web/src/features/transactions/components/DetectionMethodBadge.tsx` | Does not exist |

---

## Implementation Steps

### Step 1 — Update `ImportResult` TypeScript type

**File**: [src/BudgetTracker.Web/src/features/transactions/types.ts](src/BudgetTracker.Web/src/features/transactions/types.ts)

Add two optional fields to `ImportResult`:

```ts
detectionMethod?: string;       // "RuleBased" | "AI"
detectionConfidence?: number;   // 0–1 (backend sends double, not 0–100)
```

Note: The backend `DetectionConfidence` is a `double` in the range 0–1, not 0–100. Multiply by 100 in the UI when displaying as a percentage.

---

### Step 2 — Create `DetectionProgressIndicator` component

**File**: [src/BudgetTracker.Web/src/features/transactions/components/DetectionProgressIndicator.tsx](src/BudgetTracker.Web/src/features/transactions/components/DetectionProgressIndicator.tsx)

New component replacing the raw progress bar in `FileUpload`. Props:

```ts
interface DetectionProgressIndicatorProps {
  progress: number;
  currentPhase: 'uploading' | 'detecting' | 'parsing' | 'enhancing' | 'complete';
  fileName: string;
}
```

Renders:
- Phase icon + description text on the left, `{progress}%` on the right
- Animated progress bar (blue, transitions smoothly)
- When `currentPhase === 'detecting'`: animated spinner sub-line "Analyzing column structure and format..."

---

### Step 3 — Add `detecting` phase to `FileUpload`

**File**: [src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx](src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx)

Three changes:

1. **Extend the phase union type** on line 25 to include `'detecting'`:
   ```ts
   const [currentPhase, setCurrentPhase] = useState<'uploading' | 'detecting' | 'parsing' | 'enhancing' | 'complete'>('uploading');
   ```

2. **Update `handleImport` progress thresholds** to insert the detecting phase between uploading and parsing:
   ```
   progress < 20  → 'uploading'
   progress < 40  → 'detecting'
   progress < 70  → 'parsing'
   progress < 100 → 'enhancing'
   else           → 'complete'
   ```

3. **Replace the inline progress bar** (lines 364–382) with the new `<DetectionProgressIndicator>` component, passing `progress`, `currentPhase`, and `selectedFile.name`.

4. **Update the import button label** to include `'detecting'` in the ternary:
   ```tsx
   currentPhase === 'detecting' ? 'Detecting...' : ...
   ```

---

### Step 4 — Create `DetectionMethodBadge` component

**File**: [src/BudgetTracker.Web/src/features/transactions/components/DetectionMethodBadge.tsx](src/BudgetTracker.Web/src/features/transactions/components/DetectionMethodBadge.tsx)

Props:
```ts
interface DetectionMethodBadgeProps {
  method: string;       // "RuleBased" | "AI"
  confidence?: number;  // 0–1 from backend; display as percentage
  showConfidence?: boolean;
}
```

Renders two inline badges:
- Method badge: blue pill for `RuleBased` ("Pattern Match"), purple pill for `AI` ("AI Detection")
- Confidence badge (when `showConfidence && confidence !== undefined`): green ≥ 0.9, yellow ≥ 0.7, red < 0.7

---

### Step 5 — Show detection info in the preview step header

**File**: [src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx](src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx)

In the Step 2 (preview) panel header (around line 392), add a "Structure Detection" row using `DetectionMethodBadge`:

```tsx
{importResult.detectionMethod && (
  <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg mb-4">
    <span className="text-sm text-gray-600">Structure Detection</span>
    <DetectionMethodBadge
      method={importResult.detectionMethod}
      confidence={importResult.detectionConfidence}
    />
  </div>
)}
```

This sits above the enhancement list so users immediately see how their CSV was parsed before deciding whether to apply AI enhancements.

---

### Step 6 — Improve error handling for detection failures

**File**: [src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx](src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx)

Extract a `getErrorMessage(error)` helper (before the `handleImport` callback) that maps known detection error strings to user-friendly messages:

| Backend message substring | User-facing message |
|---------------------------|---------------------|
| `"Unable to automatically detect CSV structure"` | "Could not detect the CSV format. Please ensure your file has clear column headers (Date, Description, Amount)." |
| `"AI analysis"` | "AI analysis could not determine the file structure. Try a CSV with standard column names." |
| _(default)_ | Original error message or `"Failed to import the CSV file"` |

Replace the inline error string extraction in the `catch` block with a call to this helper.

---

## File Change Summary

| File | Change type |
|------|-------------|
| `types.ts` | Extend `ImportResult` with two fields |
| `DetectionProgressIndicator.tsx` | **Create new** |
| `DetectionMethodBadge.tsx` | **Create new** |
| `FileUpload.tsx` | Extend phase type; update progress thresholds; swap progress bar for new component; add detection badge in preview; extract error helper |

---

## Key Decisions

- **Confidence is 0–1, not 0–100**: The backend `double` maps directly; multiply by 100 only when rendering text (e.g. `Math.round(confidence * 100)`).
- **No new API calls**: Everything is already returned in the `ImportResult` response — no polling or separate endpoint needed.
- **Detection info shown in preview, not complete step**: Users see it before deciding whether to apply enhancements, which is when it's most actionable.
- **No `DetectionProgressIndicator` in the complete step**: The complete step is already minimal; the badge in the preview step is sufficient.
