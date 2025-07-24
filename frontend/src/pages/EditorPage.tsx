import React, { useState } from 'react';
import {
  Typography,
  Box,
  Grid,
  ToggleButton,
  ToggleButtonGroup,
  Paper,
} from '@mui/material';
import { 
  Settings as SettingsIcon,
  Tune as TuneIcon 
} from '@mui/icons-material';
import { SimpleConfigurationPanel } from '../components/configuration/SimpleConfigurationPanel';
import { AdvancedConfigurationPanel } from '../components/configuration/AdvancedConfigurationPanel';
import { SimpleLevelPreview } from '../components/preview/SimpleLevelPreview';
import { InteractiveLevelPreview } from '../components/preview/InteractiveLevelPreview';
import { useNotifications } from '../hooks/useNotifications';

interface SimpleConfig {
  width: number;
  height: number;
  seed: number;
  algorithm: string;
  terrainDensity: number;
}

interface EntityConfig {
  type: string;
  count: number;
  enabled: boolean;
}

interface AdvancedConfig {
  width: number;
  height: number;
  seed: number;
  algorithm: string;
  terrainDensity: number;
  terrainTypes: string[];
  entities: EntityConfig[];
  smoothingPasses: number;
  borderWalls: boolean;
  ensureConnectivity: boolean;
  theme: string;
}

interface SimpleLevel {
  width: number;
  height: number;
  tiles: string[][];
  entities: Array<{
    type: string;
    x: number;
    y: number;
  }>;
}

interface EditorPageProps {
  onShowLoading?: (message: string, progress?: number) => void;
  onHideLoading?: () => void;
  onUpdateProgress?: (progress: number) => void;
}

