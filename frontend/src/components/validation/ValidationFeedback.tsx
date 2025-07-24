import React from 'react';
import {
  Box,
  Typography,
  Alert,
  AlertTitle,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Chip,
  Collapse,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  CheckCircle as CheckCircleIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Lightbulb as SuggestionIcon,
} from '@mui/icons-material';

export interface ValidationFeedbackProps {
  errors?: string[];
  warnings?: string[];
  suggestions?: string[];
  showSuggestions?: boolean;
  compact?: boolean;
  severity?: 'error' | 'warning' | 'info' | 'success';
}

export const ValidationFeedback: React.FC<ValidationFeedbackProps> = ({
  errors = [],
  warnings = [],
  suggestions = [],
  showSuggestions = true,
  compact = false,
  severity
}) => {
  const [expanded, setExpanded] = React.useState(!compact);

  const hasErrors = errors.length > 0;
  const hasWarnings = warnings.length > 0;
  const hasSuggestions = suggestions.length > 0;
  const hasAnyFeedback = hasErrors || hasWarnings || (showSuggestions && hasSuggestions);

  if (!hasAnyFeedback) {
    return null;
  }

  const determinedSeverity = severity || (hasErrors ? 'error' : hasWarnings ? 'warning' : 'info');

  const getSeverityIcon = (sev: string) => {
    switch (sev) {
      case 'error':
        return <ErrorIcon />;
      case 'warning':
        return <WarningIcon />;
      case 'success':
        return <CheckCircleIcon />;
      default:
        return <InfoIcon />;
    }
  };

  const getSeverityColor = (sev: string) => {
    switch (sev) {
      case 'error':
        return 'error';
      case 'warning':
        return 'warning';
      case 'success':
        return 'success';
      default:
        return 'info';
    }
  };

  if (compact) {
    return (
      <Box sx={{ mt: 1 }}>
        <Box display="flex" alignItems="center" gap={1}>
          {hasErrors && (
            <Chip
              icon={<ErrorIcon />}
              label={`${errors.length} error${errors.length !== 1 ? 's' : ''}`}
              color="error"
              size="small"
              variant="outlined"
            />
          )}
          {hasWarnings && (
            <Chip
              icon={<WarningIcon />}
              label={`${warnings.length} warning${warnings.length !== 1 ? 's' : ''}`}
              color="warning"
              size="small"
              variant="outlined"
            />
          )}
          {showSuggestions && hasSuggestions && (
            <Chip
              icon={<SuggestionIcon />}
              label={`${suggestions.length} suggestion${suggestions.length !== 1 ? 's' : ''}`}
              color="info"
              size="small"
              variant="outlined"
            />
          )}
          {hasAnyFeedback && (
            <Tooltip title={expanded ? 'Collapse details' : 'Expand details'}>
              <IconButton
                size="small"
                onClick={() => setExpanded(!expanded)}
                sx={{ ml: 'auto' }}
              >
                {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
              </IconButton>
            </Tooltip>
          )}
        </Box>

        <Collapse in={expanded}>
          <Box sx={{ mt: 1 }}>
            {hasErrors && (
              <Alert severity="error" sx={{ mb: 1 }}>
                <AlertTitle>Errors</AlertTitle>
                <List dense>
                  {errors.map((error, index) => (
                    <ListItem key={index} sx={{ py: 0 }}>
                      <ListItemText primary={error} />
                    </ListItem>
                  ))}
                </List>
              </Alert>
            )}

            {hasWarnings && (
              <Alert severity="warning" sx={{ mb: 1 }}>
                <AlertTitle>Warnings</AlertTitle>
                <List dense>
                  {warnings.map((warning, index) => (
                    <ListItem key={index} sx={{ py: 0 }}>
                      <ListItemText primary={warning} />
                    </ListItem>
                  ))}
                </List>
              </Alert>
            )}

            {showSuggestions && hasSuggestions && (
              <Alert severity="info">
                <AlertTitle>Suggestions</AlertTitle>
                <List dense>
                  {suggestions.map((suggestion, index) => (
                    <ListItem key={index} sx={{ py: 0 }}>
                      <ListItemIcon sx={{ minWidth: 32 }}>
                        <SuggestionIcon fontSize="small" />
                      </ListItemIcon>
                      <ListItemText primary={suggestion} />
                    </ListItem>
                  ))}
                </List>
              </Alert>
            )}
          </Box>
        </Collapse>
      </Box>
    );
  }

  return (
    <Box sx={{ mt: 2 }}>
      {hasErrors && (
        <Alert severity="error" sx={{ mb: 2 }}>
          <AlertTitle>
            {errors.length} Error{errors.length !== 1 ? 's' : ''} Found
          </AlertTitle>
          <List>
            {errors.map((error, index) => (
              <ListItem key={index}>
                <ListItemIcon>
                  <ErrorIcon color="error" />
                </ListItemIcon>
                <ListItemText primary={error} />
              </ListItem>
            ))}
          </List>
        </Alert>
      )}

      {hasWarnings && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <AlertTitle>
            {warnings.length} Warning{warnings.length !== 1 ? 's' : ''} Found
          </AlertTitle>
          <List>
            {warnings.map((warning, index) => (
              <ListItem key={index}>
                <ListItemIcon>
                  <WarningIcon color="warning" />
                </ListItemIcon>
                <ListItemText primary={warning} />
              </ListItem>
            ))}
          </List>
        </Alert>
      )}

      {showSuggestions && hasSuggestions && (
        <Alert severity="info">
          <AlertTitle>Suggestions</AlertTitle>
          <List>
            {suggestions.map((suggestion, index) => (
              <ListItem key={index}>
                <ListItemIcon>
                  <SuggestionIcon color="info" />
                </ListItemIcon>
                <ListItemText primary={suggestion} />
              </ListItem>
            ))}
          </List>
        </Alert>
      )}
    </Box>
  );
};

