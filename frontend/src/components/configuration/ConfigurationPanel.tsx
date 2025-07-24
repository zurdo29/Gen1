import React, { useState, useCallback, useEffect } from 'react';
import {
  Box,
  Paper,
  Tabs,
  Tab,
  Typography,
  Alert,
  Collapse,
  IconButton,
  Chip,
  Stack
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Warning as WarningIcon,
  Error as ErrorIcon
} from '@mui/icons-material';
import { GenerationConfig, ValidationResult, ValidationError, ValidationWarning } from '../../types';
import { validateGenerationConfig } from '../../schemas';
import { TerrainConfigPanel } from './TerrainConfigPanel';
import { EntityConfigPanel } from './EntityConfigPanel';
import { VisualConfigPanel } from './VisualConfigPanel';
import { GameplayConfigPanel } from './GameplayConfigPanel';

interface ConfigurationPanelProps {
  config: GenerationConfig;
  onChange: (config: GenerationConfig) => void;
  onValidate?: (result: ValidationResult) => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel({ children, value, index, ...other }: TabPanelProps) {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`config-tabpanel-${index}`}
      aria-labelledby={`config-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

export const ConfigurationPanel: React.FC<ConfigurationPanelProps> = ({
  config,
  onChange,
  onValidate
}) => {
  const [activeTab, setActiveTab] = useState(0);
  const [validationResult, setValidationResult] = useState<ValidationResult>({
    isValid: true,
    errors: [],
    warnings: []
  });
  const [showValidation, setShowValidation] = useState(false);

  // Validate configuration whenever it changes
  const validateConfig = useCallback((configToValidate: GenerationConfig) => {
    const isValid = validateGenerationConfig(configToValidate);
    const errors: ValidationError[] = [];
    const warnings: ValidationWarning[] = [];

    if (!isValid && validateGenerationConfig.errors) {
      validateGenerationConfig.errors.forEach((error, index) => {
        const field = error.instancePath || error.schemaPath || 'unknown';
        const message = error.message || 'Validation error';
        
        errors.push({
          field: field.replace(/^\//, '').replace(/\//g, '.'),
          message: message,
          code: `validation_${index}`
        });
      });
    }

    // Additional custom validation
    if (configToValidate.entities.length === 0) {
      warnings.push({
        field: 'entities',
        message: 'No entities configured - level may be empty',
        suggestion: 'Add at least one entity type'
      });
    }

    if (configToValidate.terrainTypes.length === 1) {
      warnings.push({
        field: 'terrainTypes',
        message: 'Only one terrain type selected',
        suggestion: 'Consider adding more terrain types for variety'
      });
    }

    const result: ValidationResult = {
      isValid: errors.length === 0,
      errors,
      warnings
    };

    setValidationResult(result);
    onValidate?.(result);
    
    return result;
  }, [onValidate]);

  // Validate on config changes
  useEffect(() => {
    validateConfig(config);
  }, [config, validateConfig]);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleConfigChange = useCallback((updates: Partial<GenerationConfig>) => {
    const newConfig = { ...config, ...updates };
    onChange(newConfig);
  }, [config, onChange]);

  const getTabColor = (tabIndex: number): 'default' | 'error' | 'warning' => {
    const tabFields = [
      ['width', 'height', 'seed', 'generationAlgorithm', 'algorithmParameters', 'terrainTypes'],
      ['entities'],
      ['visualTheme'],
      ['gameplay']
    ];

    const fields = tabFields[tabIndex] || [];
    const hasError = validationResult.errors.some(error => 
      fields.some(field => error.field.startsWith(field))
    );
    const hasWarning = validationResult.warnings.some(warning => 
      fields.some(field => warning.field.startsWith(field))
    );

    if (hasError) return 'error';
    if (hasWarning) return 'warning';
    return 'default';
  };

  return (
    <Paper elevation={2} sx={{ width: '100%', bgcolor: 'background.paper' }}>
      <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 2, pt: 2 }}>
        <Typography variant="h6" gutterBottom>
          Level Configuration
        </Typography>
        
        {/* Validation Summary */}
        {(validationResult.errors.length > 0 || validationResult.warnings.length > 0) && (
          <Box sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <IconButton
                size="small"
                onClick={() => setShowValidation(!showValidation)}
                aria-label="toggle validation details"
              >
                {showValidation ? <ExpandLessIcon /> : <ExpandMoreIcon />}
              </IconButton>
              <Stack direction="row" spacing={1}>
                {validationResult.errors.length > 0 && (
                  <Chip
                    icon={<ErrorIcon />}
                    label={`${validationResult.errors.length} error${validationResult.errors.length !== 1 ? 's' : ''}`}
                    color="error"
                    size="small"
                  />
                )}
                {validationResult.warnings.length > 0 && (
                  <Chip
                    icon={<WarningIcon />}
                    label={`${validationResult.warnings.length} warning${validationResult.warnings.length !== 1 ? 's' : ''}`}
                    color="warning"
                    size="small"
                  />
                )}
              </Stack>
            </Box>
            
            <Collapse in={showValidation}>
              <Stack spacing={1}>
                {validationResult.errors.map((error, index) => (
                  <Alert key={`error-${index}`} severity="error">
                    <strong>{error.field}:</strong> {error.message}
                  </Alert>
                ))}
                {validationResult.warnings.map((warning, index) => (
                  <Alert key={`warning-${index}`} severity="warning">
                    <strong>{warning.field}:</strong> {warning.message}
                    {warning.suggestion && (
                      <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
                        Suggestion: {warning.suggestion}
                      </Typography>
                    )}
                  </Alert>
                ))}
              </Stack>
            </Collapse>
          </Box>
        )}

        <Tabs 
          value={activeTab} 
          onChange={handleTabChange} 
          aria-label="configuration tabs"
          variant="fullWidth"
        >
          <Tab 
            label="Terrain" 
            id="config-tab-0"
            aria-controls="config-tabpanel-0"
            sx={{ color: getTabColor(0) === 'error' ? 'error.main' : getTabColor(0) === 'warning' ? 'warning.main' : 'inherit' }}
          />
          <Tab 
            label="Entities" 
            id="config-tab-1"
            aria-controls="config-tabpanel-1"
            sx={{ color: getTabColor(1) === 'error' ? 'error.main' : getTabColor(1) === 'warning' ? 'warning.main' : 'inherit' }}
          />
          <Tab 
            label="Visual" 
            id="config-tab-2"
            aria-controls="config-tabpanel-2"
            sx={{ color: getTabColor(2) === 'error' ? 'error.main' : getTabColor(2) === 'warning' ? 'warning.main' : 'inherit' }}
          />
          <Tab 
            label="Gameplay" 
            id="config-tab-3"
            aria-controls="config-tabpanel-3"
            sx={{ color: getTabColor(3) === 'error' ? 'error.main' : getTabColor(3) === 'warning' ? 'warning.main' : 'inherit' }}
          />
        </Tabs>
      </Box>

      <TabPanel value={activeTab} index={0}>
        <TerrainConfigPanel
          config={config}
          onChange={handleConfigChange}
          validationErrors={validationResult.errors.filter(e => 
            ['width', 'height', 'seed', 'generationAlgorithm', 'algorithmParameters', 'terrainTypes'].some(field => 
              e.field.startsWith(field)
            )
          )}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        <EntityConfigPanel
          config={config}
          onChange={handleConfigChange}
          validationErrors={validationResult.errors.filter(e => e.field.startsWith('entities'))}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        <VisualConfigPanel
          config={config}
          onChange={handleConfigChange}
          validationErrors={validationResult.errors.filter(e => e.field.startsWith('visualTheme'))}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        <GameplayConfigPanel
          config={config}
          onChange={handleConfigChange}
          validationErrors={validationResult.errors.filter(e => e.field.startsWith('gameplay'))}
        />
      </TabPanel>
    </Paper>
  );
};