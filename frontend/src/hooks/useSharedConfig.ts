import { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { GenerationConfig, ShareResult } from '../types';
import { apiService } from '../services/api';

interface UseSharedConfigReturn {
  isLoadingShared: boolean;
  sharedConfig: GenerationConfig | null;
  shareError: string | null;
  createShareLink: (config: GenerationConfig) => Promise<ShareResult | null>;
  clearSharedConfig: () => void;
  importFromUrl: (shareId: string) => Promise<GenerationConfig | null>;
}

export const useSharedConfig = (): UseSharedConfigReturn => {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  
  const [isLoadingShared, setIsLoadingShared] = useState(false);
  const [sharedConfig, setSharedConfig] = useState<GenerationConfig | null>(null);
  const [shareError, setShareError] = useState<string | null>(null);

  // Check for shared configuration on mount
  useEffect(() => {
    const shareId = searchParams.get('share');
    if (shareId) {
      loadSharedConfiguration(shareId);
    }
  }, [searchParams]);

  const loadSharedConfiguration = async (shareId: string) => {
    setIsLoadingShared(true);
    setShareError(null);
    
    try {
      const config = await apiService.getSharedConfiguration(shareId);
      setSharedConfig(config);
      
      // Remove share parameter from URL after successful load
      const newSearchParams = new URLSearchParams(searchParams);
      newSearchParams.delete('share');
      setSearchParams(newSearchParams, { replace: true });
      
      return config;
    } catch (error: any) {
      const errorMessage = error.response?.data?.error || 'Failed to load shared configuration';
      setShareError(errorMessage);
      
      // Remove invalid share parameter
      const newSearchParams = new URLSearchParams(searchParams);
      newSearchParams.delete('share');
      setSearchParams(newSearchParams, { replace: true });
      
      return null;
    } finally {
      setIsLoadingShared(false);
    }
  };

  const createShareLink = async (config: GenerationConfig): Promise<ShareResult | null> => {
    try {
      setShareError(null);
      const result = await apiService.createShareLink(config);
      return result;
    } catch (error: any) {
      const errorMessage = error.response?.data?.error || 'Failed to create share link';
      setShareError(errorMessage);
      return null;
    }
  };

  const importFromUrl = async (shareId: string): Promise<GenerationConfig | null> => {
    return await loadSharedConfiguration(shareId);
  };

  const clearSharedConfig = () => {
    setSharedConfig(null);
    setShareError(null);
  };

  return {
    isLoadingShared,
    sharedConfig,
    shareError,
    createShareLink,
    clearSharedConfig,
    importFromUrl
  };
};

export default useSharedConfig;