export interface FieldValidationFeedbackProps {
  fieldPath: string;
  errors?: string[];
  warnings?: string[];
  suggestions?: string[];
  showSuggestions?: boolean;
  inline?: boolean;
}

export const FieldValidationFeedback: React.FC<FieldValidationFeedbackProps> = ({
  fieldPath,
  errors = [],
  warnings = [],
  suggestions = [],
  showSuggestions = true,
  inline = true
}) => {
  const hasErrors = errors.length > 0;
  const hasWarnings = warnings.length > 0;
  const hasSuggestions = suggestions.length > 0;

  if (!hasErrors && !hasWarnings && (!showSuggestions || !hasSuggestions)) {
    return null;
  }

  if (inline) {
    return (
      <Box sx={{ mt: 0.5 }}>
        {hasErrors && (
          <Typography variant="caption" color="error" display="block">
            {errors.join('. ')}
          </Typography>
        )}
        {hasWarnings && (
          <Typography variant="caption" color="warning.main" display="block">
            {warnings.join('. ')}
          </Typography>
        )}
        {showSuggestions && hasSuggestions && (
          <Typography variant="caption" color="info.main" display="block">
            💡 {suggestions.join('. ')}
          </Typography>
        )}
      </Box>
    );
  }

  return (
    <ValidationFeedback
      errors={errors}
      warnings={warnings}
      suggestions={showSuggestions ? suggestions : []}
      showSuggestions={showSuggestions}
      compact={true}
    />
  );
};

export interface ValidationSummaryProps {
  errorCount: number;
  warningCount: number;
  isValidating?: boolean;
  showDetails?: boolean;
  onToggleDetails?: () => void;
}

export const ValidationSummary: React.FC<ValidationSummaryProps> = ({
  errorCount,
  warningCount,
  isValidating = false,
  showDetails = false,
  onToggleDetails
}) => {
  const hasErrors = errorCount > 0;
  const hasWarnings = warningCount > 0;
  const hasIssues = hasErrors || hasWarnings;

  if (isValidating) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        <Box display="flex" alignItems="center" gap={1}>
          <InfoIcon />
          <Typography>Validating configuration...</Typography>
        </Box>
      </Alert>
    );
  }

  if (!hasIssues) {
    return (
      <Alert severity="success" sx={{ mb: 2 }}>
        <Box display="flex" alignItems="center" gap={1}>
          <CheckCircleIcon />
          <Typography>Configuration is valid</Typography>
        </Box>
      </Alert>
    );
  }

  const severity = hasErrors ? 'error' : 'warning';
  const title = hasErrors 
    ? `${errorCount} error${errorCount !== 1 ? 's' : ''} found`
    : `${warningCount} warning${warningCount !== 1 ? 's' : ''} found`;

  return (
    <Alert severity={severity} sx={{ mb: 2 }}>
      <Box display="flex" alignItems="center" justifyContent="space-between" width="100%">
        <Box display="flex" alignItems="center" gap={1}>
          {hasErrors ? <ErrorIcon /> : <WarningIcon />}
          <Typography>{title}</Typography>
          {hasErrors && hasWarnings && (
            <Chip
              label={`${warningCount} warning${warningCount !== 1 ? 's' : ''}`}
              color="warning"
              size="small"
              variant="outlined"
            />
          )}
        </Box>
        
        {onToggleDetails && (
          <IconButton
            size="small"
            onClick={onToggleDetails}
            color="inherit"
          >
            {showDetails ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
        )}
      </Box>
    </Alert>
  );
};