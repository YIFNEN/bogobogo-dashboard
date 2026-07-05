import { useEffect, useMemo, useRef, useState } from 'react';
import FactoryMapFallback from './FactoryMapFallback';
import type { IncidentListItem, UnityDashboardState, UnityMapBrowserEvent } from '../../types';

type UnityMode = 'checking' | 'loading' | 'ready' | 'fallback';

interface UnityInstance {
  SendMessage: (objectName: string, methodName: string, value?: string) => void;
  Quit?: () => Promise<void>;
}

declare global {
  interface Window {
    createUnityInstance?: (
      canvas: HTMLCanvasElement,
      config: Record<string, string>,
      onProgress?: (progress: number) => void,
    ) => Promise<UnityInstance>;
  }
}

const DEFAULT_BUILD_ROOT = '/simulator/Build';
const DEFAULT_BUILD_BASENAME = 'simulator';
const unityObjectName = 'FactorySimulator';
const UNITY_BOOT_TIMEOUT_MS = 12000;

function resolveBuildRoot(): string {
  return import.meta.env.VITE_UNITY_FACTORY_BUILD_ROOT ?? DEFAULT_BUILD_ROOT;
}

function resolveBuildBasename(): string {
  return import.meta.env.VITE_UNITY_FACTORY_BUILD_BASENAME ?? DEFAULT_BUILD_BASENAME;
}

function resolveLoaderUrl(): string {
  return import.meta.env.VITE_UNITY_FACTORY_LOADER_URL ?? `${resolveBuildRoot()}/${resolveBuildBasename()}.loader.js`;
}

function shouldForceFallback(): boolean {
  const params = new URLSearchParams(window.location.search);
  return import.meta.env.VITE_UNITY_FACTORY_DISABLE_WEBGL === '1' ||
    params.get('unity') === '0' ||
    params.get('simulator') === 'fallback';
}

function unityConfig() {
  const root = resolveBuildRoot();
  const basename = resolveBuildBasename();
  return {
    dataUrl: `${root}/${basename}.data`,
    frameworkUrl: `${root}/${basename}.framework.js`,
    codeUrl: `${root}/${basename}.wasm`,
    streamingAssetsUrl: '/simulator/StreamingAssets',
    companyName: 'Bogobogo',
    productName: 'Factory Simulator',
    productVersion: '0.1.0',
  };
}

interface UnityFactoryMapProps {
  state: UnityDashboardState;
  incidents: IncidentListItem[];
  mapZoom: number;
  resetNonce: number;
  onIncidentSelect: (incidentId: string) => void;
}

export default function UnityFactoryMap({ state, incidents, mapZoom, resetNonce, onIncidentSelect }: UnityFactoryMapProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const unityRef = useRef<UnityInstance | null>(null);
  const pendingStateRef = useRef<UnityDashboardState>(state);
  const pendingZoomRef = useRef(mapZoom);
  const [mode, setMode] = useState<UnityMode>('checking');
  const loaderUrl = useMemo(resolveLoaderUrl, []);

  useEffect(() => {
    let cancelled = false;
    let script: HTMLScriptElement | null = null;
    let bootTimer: number | null = null;
    let resolved = false;

    const clearBootTimer = () => {
      if (bootTimer === null) return;
      window.clearTimeout(bootTimer);
      bootTimer = null;
    };

    const switchToFallback = () => {
      if (cancelled || resolved) return;
      resolved = true;
      clearBootTimer();
      setMode('fallback');
    };

    async function bootUnity() {
      if (shouldForceFallback()) {
        setMode('fallback');
        return;
      }

      if (!canvasRef.current) {
        setMode('fallback');
        return;
      }

      try {
        const response = await fetch(loaderUrl, { method: 'HEAD' });
        if (!response.ok) {
          switchToFallback();
          return;
        }
      } catch {
        switchToFallback();
        return;
      }

      setMode('loading');
      bootTimer = window.setTimeout(switchToFallback, UNITY_BOOT_TIMEOUT_MS);
      script = document.createElement('script');
      script.src = loaderUrl;
      script.async = true;
      script.onload = async () => {
        if (cancelled || !canvasRef.current || !window.createUnityInstance) {
          switchToFallback();
          return;
        }

        try {
          const unity = await window.createUnityInstance(canvasRef.current, unityConfig());
          if (cancelled || resolved) {
            void unity.Quit?.();
            return;
          }
          resolved = true;
          clearBootTimer();
          unityRef.current = unity;
          setMode('ready');
          unityRef.current.SendMessage(unityObjectName, 'ApplyDashboardState', JSON.stringify(pendingStateRef.current));
          unityRef.current.SendMessage(unityObjectName, 'SetZoomLevel', String(pendingZoomRef.current));
        } catch {
          switchToFallback();
        }
      };
      script.onerror = () => {
        switchToFallback();
      };
      document.body.appendChild(script);
    }

    bootUnity();

    return () => {
      cancelled = true;
      clearBootTimer();
      if (script?.parentNode) script.parentNode.removeChild(script);
      void unityRef.current?.Quit?.();
      unityRef.current = null;
    };
  }, [loaderUrl]);

  useEffect(() => {
    pendingStateRef.current = state;
    unityRef.current?.SendMessage(unityObjectName, 'ApplyDashboardState', JSON.stringify(state));
  }, [state]);

  useEffect(() => {
    pendingZoomRef.current = mapZoom;
    unityRef.current?.SendMessage(unityObjectName, 'SetZoomLevel', String(mapZoom));
  }, [mapZoom]);

  useEffect(() => {
    if (resetNonce > 0) unityRef.current?.SendMessage(unityObjectName, 'ResetView');
  }, [resetNonce]);

  useEffect(() => {
    const handleUnityEvent = (event: Event) => {
      const detail = (event as CustomEvent<UnityMapBrowserEvent>).detail;
      if (detail?.type === 'incident_selected' && detail.incident_id) {
        onIncidentSelect(detail.incident_id);
      }
    };

    window.addEventListener('bogobogo:unity-map-event', handleUnityEvent);
    return () => window.removeEventListener('bogobogo:unity-map-event', handleUnityEvent);
  }, [onIncidentSelect]);

  if (mode === 'fallback') {
    return (
      <FactoryMapFallback
        state={state}
        incidents={incidents}
        mapZoom={mapZoom}
        onIncidentSelect={onIncidentSelect}
      />
    );
  }

  return (
    <div className="unity-factory-map-shell" onContextMenu={event => event.preventDefault()}>
      <canvas ref={canvasRef} className="unity-factory-map-canvas" />
      {(mode === 'checking' || mode === 'loading') && <div className="unity-factory-map-loading" aria-hidden="true" />}
    </div>
  );
}