export const EditorPage: React.FC<EditorPageProps> = ({
  onShowLoading,
  onHideLoading,
  onUpdateProgress
}) => {
  const { showSuccess, showError, showInfo } = useNotifications();
  
  const [configMode, setConfigMode] = useState<'simple' | 'advanced'>('simple');
  const [previewMode, setPreviewMode] = useState<'simple' | 'interactive'>('interactive');
  
  const [simpleConfig, setSimpleConfig] = useState<SimpleConfig>({
    width: 50,
    height: 50,
    seed: 12345,
    algorithm: 'perlin-noise',
    terrainDensity: 40
  });

  const [advancedConfig, setAdvancedConfig] = useState<AdvancedConfig>({
    width: 50,
    height: 50,
    seed: 12345,
    algorithm: 'perlin-noise',
    terrainDensity: 40,
    terrainTypes: ['floor', 'wall', 'water'],
    entities: [
      { type: 'player', count: 1, enabled: true },
      { type: 'enemy', count: 5, enabled: true },
      { type: 'item', count: 8, enabled: true },
      { type: 'exit', count: 1, enabled: true },
      { type: 'checkpoint', count: 2, enabled: true },
    ],
    smoothingPasses: 1,
    borderWalls: true,
    ensureConnectivity: true,
    theme: 'default'
  });
  
  const [level, setLevel] = useState<SimpleLevel | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);

  // Get current config based on mode
  const currentConfig = configMode === 'simple' ? simpleConfig : advancedConfig;

  const generateSimpleLevel = async (config: SimpleConfig): Promise<SimpleLevel> => {
    // Simulate level generation
    await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 2000));
    
    const tiles: string[][] = [];
    const entities: Array<{ type: string; x: number; y: number }> = [];
    
    // Generate tiles based on algorithm
    for (let y = 0; y < config.height; y++) {
      tiles[y] = [];
      for (let x = 0; x < config.width; x++) {
        let tileType = 'floor';
        
        // Simple generation based on algorithm
        const noise = Math.sin(x * 0.1 + config.seed) * Math.cos(y * 0.1 + config.seed);
        const density = config.terrainDensity / 100;
        
        switch (config.algorithm) {
          case 'perlin-noise':
            if (noise > density) tileType = 'wall';
            else if (noise < -density) tileType = 'water';
            else tileType = 'floor';
            break;
          case 'cellular-automata':
            if (Math.random() < density) tileType = 'wall';
            else tileType = 'floor';
            break;
          case 'maze':
            if ((x % 2 === 0 || y % 2 === 0) && Math.random() < density) tileType = 'wall';
            else tileType = 'floor';
            break;
          case 'random':
            const rand = Math.random();
            if (rand < density * 0.6) tileType = 'wall';
            else if (rand < density * 0.8) tileType = 'grass';
            else if (rand < density * 0.9) tileType = 'water';
            else tileType = 'floor';
            break;
        }
        
        tiles[y][x] = tileType;
      }
    }
    
    // Add some entities
    const entityTypes = ['player', 'enemy', 'item', 'exit'];
    const entityCount = Math.min(10, Math.floor(config.width * config.height / 100));
    
    for (let i = 0; i < entityCount; i++) {
      let x, y;
      let attempts = 0;
      do {
        x = Math.floor(Math.random() * config.width);
        y = Math.floor(Math.random() * config.height);
        attempts++;
      } while (tiles[y][x] === 'wall' && attempts < 50);
      
      if (attempts < 50) {
        entities.push({
          type: entityTypes[i % entityTypes.length],
          x,
          y
        });
      }
    }
    
    return {
      width: config.width,
      height: config.height,
      tiles,
      entities
    };
  };

  const handleConfigChange = (newConfig: SimpleConfig | AdvancedConfig) => {
    if (configMode === 'simple') {
      setSimpleConfig(newConfig as SimpleConfig);
    } else {
      setAdvancedConfig(newConfig as AdvancedConfig);
    }
  };

  const handleTileClick = (x: number, y: number, tileType: string) => {
    showInfo('Tile Clicked', `Clicked ${tileType} tile at (${x}, ${y})`);
  };

  const handleEntityClick = (entity: any) => {
    showInfo('Entity Clicked', `Clicked ${entity.type} at (${entity.x}, ${entity.y})`);
  };

  const handleGenerate = async () => {
    setIsGenerating(true);
    onShowLoading?.('Generating level...', 0);
    
    try {
      showInfo('Generation Started', `Generating ${currentConfig.width}×${currentConfig.height} level with ${currentConfig.algorithm}`);
      
      // Simulate progress updates
      const progressInterval = setInterval(() => {
        onUpdateProgress?.(Math.random() * 100);
      }, 200);
      
      const newLevel = await generateSimpleLevel(currentConfig);
      
      clearInterval(progressInterval);
      setLevel(newLevel);
      showSuccess('Level Generated!', `Successfully generated level with ${newLevel.entities.length} entities`);
      
    } catch (error) {
      showError('Generation Failed', 'Failed to generate level. Please try again.');
      console.error('Generation error:', error);
    } finally {
      setIsGenerating(false);
      onHideLoading?.();
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4" component="h1">
          Level Editor
        </Typography>
        
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Paper sx={{ p: 0.5 }}>
            <ToggleButtonGroup
              value={configMode}
              exclusive
              onChange={(_, value) => value && setConfigMode(value)}
              size="small"
            >
              <ToggleButton value="simple">
                <SettingsIcon sx={{ mr: 1 }} />
                Simple
              </ToggleButton>
              <ToggleButton value="advanced">
                <TuneIcon sx={{ mr: 1 }} />
                Advanced
              </ToggleButton>
            </ToggleButtonGroup>
          </Paper>
          
          <Paper sx={{ p: 0.5 }}>
            <ToggleButtonGroup
              value={previewMode}
              exclusive
              onChange={(_, value) => value && setPreviewMode(value)}
              size="small"
            >
              <ToggleButton value="simple">
                Simple Preview
              </ToggleButton>
              <ToggleButton value="interactive">
                Interactive
              </ToggleButton>
            </ToggleButtonGroup>
          </Paper>
        </Box>
      </Box>
      
      <Typography variant="body1" color="text.secondary" paragraph>
        Configure your level parameters and generate procedural game levels with {configMode} controls.
      </Typography>

      <Grid container spacing={3} sx={{ height: 'calc(100vh - 250px)' }}>
        <Grid item xs={12} md={4}>
          {configMode === 'simple' ? (
            <SimpleConfigurationPanel
              config={simpleConfig}
              onChange={handleConfigChange}
              onGenerate={handleGenerate}
              isGenerating={isGenerating}
            />
          ) : (
            <AdvancedConfigurationPanel
              config={advancedConfig}
              onChange={handleConfigChange}
              onGenerate={handleGenerate}
              isGenerating={isGenerating}
            />
          )}
        </Grid>

        <Grid item xs={12} md={8}>
          {previewMode === 'simple' ? (
            <SimpleLevelPreview
              level={level}
              isLoading={isGenerating}
            />
          ) : (
            <InteractiveLevelPreview
              level={level}
              isLoading={isGenerating}
              onTileClick={handleTileClick}
              onEntityClick={handleEntityClick}
            />
          )}
        </Grid>
      </Grid>
    </Box>
  );
};