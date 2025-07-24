import { describe, it, expect, vi, beforeEach } from 'vitest'
import { server } from '../test/mocks/server'
import { http, HttpResponse } from 'msw'
import { 
  generateLevel, 
  validateConfiguration, 
  getPresets, 
  savePreset, 
  exportLevel, 
  getExportFormats,
  shareConfiguration,
  getSharedConfiguration
} from './api'
import { mockGenerationConfig, mockLevel, mockPresets, mockExportFormats } from '../test/mocks/mockData'

describe('API Service', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('generateLevel', () => {
    it('should generate level successfully', async () => {
      const result = await generateLevel(mockGenerationConfig)
      
      expect(result).toEqual(mockLevel)
    })

    it('should handle generation errors', async () => {
      server.use(
        http.post('/api/generation/generate', () => {
          return HttpResponse.json(
            { error: 'Generation failed' },
            { status: 400 }
          )
        })
      )

      await expect(generateLevel(mockGenerationConfig)).rejects.toThrow()
    })

    it('should handle network errors', async () => {
      server.use(
        http.post('/api/generation/generate', () => {
          return HttpResponse.error()
        })
      )

      await expect(generateLevel(mockGenerationConfig)).rejects.toThrow()
    })
  })

  describe('validateConfiguration', () => {
    it('should validate configuration successfully', async () => {
      const result = await validateConfiguration(mockGenerationConfig)
      
      expect(result).toEqual({ isValid: true, errors: [] })
    })

    it('should return validation errors for invalid config', async () => {
      const validationErrors = [
        { field: 'terrain.width', message: 'Width is required', code: 'REQUIRED' }
      ]

      server.use(
        http.post('/api/generation/validate-config', () => {
          return HttpResponse.json({ isValid: false, errors: validationErrors })
        })
      )

      const result = await validateConfiguration(mockGenerationConfig)
      
      expect(result.isValid).toBe(false)
      expect(result.errors).toEqual(validationErrors)
    })
  })

  describe('getPresets', () => {
    it('should fetch presets successfully', async () => {
      const result = await getPresets()
      
      expect(result).toEqual(mockPresets)
    })

    it('should handle empty presets list', async () => {
      server.use(
        http.get('/api/configuration/presets', () => {
          return HttpResponse.json([])
        })
      )

      const result = await getPresets()
      
      expect(result).toEqual([])
    })
  })

  describe('savePreset', () => {
    it('should save preset successfully', async () => {
      const preset = {
        name: 'Test Preset',
        description: 'Test description',
        config: mockGenerationConfig
      }

      const result = await savePreset(preset)
      
      expect(result).toEqual({ id: 'new-preset-id', name: 'Test Preset' })
    })
  })

  describe('exportLevel', () => {
    it('should export level successfully', async () => {
      const exportRequest = {
        level: mockLevel,
        format: 'json',
        options: {}
      }

      const result = await exportLevel(exportRequest)
      
      expect(result).toBeDefined()
    })

    it('should handle export errors', async () => {
      server.use(
        http.post('/api/export/level', () => {
          return HttpResponse.json(
            { error: 'Export failed' },
            { status: 500 }
          )
        })
      )

      const exportRequest = {
        level: mockLevel,
        format: 'json',
        options: {}
      }

      await expect(exportLevel(exportRequest)).rejects.toThrow()
    })
  })

  describe('getExportFormats', () => {
    it('should fetch export formats successfully', async () => {
      const result = await getExportFormats()
      
      expect(result).toEqual(mockExportFormats)
    })
  })

  describe('shareConfiguration', () => {
    it('should create share link successfully', async () => {
      const result = await shareConfiguration(mockGenerationConfig)
      
      expect(result).toEqual({
        shareId: 'test-share-id',
        shareUrl: 'https://example.com/share/test-share-id',
        expiresAt: expect.any(String)
      })
    })
  })

  describe('getSharedConfiguration', () => {
    it('should retrieve shared configuration successfully', async () => {
      const result = await getSharedConfiguration('test-share-id')
      
      expect(result).toEqual(mockGenerationConfig)
    })

    it('should handle invalid share ID', async () => {
      server.use(
        http.get('/api/configuration/share/:shareId', () => {
          return HttpResponse.json(
            { error: 'Share not found' },
            { status: 404 }
          )
        })
      )

      await expect(getSharedConfiguration('invalid-id')).rejects.toThrow()
    })
  })

  describe('API retry logic', () => {
    it('should retry failed requests', async () => {
      let callCount = 0
      
      server.use(
        http.post('/api/generation/generate', () => {
          callCount++
          if (callCount < 3) {
            return HttpResponse.error()
          }
          return HttpResponse.json(mockLevel)
        })
      )

      const result = await generateLevel(mockGenerationConfig)
      
      expect(result).toEqual(mockLevel)
      expect(callCount).toBe(3)
    })
  })

  describe('API timeout handling', () => {
    it('should handle request timeouts', async () => {
      server.use(
        http.post('/api/generation/generate', async () => {
          await new Promise(resolve => setTimeout(resolve, 10000))
          return HttpResponse.json(mockLevel)
        })
      )

      await expect(generateLevel(mockGenerationConfig)).rejects.toThrow('timeout')
    })
  })
})