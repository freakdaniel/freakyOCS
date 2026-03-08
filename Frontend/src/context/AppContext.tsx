import { createContext, useContext, useReducer, type ReactNode } from 'react'
import type {
  AppState,
  HardwareReport,
  CompatibilityResult,
  MacOSVersion,
  AcpiPatch,
  KextInfo,
  SmbiosConfig,
  UsbController,
  BuildProgress,
  BuildResult,
} from '../types'

// ============================================
// Actions
// ============================================

type AppAction =
  | { type: 'SET_STEP'; step: number }
  | { type: 'SET_REPORT'; report: HardwareReport }
  | { type: 'SET_COMPATIBILITY'; result: CompatibilityResult }
  | { type: 'SET_MACOS'; version: MacOSVersion }
  | { type: 'SET_ACPI_PATCHES'; patches: AcpiPatch[] }
  | { type: 'TOGGLE_ACPI_PATCH'; id: string }
  | { type: 'SET_KEXTS'; kexts: KextInfo[] }
  | { type: 'TOGGLE_KEXT'; id: string }
  | { type: 'SET_SMBIOS'; config: SmbiosConfig }
  | { type: 'SET_USB_CONTROLLERS'; controllers: UsbController[] }
  | { type: 'UPDATE_USB_CONTROLLER'; index: number; controller: UsbController }
  | { type: 'SET_BUILD_PROGRESS'; progress: BuildProgress }
  | { type: 'SET_BUILD_RESULT'; result: BuildResult }
  | { type: 'RESET' }

// ============================================
// Initial State
// ============================================

const initialState: AppState = {
  currentStep: 0,
  acpiPatches: [],
  kexts: [],
  usbControllers: [],
}

// ============================================
// Reducer
// ============================================

function appReducer(state: AppState, action: AppAction): AppState {
  switch (action.type) {
    case 'SET_STEP':
      return { ...state, currentStep: action.step }

    case 'SET_REPORT':
      return { ...state, report: action.report }

    case 'SET_COMPATIBILITY':
      return { ...state, compatibility: action.result }

    case 'SET_MACOS':
      return { ...state, selectedMacOS: action.version }

    case 'SET_ACPI_PATCHES':
      return { ...state, acpiPatches: action.patches }

    case 'TOGGLE_ACPI_PATCH':
      return {
        ...state,
        acpiPatches: state.acpiPatches.map(p =>
          p.id === action.id ? { ...p, enabled: !p.enabled } : p
        ),
      }

    case 'SET_KEXTS':
      return { ...state, kexts: action.kexts }

    case 'TOGGLE_KEXT':
      return {
        ...state,
        kexts: state.kexts.map(k =>
          k.id === action.id ? { ...k, enabled: !k.enabled } : k
        ),
      }

    case 'SET_SMBIOS':
      return { ...state, smbios: action.config }

    case 'SET_USB_CONTROLLERS':
      return { ...state, usbControllers: action.controllers }

    case 'UPDATE_USB_CONTROLLER':
      return {
        ...state,
        usbControllers: state.usbControllers.map((c, i) =>
          i === action.index ? action.controller : c
        ),
      }

    case 'SET_BUILD_PROGRESS':
      return { ...state, buildProgress: action.progress }

    case 'SET_BUILD_RESULT':
      return { ...state, buildResult: action.result }

    case 'RESET':
      return initialState

    default:
      return state
  }
}

// ============================================
// Context
// ============================================

interface AppContextValue {
  state: AppState
  dispatch: React.Dispatch<AppAction>
}

const AppContext = createContext<AppContextValue | null>(null)

export function AppProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(appReducer, initialState)

  return (
    <AppContext.Provider value={{ state, dispatch }}>
      {children}
    </AppContext.Provider>
  )
}

export function useApp() {
  const context = useContext(AppContext)
  if (!context) {
    throw new Error('useApp must be used within AppProvider')
  }
  return context
}

// ============================================
// Navigation Steps
// ============================================

export const WIZARD_STEPS = [
  { id: 'home', label: 'Home', path: '/', icon: '🏠' },
  { id: 'report', label: 'Hardware', path: '/report', icon: '🖥️' },
  { id: 'compatibility', label: 'Compatibility', path: '/compatibility', icon: '✅' },
  { id: 'macos', label: 'macOS', path: '/macos', icon: '🍎' },
  { id: 'acpi', label: 'ACPI', path: '/acpi', icon: '⚙️' },
  { id: 'kexts', label: 'Kexts', path: '/kexts', icon: '📦' },
  { id: 'smbios', label: 'SMBIOS', path: '/smbios', icon: '🏷️' },
  { id: 'usb', label: 'USB Map', path: '/usb', icon: '🔌' },
  { id: 'build', label: 'Build', path: '/build', icon: '🔨' },
  { id: 'result', label: 'Result', path: '/result', icon: '🎉' },
] as const

export type StepId = (typeof WIZARD_STEPS)[number]['id']
