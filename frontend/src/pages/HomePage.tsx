import React from 'react';
import {
  Typography,
  Box,
  Paper,
  Grid,
  Button,
  Card,
  CardContent,
  CardActions,
} from '@mui/material';
import {
  Add as AddIcon,
  Folder as FolderIcon,
  Share as ShareIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

export const HomePage: React.FC = () => {
  const navigate = useNavigate();

  const handleNewProject = () => {
    navigate('/editor');
  };

  const handleLoadPreset = () => {
    // TODO: Implement preset loading
    console.log('Load preset clicked');
  };

  const handleImportShared = () => {
    // TODO: Implement shared configuration import
    console.log('Import shared clicked');
  };

  return (
    <Box>
      <Typography variant="h3" component="h1" gutterBottom>
        Welcome to Procedural Level Editor
      </Typography>
      
      <Typography variant="h6" color="text.secondary" paragraph>
        Create, edit, and export procedural game levels with an intuitive visual interface.
      </Typography>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                New Project
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Start creating a new procedural level from scratch with customizable parameters.
              </Typography>
            </CardContent>
            <CardActions>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={handleNewProject}
                fullWidth
              >
                Create New Level
              </Button>
            </CardActions>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Load Preset
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Choose from saved configuration presets to quickly start with proven settings.
              </Typography>
            </CardContent>
            <CardActions>
              <Button
                variant="outlined"
                startIcon={<FolderIcon />}
                onClick={handleLoadPreset}
                fullWidth
              >
                Browse Presets
              </Button>
            </CardActions>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Import Shared
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Import a level configuration shared by another user via URL or file.
              </Typography>
            </CardContent>
            <CardActions>
              <Button
                variant="outlined"
                startIcon={<ShareIcon />}
                onClick={handleImportShared}
                fullWidth
              >
                Import Configuration
              </Button>
            </CardActions>
          </Card>
        </Grid>
      </Grid>

      <Paper sx={{ p: 3, mt: 4 }}>
        <Typography variant="h5" gutterBottom>
          Features
        </Typography>
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6} md={3}>
            <Typography variant="h6" gutterBottom>
              Real-time Preview
            </Typography>
            <Typography variant="body2" color="text.secondary">
              See your level changes instantly with our interactive canvas preview.
            </Typography>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Typography variant="h6" gutterBottom>
              Multiple Formats
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Export to JSON, XML, CSV, Unity prefabs, and more game engine formats.
            </Typography>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Typography variant="h6" gutterBottom>
              Manual Editing
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Fine-tune generated levels with drag-and-drop editing tools.
            </Typography>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Typography variant="h6" gutterBottom>
              Team Collaboration
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Share configurations and collaborate with your team seamlessly.
            </Typography>
          </Grid>
        </Grid>
      </Paper>
    </Box>
  );
};