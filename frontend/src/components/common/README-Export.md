# Export Functionality

This directory contains the export functionality for the web level editor, implementing task 9.2 from the specification.

## Components

### ExportManager
The main export dialog component that provides:
- Multi-format export interface (JSON, Unity, CSV, XML)
- Export preview and validation
- Download progress tracking for large exports
- Batch export with ZIP packaging
- Format-specific customization options

### ExportButton
A simple button component that provides:
- Quick export options via dropdown menu
- Integration with ExportManager for advanced options
- Support for both single level and batch export

### useExport Hook
A custom hook that manages:
- Export state and progress tracking
- Available format loading
- Single level export operations
- Batch export with job polling
- Export validation
- Error handling and notifications

## Features Implemented

✅ **Multi-format export interface**
- JSON (web-friendly format)
- Unity (Unity-compatible format with coordinate conversion)
- CSV (spreadsheet format)
- XML (structured data format)

✅ **Download progress tracking**
- Real-time progress indicators for large exports
- Background job processing for batch operations
- Cancellation support

✅ **Export preview and validation**
- Preview export content before download
- Comprehensive validation with error/warning messages
- Format-specific validation rules

✅ **Batch export with ZIP packaging**
- Multiple level export in single operation
- ZIP file packaging for batch results
- Progress tracking for batch operations

✅ **Comprehensive testing**
- Unit tests for all components
- Integration tests for export workflows
- Error handling test coverage

## Usage Examples

### Single Level Export
```tsx
import { ExportButton } from '../components/common/ExportButton';

<ExportButton
  level={currentLevel}
  variant="outlined"
  disabled={!level}
/>
```

### Batch Export
```tsx
import { ExportButton } from '../components/common/ExportButton';

<ExportButton
  levels={batchResults.map(r => r.level)}
  variant="contained"
  disabled={batchResults.length === 0}
/>
```

### Advanced Export with Custom Options
```tsx
import { ExportManager } from '../components/common/ExportManager';

<ExportManager
  open={exportDialogOpen}
  onClose={() => setExportDialogOpen(false)}
  level={currentLevel}
  levels={batchLevels}
/>
```

### Using the Export Hook
```tsx
import { useExport } from '../../hooks/useExport';

const MyComponent = () => {
  const {
    exportState,
    availableFormats,
    loadFormats,
    exportLevel,
    exportBatch,
    validateExport
  } = useExport();

  useEffect(() => {
    loadFormats();
  }, [loadFormats]);

  const handleExport = async () => {
    try {
      await exportLevel(level, 'json', {
        format: 'json',
        includeMetadata: true,
        customSettings: { prettyPrint: true }
      });
    } catch (error) {
      console.error('Export failed:', error);
    }
  };

  return (
    <div>
      <button onClick={handleExport} disabled={exportState.isExporting}>
        {exportState.isExporting ? `Exporting... ${exportState.progress}%` : 'Export'}
      </button>
    </div>
  );
};
```

## API Integration

The export functionality integrates with the backend ExportController which provides:

- `GET /api/export/formats` - Get available export formats
- `POST /api/export/level` - Export single level
- `POST /api/export/batch` - Start batch export job
- `GET /api/export/batch/{jobId}/status` - Get batch export status
- `GET /api/export/batch/{jobId}/download` - Download batch export result
- `POST /api/export/validate` - Validate export request

## Format Support

### JSON Format
- Web-friendly JSON structure
- Optional pretty printing
- Metadata inclusion
- Compression options

### Unity Format
- Unity-compatible coordinate system
- Prefab data generation
- Collider information
- Layer mapping

### CSV Format
- Spreadsheet-compatible format
- Configurable delimiters
- Header row options
- String quoting

### XML Format
- Structured XML output
- Schema inclusion
- Encoding options
- Formatted output

## Error Handling

The export system provides comprehensive error handling:
- Validation errors with specific field information
- Network error recovery
- Progress tracking with cancellation
- User-friendly error messages
- Automatic retry mechanisms

## Testing

Run the export tests with:
```bash
npm test -- ExportManager.test.tsx
npm test -- useExport.test.ts
```

The test suite covers:
- Component rendering and interaction
- Export workflow scenarios
- Error handling and edge cases
- Batch export operations
- Validation logic
- API integration