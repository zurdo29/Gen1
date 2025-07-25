import React from 'react';
import {
  Box,
  Typography,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Alert,
  AlertTitle,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  _Link,
  Button,
  Card,
  CardContent,
  Divider,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  CheckCircle as CheckCircleIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  Launch as LaunchIcon,
  Refresh as RefreshIcon,
  Settings as SettingsIcon,
} from '@mui/icons-material';

interface TroubleshootingSection {
  id: string;
  title: string;
  description: string;
  severity: 'error' | 'warning' | 'info';
  solutions: Solution[];
  commonCauses?: string[];
  preventionTips?: string[];
}

interface Solution {
  title: string;
  steps: string[];
  difficulty: 'easy' | 'medium' | 'advanced';
  estimatedTime: string;
  success?: () => void;
}

const troubleshootingSections: TroubleshootingSection[] = [
  {
    id: 'configuration-errors',
    title: 'Configuration Errors',
    description: 'Issues with generation parameters and settings',
    severity: 'warning',
    solutions: [
      {
        title: 'Reset to Default Configuration',
        steps: [
          'Click the "Reset" button in the configuration panel',
          'Verify that all required fields are filled',
          'Try generating a level with default settings',
          'Gradually modify parameters to identify the problematic setting'
        ],
        difficulty: 'easy',
        estimatedTime: '2-3 minutes'
      },
      {
        title: 'Validate Individual Parameters',
        steps: [
          'Check that level dimensions are within limits (1-1000)',
          'Ensure entity counts are reasonable (0-500 per type)',
          'Verify that seed values are positive integers',
          'Confirm algorithm parameters match expected types'
        ],
        difficulty: 'medium',
        estimatedTime: '5-10 minutes'
      }
    ],
    commonCauses: [
      'Invalid parameter values (negative numbers, out of range)',
      'Conflicting settings between different parameters',
      'Missing required configuration fields',
      'Corrupted preset data'
    ],
    preventionTips: [
      'Use presets as starting points for custom configurations',
      'Test configurations with small level sizes first',
      'Save working configurations as custom presets'
    ]
  },
  {
    id: 'generation-errors',
    title: 'Level Generation Failures',
    description: 'Problems during the level generation process',
    severity: 'error',
    solutions: [
      {
        title: 'Reduce Level Complexity',
        steps: [
          'Decrease level dimensions (try 50x50 or smaller)',
          'Reduce the number of entities per type',
          'Simplify terrain generation parameters',
          'Use less complex generation algorithms'
        ],
        difficulty: 'easy',
        estimatedTime: '3-5 minutes'
      },
      {
        title: 'Check System Resources',
        steps: [
          'Close other browser tabs and applications',
          'Refresh the page to clear memory',
          'Try generating during off-peak hours',
          'Consider using a more powerful device'
        ],
        difficulty: 'easy',
        estimatedTime: '2-3 minutes'
      },
      {
        title: 'Adjust Generation Timeout',
        steps: [
          'Try generating smaller levels first',
          'Use simpler algorithms for large levels',
          'Generate levels in batches rather than all at once',
          'Contact support if timeouts persist'
        ],
        difficulty: 'medium',
        estimatedTime: '5-10 minutes'
      }
    ],
    commonCauses: [
      'Level too large or complex for available memory',
      'Generation algorithm timeout',
      'Invalid parameter combinations',
      'Server overload or maintenance'
    ],
    preventionTips: [
      'Start with smaller levels and gradually increase size',
      'Test new configurations with simple parameters first',
      'Monitor generation time and adjust complexity accordingly'
    ]
  },
  {
    id: 'export-errors',
    title: 'Export and Download Issues',
    description: 'Problems when exporting or downloading levels',
    severity: 'warning',
    solutions: [
      {
        title: 'Try Different Export Format',
        steps: [
          'Switch to JSON format (most reliable)',
          'Reduce export file size by excluding metadata',
          'Try exporting individual levels instead of batches',
          'Use CSV format for simple data exports'
        ],
        difficulty: 'easy',
        estimatedTime: '2-3 minutes'
      },
      {
        title: 'Check Browser Settings',
        steps: [
          'Ensure downloads are enabled in browser settings',
          'Check if popup blocker is preventing downloads',
          'Try using a different browser',
          'Clear browser cache and cookies'
        ],
        difficulty: 'medium',
        estimatedTime: '5-10 minutes'
      }
    ],
    commonCauses: [
      'Export file too large for browser limits',
      'Unsupported export format for level type',
      'Browser download restrictions',
      'Network interruption during download'
    ],
    preventionTips: [
      'Export smaller batches of levels',
      'Use JSON format for maximum compatibility',
      'Test exports with simple levels first'
    ]
  },
  {
    id: 'network-errors',
    title: 'Connection and Network Issues',
    description: 'Problems connecting to the server or loading content',
    severity: 'error',
    solutions: [
      {
        title: 'Check Internet Connection',
        steps: [
          'Verify internet connection is working',
          'Try accessing other websites',
          'Restart your router/modem if needed',
          'Switch to a different network if available'
        ],
        difficulty: 'easy',
        estimatedTime: '3-5 minutes'
      },
      {
        title: 'Clear Browser Data',
        steps: [
          'Clear browser cache and cookies',
          'Disable browser extensions temporarily',
          'Try using incognito/private browsing mode',
          'Restart your browser'
        ],
        difficulty: 'easy',
        estimatedTime: '2-3 minutes'
      },
      {
        title: 'Use Offline Mode',
        steps: [
          'Enable offline mode in settings',
          'Use cached configurations and presets',
          'Generate levels with previously loaded data',
          'Sync changes when connection is restored'
        ],
        difficulty: 'medium',
        estimatedTime: '5-10 minutes'
      }
    ],
    commonCauses: [
      'Internet connection problems',
      'Server maintenance or downtime',
      'Firewall or proxy blocking requests',
      'Browser cache corruption'
    ],
    preventionTips: [
      'Enable offline mode for critical work',
      'Save configurations locally regularly',
      'Use stable internet connection for large operations'
    ]
  },
  {
    id: 'performance-issues',
    title: 'Performance and Speed Problems',
    description: 'Slow loading, generation, or interface responsiveness',
    severity: 'info',
    solutions: [
      {
        title: 'Optimize Browser Performance',
        steps: [
          'Close unnecessary browser tabs',
          'Disable unused browser extensions',
          'Clear browser cache and restart',
          'Update browser to latest version'
        ],
        difficulty: 'easy',
        estimatedTime: '3-5 minutes'
      },
      {
        title: 'Reduce Application Load',
        steps: [
          'Generate smaller levels to test performance',
          'Disable real-time preview for complex levels',
          'Use batch generation for multiple levels',
          'Close other applications using system resources'
        ],
        difficulty: 'medium',
        estimatedTime: '5-10 minutes'
      }
    ],
    commonCauses: [
      'Large or complex level configurations',
      'Limited system memory or processing power',
      'Multiple browser tabs or applications running',
      'Outdated browser or system software'
    ],
    preventionTips: [
      'Start with simple configurations and gradually increase complexity',
      'Monitor system resources during generation',
      'Use appropriate level sizes for your system capabilities'
    ]
  }
];

