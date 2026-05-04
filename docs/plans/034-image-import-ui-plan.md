# Implementation Plan: Multimodal Image Import UI (Step 034)

**Objective**: Enhance the transaction import UI to support both CSV files and bank statement images with appropriate visual feedback and progress indicators.

**Scope**: Frontend updates to FileUpload.tsx and DetectionProgressIndicator.tsx components

**Dependency**: Requires completed backend image import support (Step 033)

---

## Implementation Phases

### Phase 1: File Input Enhancement

**Goal**: Enable file input to accept image formats alongside CSV

**Tasks**:

1. **Update FileUpload.tsx - File Input Attributes**
   - Modify file input `accept` attribute to: `.csv,.png,.jpg,.jpeg`
   - Location: File input element in FileUpload.tsx

2. **Implement File Validation Function**
   - Create `validateFile()` function that:
     - Accepts extensions: `.csv`, `.png`, `.jpg`, `.jpeg`
     - Enforces 10MB maximum file size
     - Returns appropriate error message for invalid files
   - Update error messages to reference both CSV and image formats

**Files to Modify**:
- `src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx`

---

### Phase 2: File Type Detection & Display

**Goal**: Implement helper functions to identify and display file type information

**Tasks**:

1. **Create File Type Helper Functions**
   - `isImageFile(fileName)`: Returns boolean based on file extension
   - `getFileTypeIcon(fileName)`: Returns emoji icon (🖼️ for images, 📊 for CSV)
   - `getFileTypeLabel(fileName)`: Returns display label ("Bank Statement Image" or "CSV Bank Statement")

2. **Update File Selection Display**
   - Show file icon and type label when file is selected
   - Display file name and size in KB
   - Add "Clear file" button for deselection
   - Show quality tip for image files: "For best results, ensure the image clearly shows transaction details..."

3. **Update Dropzone Instructions**
   - Modify instructional text to mention both CSV and image support
   - Update SVG icon if needed to represent multimodal input

**Files to Modify**:
- `src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx`

---

### Phase 3: Progress Phase Management

**Goal**: Add image-specific progress phases and update phase handling logic

**Tasks**:

1. **Extend ImportPhase Type**
   - Update type to include: `'uploading' | 'detecting' | 'parsing' | 'extracting' | 'enhancing' | 'complete'`
   - Add state: `const [currentPhase, setCurrentPhase] = useState<ImportPhase>('uploading')`

2. **Update handleImport Function**
   - Detect if file is image using `isImageFile()`
   - Update phase progression based on upload progress percentage:
     - 0-20%: `uploading`
     - 20-60%: `extracting` (for images) or `detecting` (for CSV)
     - 60-85%: `parsing` (CSV only)
     - 85-100%: `enhancing`
     - 100%: `complete`
   - Update success message to use `getFileTypeLabel()` for context-aware messaging

3. **Error Handling Enhancement**
   - Implement `getErrorMessage()` function to provide image-specific error handling:
     - Low confidence extraction → Suggest clearer image
     - Processing errors → Guide user to clear transaction list images
     - CSV structure detection failures → Suggest clear column headers
   - Replace generic errors with contextual messages in catch block

**Files to Modify**:
- `src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx`

---

### Phase 4: Progress Indicator Updates

**Goal**: Update DetectionProgressIndicator to display image-specific progress states

**Tasks**:

1. **Update Component Props**
   - Extend `currentPhase` prop to include all new phases
   - Ensure `fileName` prop is passed and accessible

2. **Implement Phase Description Logic**
   - Create `getPhaseDescription()` function that returns file-type-aware descriptions:
     - `uploading`: "Uploading [bank statement image/CSV file]..."
     - `detecting`: "Preparing image for analysis..." (image) or "Detecting CSV structure..." (CSV)
     - `parsing`: "Parsing CSV data..."
     - `extracting`: "Extracting transactions from image using AI..."
     - `enhancing`: "Enhancing transaction descriptions with AI..."
     - `complete`: "Import completed successfully!"

