# Web Level Editor - API Documentation

## Table of Contents
1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Rate Limiting](#rate-limiting)
4. [Generation API](#generation-api)
5. [Configuration API](#configuration-api)
6. [Export API](#export-api)
7. [Job Management API](#job-management-api)
8. [Error Handling](#error-handling)
9. [Data Models](#data-models)
10. [Code Examples](#code-examples)

## Overview

The Web Level Editor provides a comprehensive REST API for programmatic access to level generation functionality. The API is built on ASP.NET Core and follows RESTful conventions with JSON request/response format.

### Base URL
```
Production: https://api.leveleditor.com/api
Development: http://localhost:5000/api
```

### API Version
Current version: `v1`
All endpoints are prefixed with `/api/v1/`

### Content Type
All requests and responses use `application/json` content type unless otherwise specified.

## Authentication

### API Key Authentication
```http
Authorization: Bearer YOUR_API_KEY
```

### Session-based Authentication (Web UI)
```http
Cookie: SessionId=your-session-id
```

### Rate Limiting Headers
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

## Rate Limiting

| Endpoint Category | Requests per Minute | Requests per Hour |
|------------------|-------------------|------------------|
| Generation | 10 | 100 |
| Configuration | 30 | 500 |
| Export | 5 | 50 |
| Job Status | 60 | 1000 |

Rate limits are per API key or session. Exceeding limits returns HTTP 429 with retry-after header.

## Generation API

### Generate Single Level

**Endpoint:** `POST /api/v1/generation/generate`

**Description:** Generate a single level with specified parameters.

**Request Body:**
```json
{
  "config": {
    "terrain": {
      "width": 50,
      "height": 50,
      "seed": 12345,
      "algorithm": "PerlinNoise",
      "terrainTypes": ["Grass", "Water", "Rock"],
      "density": 0.7
    },
    "entities": {
      "types": ["Collectible", "Enemy", "NPC"],
      "density": 0.3,
      "placementRules": {
        "minDistance": 2,
        "terrainRestrictions": {
          "Enemy": ["Grass", "Rock"],
          "Collectible": ["Grass"]
        }
      }
    },
    "visual": {
      "theme": "Forest",
      "style": "PixelArt"
    },
    "gameplay": {
      "difficulty": "Medium",
      "mode": "Exploration"
    }
  },
  "includePreview": true,
  "sessionId": "optional-session-id"
}
```

**Response (200 OK):**
```json
{
  "level": {
    "id": "level-uuid",
    "terrain": {
      "width": 50,
      "height": 50,
      "tiles": [
        [{"type": "Grass", "x": 0, "y": 0}, ...]
      ]
    },
    "entities": [
      {
        "id": "entity-uuid",
        "type": "Collectible",
        "position": {"x": 10, "y": 15},
        "properties": {"value": 100}
      }
    ],
    "metadata": {
      "generationTime": "00:00:01.234",
      "seed": 12345,
      "configHash": "abc123"
    }
  },
  "preview": {
    "imageUrl": "/api/v1/previews/level-uuid.png",
    "thumbnailUrl": "/api/v1/previews/level-uuid-thumb.png"
  }
}
```

### Validate Configuration

**Endpoint:** `POST /api/v1/generation/validate-config`

**Description:** Validate generation parameters without generating a level.

**Request Body:**
```json
{
  "config": {
    // Same structure as generate endpoint
  }
}
```

**Response (200 OK):**
```json
{
  "isValid": true,
  "errors": [],
  "warnings": [
    {
      "field": "entities.density",
      "message": "High entity density may impact performance",
      "severity": "Warning"
    }
  ],
  "estimatedGenerationTime": "00:00:02.500"
}
```

### Batch Generation

**Endpoint:** `POST /api/v1/generation/generate-batch`

**Description:** Generate multiple level variations asynchronously.

**Request Body:**
```json
{
  "baseConfig": {
    // Base configuration object
  },
  "variations": [
    {
      "parameter": "terrain.seed",
      "values": [1001, 1002, 1003, 1004, 1005]
    },
    {
      "parameter": "terrain.width",
      "values": [30, 40, 50]
    }
  ],
  "count": 15,
  "exportFormat": "JSON"
}
```

**Response (202 Accepted):**
```json
{
  "jobId": "batch-job-uuid",
  "status": "Queued",
  "estimatedCompletion": "2024-01-15T10:30:00Z",
  "statusUrl": "/api/v1/jobs/batch-job-uuid/status"
}
```

## Configuration API

### Get Presets

**Endpoint:** `GET /api/v1/configuration/presets`

**Query Parameters:**
- `category` (optional): Filter by category (terrain, entities, visual, gameplay)
- `search` (optional): Search preset names and descriptions
- `limit` (optional): Maximum results (default: 50)

**Response (200 OK):**
```json
{
  "presets": [
    {
      "id": "preset-uuid",
      "name": "Forest Adventure",
      "description": "Dense forest with collectibles and enemies",
      "category": "Complete",
      "config": {
        // Full configuration object
      },
      "createdAt": "2024-01-15T09:00:00Z",
      "isPublic": true,
      "author": "System"
    }
  ],
  "totalCount": 25,
  "hasMore": false
}
```

### Save Preset

**Endpoint:** `POST /api/v1/configuration/presets`

**Request Body:**
```json
{
  "name": "My Custom Preset",
  "description": "Custom configuration for puzzle levels",
  "category": "Terrain",
  "config": {
    // Configuration object
  },
  "isPublic": false
}
```

**Response (201 Created):**
```json
{
  "id": "new-preset-uuid",
  "name": "My Custom Preset",
  "shareUrl": "/api/v1/configuration/share/abc123"
}
```

### Share Configuration

**Endpoint:** `POST /api/v1/configuration/share`

**Request Body:**
```json
{
  "config": {
    // Configuration to share
  },
  "expiresInDays": 30,
  "allowModification": true
}
```

**Response (201 Created):**
```json
{
  "shareId": "share-uuid",
  "shareUrl": "https://leveleditor.com/shared/share-uuid",
  "shortUrl": "https://lvl.ed/s/abc123",
  "qrCodeUrl": "/api/v1/configuration/share/share-uuid/qr",
  "expiresAt": "2024-02-15T09:00:00Z"
}
```

### Get Shared Configuration

**Endpoint:** `GET /api/v1/configuration/share/{shareId}`

**Response (200 OK):**
```json
{
  "config": {
    // Shared configuration object
  },
  "metadata": {
    "createdAt": "2024-01-15T09:00:00Z",
    "expiresAt": "2024-02-15T09:00:00Z",
    "viewCount": 15,
    "allowModification": true
  }
}
```

## Export API

### Export Level

**Endpoint:** `POST /api/v1/export/level`

**Request Body:**
```json
{
  "level": {
    // Level object from generation
  },
  "format": "Unity",
  "options": {
    "coordinateSystem": "Unity",
    "scaleFactor": 1.0,
    "includeMetadata": true,
    "compression": "None"
  }
}
```

**Response (200 OK):**
```json
{
  "downloadUrl": "/api/v1/downloads/export-uuid.zip",
  "fileName": "level_export_unity.zip",
  "fileSize": 1024000,
  "format": "Unity",
  "expiresAt": "2024-01-16T09:00:00Z"
}
```

### Get Available Formats

**Endpoint:** `GET /api/v1/export/formats`

**Response (200 OK):**
```json
{
  "formats": [
    {
      "id": "Unity",
      "name": "Unity Engine",
      "description": "Compatible with Unity 2020.3+",
      "fileExtension": "zip",
      "supportsCustomization": true,
      "options": [
        {
          "name": "coordinateSystem",
          "type": "select",
          "values": ["Unity", "Standard"],
          "default": "Unity"
        }
      ]
    },
    {
      "id": "JSON",
      "name": "Generic JSON",
      "description": "Universal format for custom engines",
      "fileExtension": "json",
      "supportsCustomization": false
    }
  ]
}
```

### Batch Export

**Endpoint:** `POST /api/v1/export/batch`

**Request Body:**
```json
{
  "levels": [
    {
      "id": "level-1-uuid",
      "fileName": "forest_level_01"
    },
    {
      "id": "level-2-uuid", 
      "fileName": "forest_level_02"
    }
  ],
  "format": "Unity",
  "options": {
    "packageAsZip": true,
    "includeManifest": true
  }
}
```

**Response (202 Accepted):**
```json
{
  "jobId": "export-job-uuid",
  "status": "Processing",
  "statusUrl": "/api/v1/jobs/export-job-uuid/status"
}
```

## Job Management API

### Get Job Status

**Endpoint:** `GET /api/v1/jobs/{jobId}/status`

**Response (200 OK):**
```json
{
  "jobId": "job-uuid",
  "status": "Running",
  "progress": 65,
  "startedAt": "2024-01-15T09:00:00Z",
  "estimatedCompletion": "2024-01-15T09:05:00Z",
  "result": null,
  "error": null,
  "logs": [
    {
      "timestamp": "2024-01-15T09:01:00Z",
      "level": "Info",
      "message": "Started processing batch generation"
    }
  ]
}
```

**Status Values:**
- `Queued`: Job is waiting to be processed
- `Running`: Job is currently being processed
- `Completed`: Job finished successfully
- `Failed`: Job encountered an error
- `Cancelled`: Job was cancelled by user

### Cancel Job

**Endpoint:** `DELETE /api/v1/jobs/{jobId}`

**Response (200 OK):**
```json
{
  "jobId": "job-uuid",
  "status": "Cancelled",
  "message": "Job cancelled successfully"
}
```

## Error Handling

### Error Response Format

All API errors follow this format:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": [
      {
        "field": "config.terrain.width",
        "message": "Width must be between 10 and 200",
        "value": 5
      }
    ],
    "requestId": "req-uuid",
    "timestamp": "2024-01-15T09:00:00Z"
  }
}
```

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Invalid request parameters |
| `GENERATION_FAILED` | 422 | Level generation failed |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `UNAUTHORIZED` | 401 | Invalid or missing authentication |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `NOT_FOUND` | 404 | Resource not found |
| `INTERNAL_ERROR` | 500 | Server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

## Data Models

### GenerationConfig

```typescript
interface GenerationConfig {
  terrain: TerrainConfig;
  entities: EntityConfig;
  visual: VisualConfig;
  gameplay: GameplayConfig;
}

interface TerrainConfig {
  width: number; // 10-200
  height: number; // 10-200
  seed?: number; // Optional, random if not provided
  algorithm: 'PerlinNoise' | 'CellularAutomata' | 'Maze';
  terrainTypes: string[];
  density: number; // 0.0-1.0
}

interface EntityConfig {
  types: string[];
  density: number; // 0.0-1.0
  placementRules: {
    minDistance: number;
    terrainRestrictions: Record<string, string[]>;
  };
}

interface VisualConfig {
  theme: 'Forest' | 'Desert' | 'Ocean' | 'Volcanic' | 'Custom';
  style: 'PixelArt' | 'Smooth' | 'Minimalist';
  customColors?: Record<string, string>;
}

interface GameplayConfig {
  difficulty: 'Easy' | 'Medium' | 'Hard';
  mode: 'Exploration' | 'Puzzle' | 'Action';
}
```

### Level

```typescript
interface Level {
  id: string;
  terrain: {
    width: number;
    height: number;
    tiles: Tile[][];
  };
  entities: Entity[];
  metadata: {
    generationTime: string;
    seed: number;
    configHash: string;
    createdAt: string;
  };
}

interface Tile {
  type: string;
  x: number;
  y: number;
  properties?: Record<string, any>;
}

interface Entity {
  id: string;
  type: string;
  position: { x: number; y: number };
  properties: Record<string, any>;
}
```

## Code Examples

### JavaScript/TypeScript

```typescript
// Initialize API client
class LevelEditorAPI {
  private baseUrl = 'https://api.leveleditor.com/api/v1';
  private apiKey: string;

  constructor(apiKey: string) {
    this.apiKey = apiKey;
  }

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.apiKey}`,
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(`API Error: ${error.error.message}`);
    }

    return response.json();
  }

  async generateLevel(config: GenerationConfig): Promise<Level> {
    return this.request<{level: Level}>('/generation/generate', {
      method: 'POST',
      body: JSON.stringify({ config }),
    }).then(response => response.level);
  }

  async validateConfig(config: GenerationConfig): Promise<ValidationResult> {
    return this.request<ValidationResult>('/generation/validate-config', {
      method: 'POST',
      body: JSON.stringify({ config }),
    });
  }

  async getPresets(category?: string): Promise<ConfigPreset[]> {
    const params = category ? `?category=${category}` : '';
    return this.request<{presets: ConfigPreset[]}>(`/configuration/presets${params}`)
      .then(response => response.presets);
  }
}

// Usage example
const api = new LevelEditorAPI('your-api-key');

const config: GenerationConfig = {
  terrain: {
    width: 50,
    height: 50,
    algorithm: 'PerlinNoise',
    terrainTypes: ['Grass', 'Water', 'Rock'],
    density: 0.7,
  },
  entities: {
    types: ['Collectible', 'Enemy'],
    density: 0.3,
    placementRules: {
      minDistance: 2,
      terrainRestrictions: {
        'Enemy': ['Grass', 'Rock'],
        'Collectible': ['Grass'],
      },
    },
  },
  visual: {
    theme: 'Forest',
    style: 'PixelArt',
  },
  gameplay: {
    difficulty: 'Medium',
    mode: 'Exploration',
  },
};

try {
  const level = await api.generateLevel(config);
  console.log('Generated level:', level);
} catch (error) {
  console.error('Generation failed:', error);
}
```

### Python

```python
import requests
import json
from typing import Dict, Any, List

class LevelEditorAPI:
    def __init__(self, api_key: str, base_url: str = "https://api.leveleditor.com/api/v1"):
        self.base_url = base_url
        self.headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {api_key}"
        }
    
    def _request(self, method: str, endpoint: str, data: Dict[str, Any] = None) -> Dict[str, Any]:
        url = f"{self.base_url}{endpoint}"
        response = requests.request(
            method=method,
            url=url,
            headers=self.headers,
            json=data
        )
        
        if not response.ok:
            error_data = response.json()
            raise Exception(f"API Error: {error_data['error']['message']}")
        
        return response.json()
    
    def generate_level(self, config: Dict[str, Any]) -> Dict[str, Any]:
        response = self._request("POST", "/generation/generate", {"config": config})
        return response["level"]
    
    def validate_config(self, config: Dict[str, Any]) -> Dict[str, Any]:
        return self._request("POST", "/generation/validate-config", {"config": config})
    
    def get_presets(self, category: str = None) -> List[Dict[str, Any]]:
        endpoint = "/configuration/presets"
        if category:
            endpoint += f"?category={category}"
        response = self._request("GET", endpoint)
        return response["presets"]

# Usage example
api = LevelEditorAPI("your-api-key")

config = {
    "terrain": {
        "width": 50,
        "height": 50,
        "algorithm": "PerlinNoise",
        "terrainTypes": ["Grass", "Water", "Rock"],
        "density": 0.7
    },
    "entities": {
        "types": ["Collectible", "Enemy"],
        "density": 0.3,
        "placementRules": {
            "minDistance": 2,
            "terrainRestrictions": {
                "Enemy": ["Grass", "Rock"],
                "Collectible": ["Grass"]
            }
        }
    },
    "visual": {
        "theme": "Forest",
        "style": "PixelArt"
    },
    "gameplay": {
        "difficulty": "Medium",
        "mode": "Exploration"
    }
}

try:
    level = api.generate_level(config)
    print(f"Generated level with {len(level['entities'])} entities")
except Exception as e:
    print(f"Generation failed: {e}")
```

### C#

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class LevelEditorAPI
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public LevelEditorAPI(string apiKey, string baseUrl = "https://api.leveleditor.com/api/v1")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    private async Task<T> RequestAsync<T>(string method, string endpoint, object data = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), $"{_baseUrl}{endpoint}");
        
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<ApiError>(responseContent);
            throw new Exception($"API Error: {error.Error.Message}");
        }

        return JsonSerializer.Deserialize<T>(responseContent);
    }

    public async Task<Level> GenerateLevelAsync(GenerationConfig config)
    {
        var response = await RequestAsync<GenerationResponse>("POST", "/generation/generate", new { config });
        return response.Level;
    }

    public async Task<ValidationResult> ValidateConfigAsync(GenerationConfig config)
    {
        return await RequestAsync<ValidationResult>("POST", "/generation/validate-config", new { config });
    }
}

// Usage example
var api = new LevelEditorAPI("your-api-key");

var config = new GenerationConfig
{
    Terrain = new TerrainConfig
    {
        Width = 50,
        Height = 50,
        Algorithm = "PerlinNoise",
        TerrainTypes = new[] { "Grass", "Water", "Rock" },
        Density = 0.7f
    },
    // ... other configuration properties
};

try
{
    var level = await api.GenerateLevelAsync(config);
    Console.WriteLine($"Generated level with {level.Entities.Length} entities");
}
catch (Exception ex)
{
    Console.WriteLine($"Generation failed: {ex.Message}");
}
```

## Webhooks (Optional)

For advanced integrations, you can configure webhooks to receive notifications when long-running jobs complete:

**Webhook Configuration:**
```json
{
  "url": "https://your-app.com/webhooks/level-editor",
  "events": ["generation.completed", "export.completed", "job.failed"],
  "secret": "your-webhook-secret"
}
```

**Webhook Payload:**
```json
{
  "event": "generation.completed",
  "jobId": "job-uuid",
  "timestamp": "2024-01-15T09:05:00Z",
  "data": {
    "level": {
      // Generated level data
    }
  }
}
```

---

For additional support or questions about the API, please contact our developer support team or check the FAQ section.