interface TroubleshootingGuideProps {
  sectionId?: string;
  compact?: boolean;
}

export const TroubleshootingGuide: React.FC<TroubleshootingGuideProps> = ({
  sectionId,
  compact = false
}) => {
  const [expandedSection, setExpandedSection] = React.useState<string | false>(
    sectionId || false
  );

  const handleSectionChange = (panel: string) => (
    event: React.SyntheticEvent,
    isExpanded: boolean
  ) => {
    setExpandedSection(isExpanded ? panel : false);
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'error':
        return <ErrorIcon color="error" />;
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'info':
        return <InfoIcon color="info" />;
      default:
        return <InfoIcon />;
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'easy':
        return 'success';
      case 'medium':
        return 'warning';
      case 'advanced':
        return 'error';
      default:
        return 'default';
    }
  };

  const sectionsToShow = sectionId 
    ? troubleshootingSections.filter(section => section.id === sectionId)
    : troubleshootingSections;

  if (compact && sectionId) {
    const section = troubleshootingSections.find(s => s.id === sectionId);
    if (!section) return null;

    return (
      <Card variant="outlined">
        <CardContent>
          <Box display="flex" alignItems="center" gap={1} mb={2}>
            {getSeverityIcon(section.severity)}
            <Typography variant="h6">{section.title}</Typography>
          </Box>
          
          <Typography variant="body2" color="text.secondary" paragraph>
            {section.description}
          </Typography>

          {section.solutions.slice(0, 2).map((solution, index) => (
            <Box key={index} mb={2}>
              <Box display="flex" alignItems="center" gap={1} mb={1}>
                <Typography variant="subtitle2">{solution.title}</Typography>
                <Chip
                  label={solution.difficulty}
                  size="small"
                  color={getDifficultyColor(solution.difficulty) as any}
                  variant="outlined"
                />
              </Box>
              
              <List dense>
                {solution.steps.slice(0, 3).map((step, stepIndex) => (
                  <ListItem key={stepIndex} sx={{ py: 0.5 }}>
                    <ListItemIcon sx={{ minWidth: 32 }}>
                      <CheckCircleIcon fontSize="small" color="action" />
                    </ListItemIcon>
                    <ListItemText 
                      primary={step}
                      primaryTypographyProps={{ variant: 'body2' }}
                    />
                  </ListItem>
                ))}
              </List>
            </Box>
          ))}

          <Button
            size="small"
            startIcon={<LaunchIcon />}
            onClick={() => window.open(`/help/troubleshooting#${section.id}`, '_blank')}
          >
            View Full Guide
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Box>
      {!sectionId && (
        <Box mb={3}>
          <Typography variant="h4" gutterBottom>
            Troubleshooting Guide
          </Typography>
          <Typography variant="body1" color="text.secondary" paragraph>
            Find solutions to common problems and learn how to resolve issues quickly.
          </Typography>
        </Box>
      )}

      {sectionsToShow.map((section) => (
        <Accordion
          key={section.id}
          expanded={expandedSection === section.id}
          onChange={handleSectionChange(section.id)}
          sx={{ mb: 1 }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box display="flex" alignItems="center" gap={2} width="100%">
              {getSeverityIcon(section.severity)}
              <Box>
                <Typography variant="h6">{section.title}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {section.description}
                </Typography>
              </Box>
            </Box>
          </AccordionSummary>
          
          <AccordionDetails>
            <Box>
              {section.commonCauses && (
                <Alert severity="info" sx={{ mb: 3 }}>
                  <AlertTitle>Common Causes</AlertTitle>
                  <List dense>
                    {section.commonCauses.map((cause, index) => (
                      <ListItem key={index} sx={{ py: 0 }}>
                        <ListItemText primary={cause} />
                      </ListItem>
                    ))}
                  </List>
                </Alert>
              )}

              <Typography variant="h6" gutterBottom>
                Solutions
              </Typography>

              {section.solutions.map((solution, index) => (
                <Card key={index} variant="outlined" sx={{ mb: 2 }}>
                  <CardContent>
                    <Box display="flex" alignItems="center" gap={1} mb={2}>
                      <Typography variant="subtitle1">{solution.title}</Typography>
                      <Chip
                        label={solution.difficulty}
                        size="small"
                        color={getDifficultyColor(solution.difficulty) as any}
                        variant="outlined"
                      />
                      <Chip
                        label={solution.estimatedTime}
                        size="small"
                        variant="outlined"
                      />
                    </Box>

                    <List>
                      {solution.steps.map((step, stepIndex) => (
                        <ListItem key={stepIndex}>
                          <ListItemIcon>
                            <CheckCircleIcon color="primary" />
                          </ListItemIcon>
                          <ListItemText primary={step} />
                        </ListItem>
                      ))}
                    </List>

                    {solution.success && (
                      <Button
                        variant="outlined"
                        startIcon={<SettingsIcon />}
                        onClick={solution.success}
                        sx={{ mt: 1 }}
                      >
                        Apply This Solution
                      </Button>
                    )}
                  </CardContent>
                </Card>
              ))}

              {section.preventionTips && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Alert severity="success">
                    <AlertTitle>Prevention Tips</AlertTitle>
                    <List dense>
                      {section.preventionTips.map((tip, index) => (
                        <ListItem key={index} sx={{ py: 0 }}>
                          <ListItemText primary={tip} />
                        </ListItem>
                      ))}
                    </List>
                  </Alert>
                </>
              )}
            </Box>
          </AccordionDetails>
        </Accordion>
      ))}

      {!sectionId && (
        <Box mt={4} p={3} bgcolor="grey.50" borderRadius={1}>
          <Typography variant="h6" gutterBottom>
            Still Need Help?
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            If you can't find a solution to your problem, try these additional resources:
          </Typography>
          
          <Box display="flex" gap={2} flexWrap="wrap">
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={() => window.location.reload()}
            >
              Refresh Page
            </Button>
            
            <Button
              variant="outlined"
              startIcon={<LaunchIcon />}
              onClick={() => window.open('/support', '_blank')}
            >
              Contact Support
            </Button>
            
            <Button
              variant="outlined"
              startIcon={<LaunchIcon />}
              onClick={() => window.open('/docs', '_blank')}
            >
              View Documentation
            </Button>
          </Box>
        </Box>
      )}
    </Box>
  );
};