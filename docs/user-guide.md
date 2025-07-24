# Web Level Editor - User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Level Generation Workflow](#level-generation-workflow)
3. [Parameter Configuration](#parameter-configuration)
4. [Level Preview and Editing](#level-preview-and-editing)
5. [Sharing and Collaboration](#sharing-and-collaboration)
6. [Export and Download](#export-and-download)
7. [Batch Generation](#batch-generation)
8. [Tips and Best Practices](#tips-and-best-practices)

## Getting Started

### Accessing the Web Level Editor

1. **Open your web browser** and navigate to the Web Level Editor URL
2. **Wait for the application to load** (typically 2-3 seconds)
3. **Familiarize yourself with the interface**:
   - Left panel: Parameter configuration
   - Center: Level preview canvas
   - Right panel: Export and sharing options
   - Top bar: Navigation and project management

### System Requirements

- **Modern web browser** (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)
- **Internet connection** for initial loading and API calls
- **Minimum screen resolution**: 1024x768 (responsive design adapts to smaller screens)
- **JavaScript enabled** (required for full functionality)

## Level Generation Workflow

### Basic Generation Process

1. **Configure Parameters**
   - Open the parameter configuration panel on the left
   - Adjust terrain, entity, visual, and gameplay settings
   - See real-time validation feedback as you type

2. **Preview Generation**
   - Changes automatically trigger preview updates
   - Wait for the preview to render (usually under 500ms)
   - Use zoom and pan controls to explore large levels

3. **Refine and Iterate**
   - Make parameter adjustments based on preview
   - Use presets for quick configuration changes
   - Save custom configurations for future use

4. **Export or Share**
   - Choose your desired export format
   - Download the generated level
   - Share configuration with team members

### Understanding the Preview

The level preview shows:
- **Terrain tiles**: Different colors/textures represent terrain types
- **Entities**: Icons or sprites showing placed objects
- **Spawn points**: Special markers for player/enemy spawns
- **Grid overlay**: Optional coordinate system (toggle in view options)

## Parameter Configuration

### Terrain Generation

**Size Settings**
- **Width/Height**: Level dimensions (recommended: 20-100 tiles)
- **Seed**: Random seed for reproducible generation (leave blank for random)

**Terrain Types**
- **Grass**: Open areas for movement
- **Water**: Obstacles or special zones
- **Rock**: Impassable terrain
- **Sand**: Alternative terrain type

**Generation Algorithm**
- **Perlin Noise**: Smooth, natural-looking terrain
- **Cellular Automata**: Cave-like or organic patterns
- **Maze**: Structured pathways and walls

### Entity Configuration

**Entity Types**
- **Collectibles**: Items for players to gather
- **Enemies**: Hostile entities
- **NPCs**: Non-player characters
- **Interactive Objects**: Doors, switches, etc.

**Placement Rules**
- **Density**: How many entities to place (0.1-1.0)
- **Minimum Distance**: Space between similar entities
- **Terrain Restrictions**: Which terrain types can host entities

### Visual Themes

**Color Schemes**
- **Forest**: Green and brown tones
- **Desert**: Sandy and warm colors
- **Ocean**: Blue and aqua themes
- **Volcanic**: Red and orange palette
- **Custom**: Define your own color scheme

**Rendering Style**
- **Pixel Art**: Retro, blocky appearance
- **Smooth**: Anti-aliased, modern look
- **Minimalist**: Simple shapes and colors

### Gameplay Parameters

**Difficulty Settings**
- **Easy**: Fewer obstacles, more resources
- **Medium**: Balanced challenge
- **Hard**: Dense obstacles, scarce resources

**Game Mode**
- **Exploration**: Open-ended discovery
- **Puzzle**: Logic-based challenges
- **Action**: Fast-paced gameplay focus

## Level Preview and Editing

### Navigation Controls

**Mouse Controls**
- **Click and drag**: Pan around the level
- **Mouse wheel**: Zoom in/out
- **Click on tile**: Select for editing (if editing mode enabled)

**Touch Controls** (Mobile/Tablet)
- **Pinch**: Zoom in/out
- **Two-finger drag**: Pan around level
- **Tap**: Select tile or entity

### Manual Editing Features

**Terrain Editing**
1. **Enable edit mode** using the toggle in the toolbar
2. **Click on any terrain tile** to open the terrain selector
3. **Choose new terrain type** from the context menu
4. **Changes apply immediately** and are reflected in the preview

**Entity Management**
1. **Drag entities** to reposition them
2. **Right-click entities** to delete or modify properties
3. **Add new entities** by selecting from the entity palette
4. **Snap-to-grid** ensures proper alignment

**Undo/Redo**
- **Ctrl+Z** (Cmd+Z on Mac): Undo last change
- **Ctrl+Y** (Cmd+Y on Mac): Redo last undone change
- **Reset button**: Return to original generated state

### Validation and Warnings

The system automatically validates your edits:
- **Red highlights**: Invalid placements (e.g., entity on water)
- **Yellow warnings**: Suboptimal placements
- **Green confirmations**: Valid changes
- **Tooltip messages**: Detailed explanations of issues

## Sharing and Collaboration

### Creating Shareable Links

1. **Generate your level** with desired parameters
2. **Click the "Share" button** in the top toolbar
3. **Copy the generated URL** - this contains all configuration data
4. **Share the link** with team members via email, chat, etc.

### Loading Shared Configurations

1. **Click on a shared link** or paste URL in browser
2. **Wait for configuration to load** (usually instant)
3. **Review the loaded parameters** in the configuration panel
4. **Generate preview** to see the shared level
5. **Make modifications** if needed (doesn't affect original share)

### Export Options for Sharing

**Image Export**
- **PNG preview**: High-quality image of the level
- **Thumbnail grid**: Multiple variations in one image
- **Annotated version**: Includes parameter information

**Configuration Files**
- **JSON format**: Complete parameter set
- **Preset file**: Importable configuration
- **QR code**: Mobile-friendly sharing method

## Export and Download

### Available Export Formats

**Game Engine Formats**
- **Unity**: Compatible with Unity's coordinate system and prefab structure
- **Unreal**: Blueprint-compatible data format
- **Godot**: Scene and resource files
- **Generic JSON**: Universal format for custom engines

**Data Formats**
- **CSV**: Spreadsheet-compatible for analysis
- **XML**: Structured data format
- **Binary**: Compact format for large levels

### Export Process

1. **Ensure your level is generated** and preview looks correct
2. **Click "Export" button** in the right panel
3. **Select desired format** from the dropdown menu
4. **Configure export options** (coordinate system, compression, etc.)
5. **Click "Download"** to save the file
6. **Wait for processing** (progress bar shows status for large levels)

### Export Options

**Unity Export Settings**
- **Coordinate system**: Unity's Y-up vs Z-up
- **Scale factor**: Adjust size for your game
- **Prefab structure**: Nested vs flat hierarchy
- **Material assignments**: Automatic or manual

**Generic Export Settings**
- **Compression**: Reduce file size
- **Precision**: Decimal places for coordinates
- **Include metadata**: Generation parameters and timestamps
- **Format version**: Compatibility with different tools

## Batch Generation

### Setting Up Batch Generation

1. **Configure base parameters** for your level type
2. **Click "Batch Generate"** in the generation panel
3. **Specify number of variations** (1-50 recommended)
4. **Choose variation parameters**:
   - Seed variations (most common)
   - Size variations
   - Density variations
   - Theme variations

### Managing Batch Results

**Thumbnail View**
- **Grid layout** shows all generated levels
- **Click thumbnails** to view full-size preview
- **Hover for quick info** (seed, size, generation time)

**Selection and Export**
- **Check boxes** to select multiple levels
- **Bulk export** selected levels
- **Compare side-by-side** using the comparison tool
- **Save favorites** to your preset collection

### Batch Export

1. **Select levels** you want to export from the batch
2. **Choose export format** (same options as single export)
3. **Configure batch settings**:
   - File naming convention
   - Folder structure
   - ZIP packaging option
4. **Start batch export** and monitor progress
5. **Download ZIP file** containing all selected levels

## Tips and Best Practices

### Performance Optimization

**For Large Levels (100x100+)**
- Use **progressive loading** - start with smaller previews
- Enable **background generation** for batch operations
- Consider **tiled generation** for extremely large levels
- Use **caching** - identical parameters generate identical results

**For Real-time Editing**
- **Debounce parameter changes** - wait 500ms before regenerating
- **Use presets** for quick configuration switching
- **Limit entity density** for smoother preview updates

### Configuration Best Practices

**Starting Points**
- **Begin with presets** that match your game genre
- **Make incremental changes** rather than dramatic adjustments
- **Test with small levels first** before scaling up
- **Save working configurations** as custom presets

**Parameter Relationships**
- **Entity density** should scale with level size
- **Terrain complexity** affects generation time
- **Visual themes** don't impact gameplay mechanics
- **Seed values** ensure reproducible results

### Collaboration Workflows

**Team Coordination**
- **Use descriptive preset names** (e.g., "Forest_Level_v2_John")
- **Share configuration files** rather than just images
- **Document parameter reasoning** in preset descriptions
- **Version control** important configurations

**Review Process**
- **Export preview images** for quick team review
- **Use batch generation** to show multiple options
- **Include generation parameters** in review materials
- **Test exported levels** in target game engine

### Troubleshooting Common Issues

**Generation Problems**
- **Slow generation**: Reduce level size or entity density
- **Empty levels**: Check terrain generation parameters
- **Too many entities**: Adjust density or placement rules
- **Inconsistent results**: Verify seed settings

**Preview Issues**
- **Blurry preview**: Check zoom level and browser settings
- **Missing elements**: Refresh page or clear browser cache
- **Slow updates**: Reduce preview quality in settings
- **Touch problems**: Ensure touch events are enabled

**Export Problems**
- **Large file sizes**: Enable compression or reduce precision
- **Format compatibility**: Verify target engine requirements
- **Download failures**: Check browser download settings
- **Corrupted files**: Try different export format

### Advanced Features

**Custom Scripting** (Advanced Users)
- **Parameter formulas**: Link parameters with mathematical relationships
- **Conditional generation**: Different rules based on level properties
- **Custom validation**: Define your own placement rules

**Integration Options**
- **API access**: Programmatic level generation
- **Webhook notifications**: Automated workflow integration
- **Batch processing**: Command-line tools for bulk operations

---

## Need More Help?

- **FAQ Section**: Check common questions and answers
- **Video Tutorials**: Step-by-step visual guides
- **API Documentation**: For developers and advanced users
- **Community Forum**: Connect with other users
- **Support Contact**: Direct help for technical issues

Remember: The Web Level Editor is designed to be intuitive, but don't hesitate to experiment! Most changes can be undone, and the preview updates in real-time to show you exactly what your parameters will create.