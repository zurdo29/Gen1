import React from 'react';
import {
  TextField,
  TextFieldProps,
  FormControl,
  FormLabel,
  FormHelperText,
  InputAdornment,
  IconButton,
  Tooltip,
  Box,
  Autocomplete,
  AutocompleteProps,
  Chip,
} from '@mui/material';
import {
  Error as ErrorIcon,
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Help as HelpIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { FieldValidationFeedback } from './ValidationFeedback';

export interface ValidatedInputProps extends Omit<TextFieldProps, 'error' | 'helperText'> {
  fieldPath: string;
  errors?: string[];
  warnings?: string[];
  suggestions?: string[];
  showSuggestions?: boolean;
  onValidate?: (value: any) => void;
  validationStatus?: 'valid' | 'warning' | 'error' | 'validating';
  showValidationIcon?: boolean;
  helpText?: string;
  onRefresh?: () => void;
}

export const ValidatedInput: React.FC<ValidatedInputProps> = ({
  fieldPath,
  errors = [],
  warnings = [],
  suggestions = [],
  showSuggestions = true,
  onValidate,
  validationStatus = 'valid',
  showValidationIcon = true,
  helpText,
  onRefresh,
  onChange,
  onBlur,
  ...textFieldProps
}) => {
  const hasErrors = errors.length > 0;
  const hasWarnings = warnings.length > 0;
  const isValidating = validationStatus === 'validating';

  const getValidationIcon = () => {
    if (isValidating) {
      return <RefreshIcon className="rotating" />;
    }
    
    switch (validationStatus) {
      case 'error':
        return <ErrorIcon color="error" />;
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'valid':
        return hasWarnings ? <WarningIcon color="warning" /> : <CheckCircleIcon color="success" />;
      default:
        return null;
    }
  };

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(event);
    if (onValidate) {
      onValidate(event.target.value);
    }
  };

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    onBlur?.(event);
    if (onValidate) {
      onValidate(event.target.value);
    }
  };

  const endAdornment = (
    <InputAdornment position="end">
      <Box display="flex" alignItems="center" gap={0.5}>
        {helpText && (
          <Tooltip title={helpText} arrow>
            <IconButton size="small" edge="end">
              <HelpIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
        
        {onRefresh && (
          <Tooltip title="Refresh validation" arrow>
            <IconButton size="small" onClick={onRefresh} edge="end">
              <RefreshIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
        
        {showValidationIcon && getValidationIcon()}
        
        {textFieldProps.InputProps?.endAdornment}
      </Box>
    </InputAdornment>
  );

  return (
    <FormControl fullWidth error={hasErrors} variant={textFieldProps.variant}>
      <TextField
        {...textFieldProps}
        error={hasErrors}
        onChange={handleChange}
        onBlur={handleBlur}
        InputProps={{
          ...textFieldProps.InputProps,
          endAdornment
        }}
        sx={{
          '& .rotating': {
            animation: 'spin 1s linear infinite',
          },
          '@keyframes spin': {
            '0%': {
              transform: 'rotate(0deg)',
            },
            '100%': {
              transform: 'rotate(360deg)',
            },
          },
          ...textFieldProps.sx
        }}
      />
      
      <FieldValidationFeedback
        fieldPath={fieldPath}
        errors={errors}
        warnings={warnings}
        suggestions={suggestions}
        showSuggestions={showSuggestions}
        inline={true}
      />
    </FormControl>
  );
};

export interface ValidatedAutocompleteProps<T> extends Omit<AutocompleteProps<T, boolean, boolean, boolean>, 'renderInput'> {
  fieldPath: string;
  label: string;
  errors?: string[];
  warnings?: string[];
  suggestions?: string[];
  showSuggestions?: boolean;
  onValidate?: (value: T | null) => void;
  validationStatus?: 'valid' | 'warning' | 'error' | 'validating';
  showValidationIcon?: boolean;
  helpText?: string;
  textFieldProps?: Partial<TextFieldProps>;
}

export const ValidatedAutocomplete = <T,>({
  fieldPath,
  label,
  errors = [],
  warnings = [],
  suggestions = [],
  showSuggestions = true,
  onValidate,
  validationStatus = 'valid',
  showValidationIcon = true,
  helpText,
  textFieldProps = {},
  onChange,
  ...autocompleteProps
}: ValidatedAutocompleteProps<T>) => {
  const hasErrors = errors.length > 0;
  const hasWarnings = warnings.length > 0;
  const isValidating = validationStatus === 'validating';

  const getValidationIcon = () => {
    if (isValidating) {
      return <RefreshIcon className="rotating" />;
    }
    
    switch (validationStatus) {
      case 'error':
        return <ErrorIcon color="error" />;
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'valid':
        return hasWarnings ? <WarningIcon color="warning" /> : <CheckCircleIcon color="success" />;
      default:
        return null;
    }
  };

  const handleChange = (event: React.SyntheticEvent, value: T | null) => {
    onChange?.(event, value);
    if (onValidate) {
      onValidate(value);
    }
  };

  return (
    <FormControl fullWidth error={hasErrors}>
      <Autocomplete
        {...autocompleteProps}
        onChange={handleChange}
        renderInput={(params) => (
          <TextField
            {...params}
            {...textFieldProps}
            label={label}
            error={hasErrors}
            InputProps={{
              ...params.InputProps,
              ...textFieldProps.InputProps,
              endAdornment: (
                <Box display="flex" alignItems="center">
                  {params.InputProps.endAdornment}
                  <InputAdornment position="end">
                    <Box display="flex" alignItems="center" gap={0.5}>
                      {helpText && (
                        <Tooltip title={helpText} arrow>
                          <IconButton size="small">
                            <HelpIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                      {showValidationIcon && getValidationIcon()}
                    </Box>
                  </InputAdornment>
                </Box>
              )
            }}
            sx={{
              '& .rotating': {
                animation: 'spin 1s linear infinite',
              },
              '@keyframes spin': {
                '0%': {
                  transform: 'rotate(0deg)',
                },
                '100%': {
                  transform: 'rotate(360deg)',
                },
              },
              ...textFieldProps.sx
            }}
          />
        )}
      />
      
      <FieldValidationFeedback
        fieldPath={fieldPath}
        errors={errors}
        warnings={warnings}
        suggestions={suggestions}
        showSuggestions={showSuggestions}
        inline={true}
      />
    </FormControl>
  );
};

export interface ValidatedChipInputProps {
  fieldPath: string;
  label: string;
  value: string[];
  onChange: (value: string[]) => void;
  options?: string[];
  errors?: string[];
  warnings?: string[];
  suggestions?: string[];
  showSuggestions?: boolean;
  onValidate?: (value: string[]) => void;
  validationStatus?: 'valid' | 'warning' | 'error' | 'validating';
  helpText?: string;
  placeholder?: string;
  maxItems?: number;
}

export const ValidatedChipInput: React.FC<ValidatedChipInputProps> = ({
  fieldPath,
  label,
  value,
  onChange,
  options = [],
  errors = [],
  warnings = [],
  suggestions = [],
  showSuggestions = true,
  onValidate,
  validationStatus: _validationStatus = 'valid',
  helpText,
  placeholder,
  maxItems
}) => {
  const hasErrors = errors.length > 0;
  const [inputValue, setInputValue] = React.useState('');

  const handleChange = (newValue: string[]) => {
    onChange(newValue);
    if (onValidate) {
      onValidate(newValue);
    }
  };

  const handleDelete = (chipToDelete: string) => {
    const newValue = value.filter(chip => chip !== chipToDelete);
    handleChange(newValue);
  };

  const availableOptions = options.filter(option => !value.includes(option));

  return (
    <FormControl fullWidth error={hasErrors}>
      <FormLabel component="legend" sx={{ mb: 1 }}>
        {label}
        {helpText && (
          <Tooltip title={helpText} arrow>
            <IconButton size="small" sx={{ ml: 0.5 }}>
              <HelpIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
      </FormLabel>
      
      <Box sx={{ mb: 1 }}>
        {value.map((chip) => (
          <Chip
            key={chip}
            label={chip}
            onDelete={() => handleDelete(chip)}
            color={hasErrors ? 'error' : 'default'}
            sx={{ mr: 0.5, mb: 0.5 }}
          />
        ))}
      </Box>

      {(!maxItems || value.length < maxItems) && (
        <Autocomplete
          options={availableOptions}
          value={null}
          inputValue={inputValue}
          onInputChange={(event, newInputValue) => {
            setInputValue(newInputValue);
          }}
          onChange={(event, newValue) => {
            if (newValue && !value.includes(newValue)) {
              handleChange([...value, newValue]);
              setInputValue('');
            }
          }}
          renderInput={(params) => (
            <TextField
              {...params}
              placeholder={placeholder || `Add ${label.toLowerCase()}...`}
              size="small"
              error={hasErrors}
            />
          )}
          freeSolo
          clearOnBlur
          handleHomeEndKeys
        />
      )}

      {maxItems && value.length >= maxItems && (
        <FormHelperText>
          Maximum {maxItems} items allowed
        </FormHelperText>
      )}
      
      <FieldValidationFeedback
        fieldPath={fieldPath}
        errors={errors}
        warnings={warnings}
        suggestions={suggestions}
        showSuggestions={showSuggestions}
        inline={true}
      />
    </FormControl>
  );
};