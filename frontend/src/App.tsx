import { useState } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ErrorBoundary } from './components/common/ErrorBoundary';
import { AppShell } from './components/layout/AppShell';
import { NotificationSystem } from './components/common/NotificationSystem';
import { ErrorDialog } from './components/common/ErrorDialog';
import { HomePage } from './pages/HomePage';
import { EditorPage } from './pages/EditorPage';
import { useNotifications } from './hooks/useNotifications';
import { useErrorHandling } from './hooks/useErrorHandling';

function App() {
  const { notifications, removeNotification } = useNotifications();
  const { 
    error, 
    isErrorDialogOpen, 
    closeErrorDialog, 
    retryLastAction 
  } = useErrorHandling();
  
  const [isLoading, setIsLoading] = useState(false);
  const [loadingMessage, setLoadingMessage] = useState<string>();
  const [progress, setProgress] = useState<number>();

  // Global loading state management
  const showLoading = (message: string, progressValue?: number) => {
    setLoadingMessage(message);
    setProgress(progressValue);
    setIsLoading(true);
  };

  const hideLoading = () => {
    setIsLoading(false);
    setLoadingMessage(undefined);
    setProgress(undefined);
  };

  const updateProgress = (progressValue: number) => {
    setProgress(progressValue);
  };

  return (
    <ErrorBoundary>
      <Router
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true
        }}
      >
        <AppShell 
          isLoading={isLoading}
          loadingMessage={loadingMessage}
          progress={progress}
        >
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route 
              path="/editor" 
              element={
                <EditorPage 
                  onShowLoading={showLoading}
                  onHideLoading={hideLoading}
                  onUpdateProgress={updateProgress}
                />
              } 
            />
            <Route path="*" element={<HomePage />} />
          </Routes>
        </AppShell>
        <NotificationSystem 
          notifications={notifications}
          onRemove={removeNotification}
        />
        <ErrorDialog
          open={isErrorDialogOpen}
          error={error}
          onClose={closeErrorDialog}
          onRetry={retryLastAction}
        />
      </Router>
    </ErrorBoundary>
  );
}

export default App;