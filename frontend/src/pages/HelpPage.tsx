import React from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import {
  Box,
  Typography,
  Container,
  Tabs,
  Tab,
  Paper,
  Alert,
  AlertTitle,
  Button,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
} from '@mui/material';
import {
  Help as HelpIcon,
  BugReport as BugReportIcon,
  ContactSupport as ContactSupportIcon,
  Description as DocumentationIcon,
  QuestionAnswer as FAQIcon,
  Launch as LaunchIcon,
} from '@mui/icons-material';
import { TroubleshootingGuide } from '../components/common/TroubleshootingGuide';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`help-tabpanel-${index}`}
      aria-labelledby={`help-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const faqItems = [
  {
    question: "How do I create my first level?",
    answer: "Start by selecting a preset configuration from the dropdown menu, then click 'Generate Level' to create your first procedural level. You can then customize the parameters and regenerate as needed."
  },
  {
    question: "What export formats are supported?",
    answer: "We support JSON, XML, CSV, and Unity-compatible formats. JSON is recommended for maximum compatibility and smallest file size."
  },
  {
    question: "Why is my level generation taking so long?",
    answer: "Large levels (over 100x100) or complex configurations can take longer to generate. Try reducing the level size or entity count for faster generation."
  },
  {
    question: "Can I save my configurations?",
    answer: "Yes! You can save custom configurations as presets and share them with others using the share button."
  },
  {
    question: "How do I report a bug?",
    answer: "Use the 'Report Issue' button in error dialogs, or contact us through the support form with detailed information about the problem."
  },
  {
    question: "Is there an offline mode?",
    answer: "Yes, the application supports offline mode for basic functionality. Enable it in the settings menu."
  }
];

export const HelpPage: React.FC = () => {
  const { section } = useParams<{ section?: string }>();
  const [searchParams] = useSearchParams();
  const [tabValue, setTabValue] = React.useState(0);

  const errorParam = searchParams.get('error');
  const operationParam = searchParams.get('operation');

  React.useEffect(() => {
    if (section === 'troubleshooting') {
      setTabValue(1);
    } else if (section === 'faq') {
      setTabValue(2);
    } else if (section === 'contact') {
      setTabValue(3);
    }
  }, [section]);

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h3" gutterBottom>
          Help & Support
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Find answers to common questions and get help with the Procedural Level Generator.
        </Typography>
      </Box>

      {errorParam && (
        <Alert severity="info" sx={{ mb: 3 }}>
          <AlertTitle>Error Report</AlertTitle>
          You were directed here from an error (ID: {errorParam})
          {operationParam && ` during ${decodeURIComponent(operationParam)}`}.
          Check the troubleshooting section below for solutions.
        </Alert>
      )}

      <Paper sx={{ width: '100%' }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="help sections"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab icon={<HelpIcon />} label="Overview" />
          <Tab icon={<BugReportIcon />} label="Troubleshooting" />
          <Tab icon={<FAQIcon />} label="FAQ" />
          <Tab icon={<ContactSupportIcon />} label="Contact" />
        </Tabs>

        <TabPanel value={tabValue} index={0}>
          <Box>
            <Typography variant="h5" gutterBottom>
              Getting Started
            </Typography>
            <Typography variant="body1" paragraph>
              Welcome to the Procedural Level Generator! This tool helps you create
              procedural levels for games using various algorithms and parameters.
            </Typography>

            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 3, mt: 3 }}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" gap={1} mb={2}>
                    <HelpIcon color="primary" />
                    <Typography variant="h6">Quick Start</Typography>
                  </Box>
                  <List dense>
                    <ListItem>
                      <ListItemText primary="1. Choose a preset configuration" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="2. Adjust parameters as needed" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="3. Generate your level" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="4. Export in your preferred format" />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>

              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" gap={1} mb={2}>
                    <DocumentationIcon color="primary" />
                    <Typography variant="h6">Key Features</Typography>
                  </Box>
                  <List dense>
                    <ListItem>
                      <ListItemText primary="Real-time level preview" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Multiple export formats" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Batch generation" />
                    </ListItem>
                    <ListItem>
                      <ListItemText primary="Configuration sharing" />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Box>

            <Box sx={{ mt: 4 }}>
              <Typography variant="h6" gutterBottom>
                Need More Help?
              </Typography>
              <Box display="flex" gap={2} flexWrap="wrap">
                <Button
                  variant="outlined"
                  startIcon={<BugReportIcon />}
                  onClick={() => setTabValue(1)}
                >
                  Troubleshooting
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<FAQIcon />}
                  onClick={() => setTabValue(2)}
                >
                  FAQ
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<LaunchIcon />}
                  onClick={() => window.open('/docs', '_blank')}
                >
                  Documentation
                </Button>
              </Box>
            </Box>
          </Box>
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          <TroubleshootingGuide />
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <Box>
            <Typography variant="h5" gutterBottom>
              Frequently Asked Questions
            </Typography>
            
            {faqItems.map((item, index) => (
              <Card key={index} sx={{ mb: 2 }}>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {item.question}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {item.answer}
                  </Typography>
                </CardContent>
              </Card>
            ))}

            <Box sx={{ mt: 4, p: 3, bgcolor: 'grey.50', borderRadius: 1 }}>
              <Typography variant="h6" gutterBottom>
                Can't find what you're looking for?
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                If your question isn't answered here, try the troubleshooting guide
                or contact our support team.
              </Typography>
              <Box display="flex" gap={2}>
                <Button
                  variant="outlined"
                  startIcon={<BugReportIcon />}
                  onClick={() => setTabValue(1)}
                >
                  Troubleshooting
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<ContactSupportIcon />}
                  onClick={() => setTabValue(3)}
                >
                  Contact Support
                </Button>
              </Box>
            </Box>
          </Box>
        </TabPanel>

        <TabPanel value={tabValue} index={3}>
          <Box>
            <Typography variant="h5" gutterBottom>
              Contact Support
            </Typography>
            <Typography variant="body1" paragraph>
              Need personalized help? Our support team is here to assist you.
            </Typography>

            <Card sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Before Contacting Support
                </Typography>
                <List>
                  <ListItem>
                    <ListItemIcon>
                      <BugReportIcon />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Check the troubleshooting guide"
                      secondary="Many common issues have quick solutions"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <FAQIcon />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Review the FAQ section"
                      secondary="Your question might already be answered"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <DocumentationIcon />
                    </ListItemIcon>
                    <ListItemText 
                      primary="Try reproducing the issue"
                      secondary="Note the exact steps that led to the problem"
                    />
                  </ListItem>
                </List>
              </CardContent>
            </Card>

            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Support Information
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  When contacting support, please include:
                </Typography>
                <List dense>
                  <ListItem>
                    <ListItemText primary="• Error messages or codes" />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="• Steps to reproduce the issue" />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="• Your browser and operating system" />
                  </ListItem>
                  <ListItem>
                    <ListItemText primary="• Configuration details (if applicable)" />
                  </ListItem>
                </List>

                <Divider sx={{ my: 2 }} />

                <Box display="flex" gap={2} flexWrap="wrap">
                  <Button
                    variant="contained"
                    startIcon={<ContactSupportIcon />}
                    onClick={() => window.open('mailto:support@example.com', '_blank')}
                  >
                    Email Support
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<LaunchIcon />}
                    onClick={() => window.open('/support/ticket', '_blank')}
                  >
                    Submit Ticket
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<LaunchIcon />}
                    onClick={() => window.open('/community', '_blank')}
                  >
                    Community Forum
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Box>
        </TabPanel>
      </Paper>
    </Container>
  );
};