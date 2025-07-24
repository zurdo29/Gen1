import { renderHook, act } from '@testing-library/react';
import { vi } from 'vitest';
import { useUndoRedo, UndoRedoAction } from './useUndoRedo';

describe('useUndoRedo', () => {
  it('initializes with empty stacks', () => {
    const { result } = renderHook(() => useUndoRedo());

    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
    expect(result.current.undoStack).toHaveLength(0);
    expect(result.current.redoStack).toHaveLength(0);
  });

  it('executes action and adds to undo stack', () => {
    const { result } = renderHook(() => useUndoRedo());
    const mockRedo = vi.fn();
    const mockUndo = vi.fn();

    const action: UndoRedoAction = {
      id: 'test-1',
      type: 'test',
      description: 'Test action',
      timestamp: new Date(),
      redo: mockRedo,
      undo: mockUndo
    };

    act(() => {
      result.current.executeAction(action);
    });

    expect(mockRedo).toHaveBeenCalledTimes(1);
    expect(result.current.canUndo).toBe(true);
    expect(result.current.canRedo).toBe(false);
    expect(result.current.undoStack).toHaveLength(1);
  });

  it('performs undo operation', () => {
    const { result } = renderHook(() => useUndoRedo());
    const mockRedo = vi.fn();
    const mockUndo = vi.fn();

    const action: UndoRedoAction = {
      id: 'test-1',
      type: 'test',
      description: 'Test action',
      timestamp: new Date(),
      redo: mockRedo,
      undo: mockUndo
    };

    act(() => {
      result.current.executeAction(action);
    });

    act(() => {
      result.current.undo();
    });

    expect(mockUndo).toHaveBeenCalledTimes(1);
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(true);
    expect(result.current.undoStack).toHaveLength(0);
    expect(result.current.redoStack).toHaveLength(1);
  });

  it('performs redo operation', () => {
    const { result } = renderHook(() => useUndoRedo());
    const mockRedo = vi.fn();
    const mockUndo = vi.fn();

    const action: UndoRedoAction = {
      id: 'test-1',
      type: 'test',
      description: 'Test action',
      timestamp: new Date(),
      redo: mockRedo,
      undo: mockUndo
    };

    act(() => {
      result.current.executeAction(action);
    });

    act(() => {
      result.current.undo();
    });

    act(() => {
      result.current.redo();
    });

    expect(mockRedo).toHaveBeenCalledTimes(2); // Once for execute, once for redo
    expect(result.current.canUndo).toBe(true);
    expect(result.current.canRedo).toBe(false);
    expect(result.current.undoStack).toHaveLength(1);
    expect(result.current.redoStack).toHaveLength(0);
  });

  it('clears redo stack when new action is executed', () => {
    const { result } = renderHook(() => useUndoRedo());
    const mockRedo1 = vi.fn();
    const mockUndo1 = vi.fn();
    const mockRedo2 = vi.fn();
    const mockUndo2 = vi.fn();

    const action1: UndoRedoAction = {
      id: 'test-1',
      type: 'test',
      description: 'Test action 1',
      timestamp: new Date(),
      redo: mockRedo1,
      undo: mockUndo1
    };

    const action2: UndoRedoAction = {
      id: 'test-2',
      type: 'test',
      description: 'Test action 2',
      timestamp: new Date(),
      redo: mockRedo2,
      undo: mockUndo2
    };

    // Execute first action
    act(() => {
      result.current.executeAction(action1);
    });

    // Undo first action
    act(() => {
      result.current.undo();
    });

    expect(result.current.canRedo).toBe(true);

    // Execute second action - should clear redo stack
    act(() => {
      result.current.executeAction(action2);
    });

    expect(result.current.canRedo).toBe(false);
    expect(result.current.redoStack).toHaveLength(0);
  });

  it('limits undo stack size', () => {
    const { result } = renderHook(() => useUndoRedo());

    // Execute 60 actions (more than MAX_HISTORY_SIZE of 50)
    for (let i = 0; i < 60; i++) {
      const action: UndoRedoAction = {
        id: `test-${i}`,
        type: 'test',
        description: `Test action ${i}`,
        timestamp: new Date(),
        redo: vi.fn(),
        undo: vi.fn()
      };

      act(() => {
        result.current.executeAction(action);
      });
    }

    // Should be limited to 50 items
    expect(result.current.undoStack).toHaveLength(50);
    // Should contain the most recent 50 actions
    expect(result.current.undoStack[0].id).toBe('test-10');
    expect(result.current.undoStack[49].id).toBe('test-59');
  });

  it('clears history', () => {
    const { result } = renderHook(() => useUndoRedo());
    const mockRedo = vi.fn();
    const mockUndo = vi.fn();

    const action: UndoRedoAction = {
      id: 'test-1',
      type: 'test',
      description: 'Test action',
      timestamp: new Date(),
      redo: mockRedo,
      undo: mockUndo
    };

    act(() => {
      result.current.executeAction(action);
    });

    act(() => {
      result.current.undo();
    });

    expect(result.current.undoStack).toHaveLength(0);
    expect(result.current.redoStack).toHaveLength(1);

    act(() => {
      result.current.clearHistory();
    });

    expect(result.current.undoStack).toHaveLength(0);
    expect(result.current.redoStack).toHaveLength(0);
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });

  it('returns last action', () => {
    const { result } = renderHook(() => useUndoRedo());
    const mockRedo = vi.fn();
    const mockUndo = vi.fn();

    expect(result.current.getLastAction()).toBeNull();

    const action: UndoRedoAction = {
      id: 'test-1',
      type: 'test',
      description: 'Test action',
      timestamp: new Date(),
      redo: mockRedo,
      undo: mockUndo
    };

    act(() => {
      result.current.executeAction(action);
    });

    expect(result.current.getLastAction()).toBe(action);
  });

  it('handles undo when stack is empty', () => {
    const { result } = renderHook(() => useUndoRedo());

    act(() => {
      result.current.undo();
    });

    // Should not crash and state should remain unchanged
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });

  it('handles redo when stack is empty', () => {
    const { result } = renderHook(() => useUndoRedo());

    act(() => {
      result.current.redo();
    });

    // Should not crash and state should remain unchanged
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });
});