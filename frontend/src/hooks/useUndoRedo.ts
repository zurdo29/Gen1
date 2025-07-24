import { useState, useCallback } from 'react';

export interface UndoRedoAction<T = any> {
  id: string;
  type: string;
  description: string;
  undo: () => void;
  redo: () => void;
  data?: T;
  timestamp: Date;
}

interface UseUndoRedoReturn {
  canUndo: boolean;
  canRedo: boolean;
  undoStack: UndoRedoAction[];
  redoStack: UndoRedoAction[];
  executeAction: (action: UndoRedoAction) => void;
  undo: () => void;
  redo: () => void;
  clearHistory: () => void;
  getLastAction: () => UndoRedoAction | null;
}

const MAX_HISTORY_SIZE = 50;

export const useUndoRedo = (): UseUndoRedoReturn => {
  const [undoStack, setUndoStack] = useState<UndoRedoAction[]>([]);
  const [redoStack, setRedoStack] = useState<UndoRedoAction[]>([]);

  const executeAction = useCallback((action: UndoRedoAction) => {
    // Execute the action (redo function represents the forward action)
    action.redo();
    
    // Add to undo stack
    setUndoStack(prev => {
      const newStack = [...prev, action];
      // Limit stack size
      if (newStack.length > MAX_HISTORY_SIZE) {
        return newStack.slice(-MAX_HISTORY_SIZE);
      }
      return newStack;
    });
    
    // Clear redo stack when new action is executed
    setRedoStack([]);
  }, []);

  const undo = useCallback(() => {
    if (undoStack.length === 0) return;
    
    const lastAction = undoStack[undoStack.length - 1];
    
    // Execute undo
    lastAction.undo();
    
    // Move from undo to redo stack
    setUndoStack(prev => prev.slice(0, -1));
    setRedoStack(prev => [...prev, lastAction]);
  }, [undoStack]);

  const redo = useCallback(() => {
    if (redoStack.length === 0) return;
    
    const lastAction = redoStack[redoStack.length - 1];
    
    // Execute redo
    lastAction.redo();
    
    // Move from redo to undo stack
    setRedoStack(prev => prev.slice(0, -1));
    setUndoStack(prev => [...prev, lastAction]);
  }, [redoStack]);

  const clearHistory = useCallback(() => {
    setUndoStack([]);
    setRedoStack([]);
  }, []);

  const getLastAction = useCallback(() => {
    return undoStack.length > 0 ? undoStack[undoStack.length - 1] : null;
  }, [undoStack]);

  return {
    canUndo: undoStack.length > 0,
    canRedo: redoStack.length > 0,
    undoStack,
    redoStack,
    executeAction,
    undo,
    redo,
    clearHistory,
    getLastAction
  };
};