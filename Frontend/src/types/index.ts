// ============================================
// Hardware & Report Types
// ============================================

export interface HardwareReport {
  cpu: CpuInfo
  gpu: GpuInfo[]
  audio: AudioInfo[]
  network: NetworkInfo[]
  storage: StorageInfo[]
  usb: UsbControllerInfo[]
  motherboard: MotherboardInfo
  memory: MemoryInfo
  generatedAt: string
  platform: 'Windows' | 'Linux' | 'macOS'
}

export interface CpuInfo {
  name: string
  codename: string
  vendor: 'Intel' | 'AMD' | 'Unknown'
  family: number
  model: number
  stepping: number
  cores: number
  threads: number
  supportedFeatures: string[]
}

export interface GpuInfo {
  name: string
  vendor: string
  deviceId: string
  codename?: string
  vram?: number
  discrete: boolean
}

export interface AudioInfo {
  name: string
  vendorId: string
  deviceId: string
  codecId?: number
  suggestedLayouts?: number[]
}

export interface NetworkInfo {
  name: string
  type: 'Ethernet' | 'WiFi' | 'Bluetooth' | 'Unknown'
  vendorId: string
  deviceId: string
  macAddress?: string
}

export interface StorageInfo {
  name: string
  type: 'NVMe' | 'SATA' | 'USB' | 'Unknown'
  size: number
  vendorId?: string
  deviceId?: string
}

export interface UsbControllerInfo {
  name: string
  type: 'XHCI' | 'EHCI' | 'OHCI' | 'UHCI' | 'Unknown'
  vendorId: string
  deviceId: string
  portCount: number
}

export interface MotherboardInfo {
  manufacturer: string
  model: string
  biosVersion?: string
  chipset?: string
}

export interface MemoryInfo {
  totalSize: number
  slots: MemorySlot[]
}

export interface MemorySlot {
  size: number
  speed: number
  type: string
  manufacturer?: string
}

// ============================================
// Compatibility Types
// ============================================

export type CompatibilityStatus = 'supported' | 'limited' | 'unsupported' | 'unknown'

export interface CompatibilityResult {
  devices: DeviceCompatibility[]
  overallStatus: CompatibilityStatus
  warnings: string[]
  blockers: string[]
}

export interface DeviceCompatibility {
  category: string
  name: string
  status: CompatibilityStatus
  notes: string
  requiredKexts?: string[]
  minMacOS?: string
  maxMacOS?: string
}

// ============================================
// macOS Version Types
// ============================================

export interface MacOSVersion {
  name: string
  version: string
  darwin: string
  buildNumber?: string
  releaseDate?: string
  supported: boolean
  warnings?: string[]
}

// ============================================
// ACPI Patch Types
// ============================================

export interface AcpiPatch {
  id: string
  name: string
  description: string
  category: 'Required' | 'Recommended' | 'Optional' | 'Hardware-Specific'
  enabled: boolean
  fileName?: string
  required: boolean
  dependencies?: string[]
}

// ============================================
// Kext Types
// ============================================

export interface KextInfo {
  id: string
  name: string
  version: string
  description: string
  category: 'Core' | 'Audio' | 'Graphics' | 'Network' | 'Storage' | 'USB' | 'Other'
  enabled: boolean
  required: boolean
  downloadUrl?: string
  dependencies?: string[]
  conflicts?: string[]
  minMacOS?: string
  maxMacOS?: string
}

// ============================================
// SMBIOS Types
// ============================================

export interface SmbiosModel {
  id: string
  name: string
  year: number
  cpuFamily: string
  gpuFamily: string
  recommended: boolean
  notes?: string
  minMacOS: string
  maxMacOS?: string
}

export interface SmbiosConfig {
  model: string
  serial: string
  mlb: string
  uuid: string
  rom: string
}

// ============================================
// USB Mapper Types
// ============================================

export interface UsbController {
  name: string
  type: 'XHCI' | 'EHCI' | 'OHCI' | 'UHCI' | 'Unknown'
  ports: UsbPort[]
  selectedCount: number
}

export interface UsbPort {
  name: string
  index: number
  speedClass: 'SuperSpeedPlus' | 'SuperSpeed' | 'HighSpeed' | 'FullSpeed' | 'LowSpeed' | 'Unknown'
  connectorType?: number
  guessedType?: number
  devices: UsbDevice[]
  selected: boolean
  comment?: string
}

export interface UsbDevice {
  name: string
  speed?: string
  instanceId?: string
}

export type UsbConnectorType =
  | { value: 0; label: 'USB 2 Type A' }
  | { value: 3; label: 'USB 3 Type A' }
  | { value: 8; label: 'Type C - USB 2' }
  | { value: 9; label: 'Type C + Switch' }
  | { value: 10; label: 'Type C - No Switch' }
  | { value: 255; label: 'Internal' }

export const USB_CONNECTOR_TYPES: UsbConnectorType[] = [
  { value: 0, label: 'USB 2 Type A' },
  { value: 3, label: 'USB 3 Type A' },
  { value: 8, label: 'Type C - USB 2' },
  { value: 9, label: 'Type C + Switch' },
  { value: 10, label: 'Type C - No Switch' },
  { value: 255, label: 'Internal' },
]

// ============================================
// Build Types
// ============================================

export type BuildStage =
  | 'idle'
  | 'downloading-opencore'
  | 'downloading-kexts'
  | 'generating-acpi'
  | 'generating-config'
  | 'generating-usb-map'
  | 'packaging'
  | 'complete'
  | 'error'

export interface BuildProgress {
  stage: BuildStage
  progress: number
  message: string
  log: string[]
}

export interface BuildResult {
  success: boolean
  outputPath: string
  biosSettings: BiosSetting[]
  nextSteps: string[]
  errors?: string[]
}

export interface BiosSetting {
  name: string
  recommended: string
  category: string
  required: boolean
}

// ============================================
// App State Types
// ============================================

export interface AppState {
  currentStep: number
  report?: HardwareReport
  compatibility?: CompatibilityResult
  selectedMacOS?: MacOSVersion
  acpiPatches: AcpiPatch[]
  kexts: KextInfo[]
  smbios?: SmbiosConfig
  usbControllers: UsbController[]
  buildProgress?: BuildProgress
  buildResult?: BuildResult
}

// ============================================
// Bridge Message Types
// ============================================

export type BridgeAction =
  | 'hardware:detect'
  | 'hardware:load-report'
  | 'compatibility:check'
  | 'macos:list'
  | 'acpi:list'
  | 'acpi:toggle'
  | 'kexts:list'
  | 'kexts:toggle'
  | 'smbios:list'
  | 'smbios:generate'
  | 'usb:scan'
  | 'usb:toggle-port'
  | 'usb:set-type'
  | 'build:start'
  | 'build:cancel'
  | 'result:open-folder'

export type BridgeEvent =
  | 'hardware:detected'
  | 'hardware:loaded'
  | 'compatibility:result'
  | 'macos:versions'
  | 'acpi:patches'
  | 'kexts:list'
  | 'smbios:models'
  | 'smbios:generated'
  | 'usb:controllers'
  | 'usb:updated'
  | 'build:progress'
  | 'build:complete'
  | 'build:error'
  | 'error'
