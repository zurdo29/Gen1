import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { Box, Typography } from '@mui/material';

interface VirtualScrollGridProps<T> {
  items: T[];
  itemHeight: number;
  itemWidth: number;
  containerHeight: number;
  containerWidth: number;
  columns: number;
  renderItem: (item: T, index: number) => React.ReactNode;
  gap?: number;
  overscan?: number;
  onScroll?: (scrollTop: number) => void;
  className?: string;
}

interface VirtualizedRange {
  startIndex: number;
  endIndex: number;
  visibleStartIndex: number;
  visibleEndIndex: number;
}

export function VirtualScrollGrid<T>({
  items,
  itemHeight,
  itemWidth,
  containerHeight,
  containerWidth,
  columns,
  renderItem,
  gap = 8,
  overscan = 5,
  onScroll,
  className
}: VirtualScrollGridProps<T>) {
  const [scrollTop, setScrollTop] = useState(0);
  const scrollElementRef = useRef<HTMLDivElement>(null);
  
  // Calculate grid dimensions
  const rowHeight = itemHeight + gap;
  const totalRows = Math.ceil(items.length / columns);
  const totalHeight = totalRows * rowHeight - gap; // Remove gap from last row
  
  // Calculate visible range with overscan
  const visibleRange = useMemo((): VirtualizedRange => {
    const visibleStartRow = Math.floor(scrollTop / rowHeight);
    const visibleEndRow = Math.min(
      totalRows - 1,
      Math.ceil((scrollTop + containerHeight) / rowHeight)
    );
    
    const startRow = Math.max(0, visibleStartRow - overscan);
    const endRow = Math.min(totalRows - 1, visibleEndRow + overscan);
    
    return {
      startIndex: startRow * columns,
      endIndex: Math.min(items.length - 1, (endRow + 1) * columns - 1),
      visibleStartIndex: visibleStartRow * columns,
      visibleEndIndex: Math.min(items.length - 1, (visibleEndRow + 1) * columns - 1)
    };
  }, [scrollTop, containerHeight, rowHeight, totalRows, columns, items.length, overscan]);
  
  // Get visible items
  const visibleItems = useMemo(() => {
    const result: { item: T; index: number; row: number; col: number }[] = [];
    
    for (let i = visibleRange.startIndex; i <= visibleRange.endIndex; i++) {
      if (i >= items.length) break;
      
      const row = Math.floor(i / columns);
      const col = i % columns;
      
      result.push({
        item: items[i],
        index: i,
        row,
        col
      });
    }
    
    return result;
  }, [items, visibleRange, columns]);
  
  // Handle scroll events
  const handleScroll = useCallback((event: React.UIEvent<HTMLDivElement>) => {
    const newScrollTop = event.currentTarget.scrollTop;
    setScrollTop(newScrollTop);
    onScroll?.(newScrollTop);
  }, [onScroll]);
  
  // Scroll to specific item
  const _scrollToItem = useCallback((index: number, align: 'start' | 'center' | 'end' = 'start') => {
    if (!scrollElementRef.current) return;
    
    const row = Math.floor(index / columns);
    const itemTop = row * rowHeight;
    
    let scrollTo: number;
    
    switch (align) {
      case 'start':
        scrollTo = itemTop;
        break;
      case 'center':
        scrollTo = itemTop - (containerHeight - itemHeight) / 2;
        break;
      case 'end':
        scrollTo = itemTop - containerHeight + itemHeight;
        break;
    }
    
    scrollTo = Math.max(0, Math.min(scrollTo, totalHeight - containerHeight));
    
    scrollElementRef.current.scrollTo({
      top: scrollTo,
      behavior: 'smooth'
    });
  }, [columns, rowHeight, containerHeight, itemHeight, totalHeight]);
  
  // Scroll to top
  const _scrollToTop = useCallback(() => {
    scrollElementRef.current?.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);
  
  // Scroll to bottom
  const _scrollToBottom = useCallback(() => {
    scrollElementRef.current?.scrollTo({ top: totalHeight, behavior: 'smooth' });
  }, [totalHeight]);
  
  // Get current scroll percentage
  const scrollPercentage = useMemo(() => {
    if (totalHeight <= containerHeight) return 0;
    return (scrollTop / (totalHeight - containerHeight)) * 100;
  }, [scrollTop, totalHeight, containerHeight]);
  
  // Performance monitoring
  const renderCount = useRef(0);
  useEffect(() => {
    renderCount.current++;
  });
  
  return (
    <Box
      className={className}
      sx={{
        position: 'relative',
        height: containerHeight,
        width: containerWidth,
        overflow: 'hidden'
      }}
    >
      {/* Scrollable container */}
      <Box
        ref={scrollElementRef}
        onScroll={handleScroll}
        sx={{
          height: '100%',
          width: '100%',
          overflow: 'auto',
          scrollbarWidth: 'thin'
        }}
      >
        {/* Virtual spacer for total height */}
        <Box sx={{ height: totalHeight, position: 'relative' }}>
          {/* Visible items container */}
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              transform: `translateY(${Math.floor(visibleRange.startIndex / columns) * rowHeight}px)`
            }}
          >
            {visibleItems.map(({ item, index, row, col }) => (
              <Box
                key={index}
                sx={{
                  position: 'absolute',
                  left: col * (itemWidth + gap),
                  top: (row - Math.floor(visibleRange.startIndex / columns)) * rowHeight,
                  width: itemWidth,
                  height: itemHeight
                }}
              >
                {renderItem(item, index)}
              </Box>
            ))}
          </Box>
        </Box>
      </Box>
      
      {/* Debug info (development only) */}
      {process.env.NODE_ENV === 'development' && (
        <Box
          sx={{
            position: 'absolute',
            top: 8,
            left: 8,
            background: 'rgba(0, 0, 0, 0.7)',
            color: 'white',
            padding: '4px 8px',
            borderRadius: 1,
            fontSize: '10px',
            fontFamily: 'monospace',
            pointerEvents: 'none',
            zIndex: 1000
          }}
        >
          <Typography variant="caption" component="div">
            Items: {items.length} | Visible: {visibleItems.length}
          </Typography>
          <Typography variant="caption" component="div">
            Scroll: {Math.round(scrollPercentage)}% | Renders: {renderCount.current}
          </Typography>
          <Typography variant="caption" component="div">
            Range: {visibleRange.startIndex}-{visibleRange.endIndex}
          </Typography>
        </Box>
      )}
    </Box>
  );
}

