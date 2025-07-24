import React, { useState } from 'react';
import {
  Paper,
  Typography,
  TextField,
  Button,
  Box,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Slider,
} from '@mui/material';
import { Save as SaveIcon, Refresh as RefreshIcon } from '@mui/icons-material';

interface SimpleConfig {
  width: number;
  height: number;
  seed: number;
  algorithm: string;
  terrainDensity: number;
}

interface SimpleConfigurationPanelProps {
  config: SimpleConfig;
  onChange: (config: SimpleConfig) => void;
  onGenerate: () => void;
  isGenerating?: boolean;
}

export const SimpleConfigurationPanel: React.FC<SimpleConfigurationPanelProps> = ({
  config,
  onChange,
  onGenerate,
  isGenerating = false
}) => {
  const handleChange = (field: keyof SimpleConfig, value: any) => {
    onChange({
      ...config,
      [field]: value
    });
  };

  const handleRandomSeed = () => {
    handleChange('seed', Math.floor(Math.random() * 1000000));
  };

  return (
    <Paper sx={{ p: 3, height: '100%' }}>
      <Typography variant="h6" gutterBottom>
        Level Configuration
      </Typography>
      
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
        <Grid container spacing={2}>
          <Grid item xs={6}>
            <TextField
              label="Width"
              type="number"
              value={config.width}
              onChange={(e) => handleChange('width', parseInt(e.target.value) || 50)}
              fullWidth
              inputProps={{ min: 10, max: 200 }}
            />
          </Grid>
          <Grid item xs={6}>
            <TextField
              label="Height"
              type="number"
              value={config.height}
              onChange={(e) => handleChange('height', parseInt(e.target.value) || 50)}
              fullWidth
              inputProps={{ min: 10, max: 200 }}
            />
          </Grid>
        </Grid>

        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <TextField
              label="Seed"
              type="number"
              value={config.seed}
              onChange={(e) => handleChange('seed', parseInt(e.target.value) || 0)}
              sx={{ flexGrow: 1 }}
            />
            <Button
              variant="outlined"
              onClick={handleRandomSeed}
              disabled={isGenerating}
            >
              Random
            </Button>
          </Box>
        </Box>

        <FormControl fullWidth>
          <InputLabel>Generation Algorithm</InputLabel>
          <Select
            value={config.algorithm}
            label="Generation Algorithm"
            onChange={(e) => handleChange('algorithm', e.target.value)}
          >
            <MenuItem value="perlin-noise">Perlin Noise</MenuItem>
            <MenuItem value="cellular-automata">Cellular Automata</MenuItem>
            <MenuItem value="maze">Maze Generator</MenuItem>
            <MenuItem value="random">Random</MenuItem>
          </Select>
        </FormControl>

        <Box>
          <Typography gutterBottom>
            Terrain Density: {config.terrainDensity}%
          </Typography>
          <Slider
            value={config.terrainDensity}
            onChange={(_, value) => handleChange('terrainDensity', value as number)}
            min={10}
            max={90}
            step={5}
            marks
            valueLabelDisplay="auto"
            disabled={isGenerating}
          />
        </Box>

        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
          <Button
            variant="contained"
            startIcon={<RefreshIcon />}
            onClick={onGenerate}
            disabled={isGenerating}
            fullWidth
          >
            {isGenerating ? 'Generating...' : 'Generate Level'}
          </Button>
          <Button
            variant="outlined"
            startIcon={<SaveIcon />}
            disabled={isGenerating}
          >
            Save
          </Button>
        </Box>
      </Box>
    </Paper>
  );
};