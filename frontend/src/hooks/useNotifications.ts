import { useState, useCallback } from 'react';
import { Notification } from '../types';

export const useNotifications = () => {
  const [notifications, setNotifications] = useState<Notification[]>([]);

  const addNotification = useCallback((
    type: Notification['type'],
    title: string,
    message: string,
    autoHide: boolean = true
  ) => {
    const notification: Notification = {
      id: Date.now().toString(),
      type,
      title,
      message,
      timestamp: new Date(),
      autoHide,
    };

    setNotifications(prev => [...prev, notification]);

    if (autoHide) {
      setTimeout(() => {
        removeNotification(notification.id);
      }, 5000);
    }

    return notification.id;
  }, []);

  const removeNotification = useCallback((id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  }, []);

  const clearAllNotifications = useCallback(() => {
    setNotifications([]);
  }, []);

  const showSuccess = useCallback((title: string, message: string) => {
    return addNotification('success', title, message);
  }, [addNotification]);

  const showError = useCallback((title: string, message: string) => {
    return addNotification('error', title, message, false);
  }, [addNotification]);

  const showWarning = useCallback((title: string, message: string) => {
    return addNotification('warning', title, message);
  }, [addNotification]);

  const showInfo = useCallback((title: string, message: string) => {
    return addNotification('info', title, message);
  }, [addNotification]);

  return {
    notifications,
    addNotification,
    removeNotification,
    clearAllNotifications,
    showSuccess,
    showError,
    showWarning,
    showInfo,
  };
};