// Hook for managing virtual scroll state
export const useVirtualScroll = <T,>(items: T[], itemsPerPage = 50) => {
  const [currentPage, setCurrentPage] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  
  const totalPages = Math.ceil(items.length / itemsPerPage);
  const currentItems = useMemo(() => {
    const start = currentPage * itemsPerPage;
    const end = start + itemsPerPage;
    return items.slice(start, end);
  }, [items, currentPage, itemsPerPage]);
  
  const loadNextPage = useCallback(async () => {
    if (currentPage >= totalPages - 1 || isLoading) return;
    
    setIsLoading(true);
    // Simulate async loading
    await new Promise(resolve => setTimeout(resolve, 100));
    setCurrentPage(prev => prev + 1);
    setIsLoading(false);
  }, [currentPage, totalPages, isLoading]);
  
  const loadPreviousPage = useCallback(async () => {
    if (currentPage <= 0 || isLoading) return;
    
    setIsLoading(true);
    await new Promise(resolve => setTimeout(resolve, 100));
    setCurrentPage(prev => prev - 1);
    setIsLoading(false);
  }, [currentPage, isLoading]);
  
  const goToPage = useCallback(async (page: number) => {
    if (page < 0 || page >= totalPages || page === currentPage || isLoading) return;
    
    setIsLoading(true);
    await new Promise(resolve => setTimeout(resolve, 100));
    setCurrentPage(page);
    setIsLoading(false);
  }, [totalPages, currentPage, isLoading]);
  
  const reset = useCallback(() => {
    setCurrentPage(0);
    setIsLoading(false);
  }, []);
  
  return {
    currentItems,
    currentPage,
    totalPages,
    isLoading,
    hasNextPage: currentPage < totalPages - 1,
    hasPreviousPage: currentPage > 0,
    loadNextPage,
    loadPreviousPage,
    goToPage,
    reset
  };
};

// Performance utilities
export const useVirtualScrollPerformance = () => {
  const [metrics, setMetrics] = useState({
    renderTime: 0,
    scrollEvents: 0,
    lastUpdate: Date.now()
  });
  
  const trackRender = useCallback((renderFn: () => void) => {
    const start = performance.now();
    renderFn();
    const end = performance.now();
    
    setMetrics(prev => ({
      ...prev,
      renderTime: end - start,
      lastUpdate: Date.now()
    }));
  }, []);
  
  const trackScroll = useCallback(() => {
    setMetrics(prev => ({
      ...prev,
      scrollEvents: prev.scrollEvents + 1
    }));
  }, []);
  
  const resetMetrics = useCallback(() => {
    setMetrics({
      renderTime: 0,
      scrollEvents: 0,
      lastUpdate: Date.now()
    });
  }, []);
  
  return {
    metrics,
    trackRender,
    trackScroll,
    resetMetrics
  };
};