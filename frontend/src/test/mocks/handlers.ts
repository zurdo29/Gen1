import { http, HttpResponse } from 'msw'
import { mockLevel, mockGenerationConfig, mockExportFormats, mockPresets } from './mockData'

export const handlers = [
  // Generation API endpoints
  http.post('/api/generation/generate', () => {
    return HttpResponse.json(mockLevel)
  }),

  http.post('/api/generation/validate-config', () => {
    return HttpResponse.json({ isValid: true, errors: [] })
  }),

  http.get('/api/generation/job/:jobId/status', ({ params }) => {
    return HttpResponse.json({
      jobId: params.jobId,
      status: 'completed',
      progress: 100,
      result: mockLevel
    })
  }),

  // Configuration API endpoints
  http.get('/api/configuration/presets', () => {
    return HttpResponse.json(mockPresets)
  }),

  http.post('/api/configuration/presets', () => {
    return HttpResponse.json({ id: 'new-preset-id', name: 'Test Preset' })
  }),

  http.get('/api/configuration/share/:shareId', () => {
    return HttpResponse.json(mockGenerationConfig)
  }),

  http.post('/api/configuration/share', () => {
    return HttpResponse.json({
      shareId: 'test-share-id',
      shareUrl: 'https://example.com/share/test-share-id',
      expiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString()
    })
  }),

  // Export API endpoints
  http.get('/api/export/formats', () => {
    return HttpResponse.json(mockExportFormats)
  }),

  http.post('/api/export/level', () => {
    return new HttpResponse('mock-exported-data', {
      headers: {
        'Content-Type': 'application/json',
        'Content-Disposition': 'attachment; filename="level.json"'
      }
    })
  }),

  // Error scenarios for testing
  http.post('/api/generation/generate-error', () => {
    return HttpResponse.json(
      { error: 'Generation failed', details: 'Invalid configuration' },
      { status: 400 }
    )
  }),

  http.post('/api/generation/generate-timeout', () => {
    return HttpResponse.json(
      { jobId: 'timeout-job', status: 'pending' },
      { status: 202 }
    )
  })
]