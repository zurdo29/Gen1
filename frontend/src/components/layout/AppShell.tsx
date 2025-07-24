import React from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  Container,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Tooltip,
  LinearProgress,
  useTheme,
  useMediaQuery,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Divider,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Settings as SettingsIcon,
  Share as ShareIcon,
  Download as DownloadIcon,
  Home as HomeIcon,
  Edit as EditIcon,
  Folder as FolderIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { LoadingSpinner } from '../common/LoadingSpinner';

interface AppShellProps {
  children: React.ReactNode;
  isLoading?: boolean;
  loadingMessage?: string;
  progress?: number;
}

export const AppShell: React.FC<AppShellProps> = ({ 
  children, 
  isLoading = false,
  loadingMessage,
  progress 
}) => {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const [drawerOpen, setDrawerOpen] = React.useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const navigate = useNavigate();
  const location = useLocation();

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    if (isMobile) {
      setDrawerOpen(true);
    } else {
      setAnchorEl(event.currentTarget);
    }
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleDrawerClose = () => {
    setDrawerOpen(false);
  };

  const handleNavigation = (path: string) => {
    navigate(path);
    handleMenuClose();
    handleDrawerClose();
  };

  const menuItems = [
    { label: 'Home', icon: <HomeIcon />, path: '/' },
    { label: 'Editor', icon: <EditIcon />, path: '/editor' },
    { label: 'Load Preset', icon: <FolderIcon />, action: 'load-preset' },
  ];

  const renderNavigationMenu = () => (
    <>
      {menuItems.map((item) => (
        <MenuItem 
          key={item.label}
          onClick={() => {
            if (item.path) {
              handleNavigation(item.path);
            } else if (item.action) {
              // Handle special actions
              console.log(`Action: ${item.action}`);
              handleMenuClose();
            }
          }}
        >
          {item.label}
        </MenuItem>
      ))}
      <Divider />
      <MenuItem onClick={handleMenuClose}>New Project</MenuItem>
      <MenuItem onClick={handleMenuClose}>Save Preset</MenuItem>
      <MenuItem onClick={handleMenuClose}>Import Configuration</MenuItem>
    </>
  );

  const renderDrawerContent = () => (
    <Box sx={{ width: 250 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2 }}>
        <Typography variant="h6">Menu</Typography>
        <IconButton onClick={handleDrawerClose}>
          <CloseIcon />
        </IconButton>
      </Box>
      <Divider />
      <List>
        {menuItems.map((item) => (
          <ListItem 
            key={item.label}
            onClick={() => {
              if (item.path) {
                handleNavigation(item.path);
              } else if (item.action) {
                console.log(`Action: ${item.action}`);
                handleDrawerClose();
              }
            }}
            sx={{ 
              cursor: 'pointer',
              backgroundColor: location.pathname === item.path ? 'action.selected' : 'transparent'
            }}
          >
            <ListItemIcon>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItem>
        ))}
        <Divider sx={{ my: 1 }} />
        <ListItem onClick={handleDrawerClose} sx={{ cursor: 'pointer' }}>
          <ListItemText primary="New Project" />
        </ListItem>
        <ListItem onClick={handleDrawerClose} sx={{ cursor: 'pointer' }}>
          <ListItemText primary="Save Preset" />
        </ListItem>
        <ListItem onClick={handleDrawerClose} sx={{ cursor: 'pointer' }}>
          <ListItemText primary="Import Configuration" />
        </ListItem>
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static" elevation={1}>
        <Toolbar>
          <IconButton
            edge="start"
            color="inherit"
            aria-label="menu"
            onClick={handleMenuOpen}
            sx={{ mr: 2 }}
          >
            <MenuIcon />
          </IconButton>
          
          <Typography 
            variant={isMobile ? "h6" : "h5"} 
            component="div" 
            sx={{ 
              flexGrow: 1,
              cursor: 'pointer',
              '&:hover': { opacity: 0.8 }
            }}
            onClick={() => handleNavigation('/')}
          >
            {isMobile ? "Level Editor" : "Procedural Level Editor"}
          </Typography>

          {!isMobile && (
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Tooltip title="Share Configuration">
                <span>
                  <IconButton color="inherit" disabled={location.pathname !== '/editor'}>
                    <ShareIcon />
                  </IconButton>
                </span>
              </Tooltip>
              
              <Tooltip title="Export Level">
                <span>
                  <IconButton color="inherit" disabled={location.pathname !== '/editor'}>
                    <DownloadIcon />
                  </IconButton>
                </span>
              </Tooltip>
              
              <Tooltip title="Settings">
                <IconButton color="inherit">
                  <SettingsIcon />
                </IconButton>
              </Tooltip>
            </Box>
          )}
        </Toolbar>
        
        {/* Loading progress bar */}
        {isLoading && (
          <LinearProgress 
            variant={progress !== undefined ? 'determinate' : 'indeterminate'}
            value={progress}
            sx={{ height: 2 }}
          />
        )}
      </AppBar>

      {/* Desktop Menu */}
      {!isMobile && (
        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          PaperProps={{
            sx: { minWidth: 200 }
          }}
        >
          {renderNavigationMenu()}
        </Menu>
      )}

      {/* Mobile Drawer */}
      <Drawer
        anchor="left"
        open={drawerOpen}
        onClose={handleDrawerClose}
        ModalProps={{
          keepMounted: true, // Better open performance on mobile
        }}
      >
        {renderDrawerContent()}
      </Drawer>

      <Container 
        maxWidth="xl" 
        sx={{ 
          flex: 1, 
          py: 2,
          display: 'flex',
          flexDirection: 'column',
          position: 'relative'
        }}
      >
        {isLoading && loadingMessage ? (
          <Box sx={{ 
            position: 'absolute', 
            top: 0, 
            left: 0, 
            right: 0, 
            bottom: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: 'rgba(255, 255, 255, 0.8)',
            zIndex: 1000
          }}>
            <LoadingSpinner 
              message={loadingMessage}
              progress={progress}
              variant={progress !== undefined ? 'circular' : 'circular'}
            />
          </Box>
        ) : null}
        {children}
      </Container>
    </Box>
  );
};