3. **Implement Phase Icon Logic**
   - Create `getPhaseIcon()` function mapping phases to emojis:
     - `uploading` → ⬆️
     - `detecting` → 🔍
     - `parsing` → 📊
     - `extracting` → 🖼️
     - `enhancing` → ✨
     - `complete` → ✅

4. **Add Image Extraction Animation**
   - Detect when phase is `extracting`
   - Display spinner animation with text: "AI vision analyzing bank statement..."
   - Position: Below progress bar with 2px margin-top

5. **Update Progress Display**
   - Show phase icon, description, and percentage
   - Ensure smooth progress transitions with `transition-all duration-300`

**Files to Modify**:
- `src/BudgetTracker.Web/src/features/transactions/components/DetectionProgressIndicator.tsx`

---

## Implementation Checklist

- [ ] **Phase 1: File Input Enhancement**
  - [ ] Update file input accept attribute
  - [ ] Implement validateFile() function
  - [ ] Update validation error messages

- [ ] **Phase 2: File Type Detection & Display**
  - [ ] Create isImageFile() helper
  - [ ] Create getFileTypeIcon() helper
  - [ ] Create getFileTypeLabel() helper
  - [ ] Update file selection display section
  - [ ] Add image quality tip
  - [ ] Update dropzone instructions

- [ ] **Phase 3: Progress Phase Management**
  - [ ] Extend ImportPhase type definition
  - [ ] Add currentPhase state
  - [ ] Update handleImport() phase progression logic
  - [ ] Implement getErrorMessage() function
  - [ ] Update error handling in catch block

- [ ] **Phase 4: Progress Indicator Updates**
  - [ ] Update component props interface
  - [ ] Implement getPhaseDescription() function
  - [ ] Implement getPhaseIcon() function
  - [ ] Add image extraction animation
  - [ ] Update progress display markup

---

## Testing Strategy

### Unit Level
- Test `validateFile()` with various file types and sizes
- Test `isImageFile()` with all supported extensions
- Test `getErrorMessage()` with image-specific error scenarios

### Integration Level
- **Image Upload Display**: Select image → Verify icon, label, and tip appear
- **Image Processing**: Upload image → Verify extraction phase shows → Verify success message
- **CSV vs Image Distinction**: Upload both file types → Compare UI behavior
- **Error Handling**: Upload low-quality image → Verify contextual error message

### User Flow Testing
1. Select CSV file → Display differs from image
2. Upload bank statement image → Progress shows extraction phase
3. Upload image with poor quality → See helpful error message
4. Clear file and select different type → UI updates correctly

---

## Key Technical Considerations

1. **File Type Detection**: Use fileName extension matching (case-insensitive) rather than MIME type
2. **Progress Phase Timing**: Phase changes tied to upload progress percentage since backend processes images differently than CSV
3. **Error Messages**: Image-specific errors require API response message parsing for context (confidence scores, processing errors)
4. **Animation**: CSS spinning animation for extraction phase using Tailwind's `animate-spin`
5. **Type Safety**: All phase strings should use the ImportPhase type union to prevent invalid states

---

## Success Criteria

- ✅ File input accepts CSV and image formats
- ✅ File selection displays appropriate icon and label
- ✅ Image-specific quality tip displays when image selected
- ✅ Progress indicator shows extraction phase for images
- ✅ Extraction phase includes spinner animation
- ✅ Error messages are contextual to file type
- ✅ CSV and image flows have distinct visual and textual differences
- ✅ All 4 test scenarios pass without errors

---

## Files Modified

| File | Changes |
|------|---------|
| `src/BudgetTracker.Web/src/features/transactions/components/FileUpload.tsx` | File input attributes, validation, helpers, phase management, error handling |
| `src/BudgetTracker.Web/src/features/transactions/components/DetectionProgressIndicator.tsx` | Phase descriptions, icons, extraction animation |

---

## Estimated Effort

- **Phase 1**: 10 minutes (file input and validation updates)
- **Phase 2**: 20 minutes (helpers and file display)
- **Phase 3**: 20 minutes (phase management and error handling)
- **Phase 4**: 20 minutes (progress indicator updates)
- **Testing**: 15 minutes

**Total Estimate**: ~85 minutes
