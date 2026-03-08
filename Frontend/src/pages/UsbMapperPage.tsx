import { useEffect, useCallback, useState } from 'react'
import { Box, Text, HStack, Flex, Spinner } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { UsbController, UsbPort } from '../types'
import { USB_CONNECTOR_TYPES } from '../types'
import {
  RefreshCw, ChevronLeft, ChevronRight, Check,
  AlertTriangle, Zap, Info,
} from 'lucide-react'

/** Strip "@driver.inf,%key%;" INF-style prefixes Windows puts in registry device names. */
const cleanDeviceName = (name: string) => {
  if (name.startsWith('@')) {
    const idx = name.indexOf(';')
    if (idx >= 0) return name.slice(idx + 1).trim()
  }
  return name
}

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const speedColors: Record<string, string> = {
  SuperSpeedPlus: '#A78BFA',
  SuperSpeed:     '#38BDF8',
  HighSpeed:      '#22C55E',
  FullSpeed:      '#EAB308',
  LowSpeed:       '#f97316',
  Unknown:        '#555',
}

const speedLabels: Record<string, string> = {
  SuperSpeedPlus: 'SS+ 10Gbps',
  SuperSpeed:     'SS 5Gbps',
  HighSpeed:      'HS 480Mbps',
  FullSpeed:      'FS 12Mbps',
  LowSpeed:       'LS 1.5Mbps',
  Unknown:        'Unknown',
}

export function UsbMapperPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isScanning, setIsScanning] = useState(false)

  usePhotinoEvent<UsbController[]>('usb:controllers', (data) => {
    setIsScanning(false)
    if (data) dispatch({ type: 'SET_USB_CONTROLLERS', controllers: data })
  })

  usePhotinoEvent<UsbController[]>('usb:updated', (data) => {
    if (data) dispatch({ type: 'SET_USB_CONTROLLERS', controllers: data })
  })

  const startScan = useCallback(() => {
    setIsScanning(true)
    invoke('usb:scan').catch(() => setIsScanning(false))
  }, [invoke])

  useEffect(() => {
    if (state.usbControllers.length === 0) startScan()
  }, [])

  const togglePort = useCallback(
    (controllerIndex: number, portIndex: number) => {
      const controller = state.usbControllers[controllerIndex]
      if (!controller) return
      const updatedPorts = controller.ports.map((p, i) =>
        i === portIndex ? { ...p, selected: !p.selected } : p
      )
      dispatch({
        type: 'UPDATE_USB_CONTROLLER',
        index: controllerIndex,
        controller: { ...controller, ports: updatedPorts, selectedCount: updatedPorts.filter((p) => p.selected).length },
      })
      invoke('usb:toggle-port', { controller: controllerIndex, port: portIndex })
    },
    [state.usbControllers, dispatch, invoke]
  )

  const setPortType = useCallback(
    (controllerIndex: number, portIndex: number, connectorType: number) => {
      const controller = state.usbControllers[controllerIndex]
      if (!controller) return
      const updatedPorts = controller.ports.map((p, i) =>
        i === portIndex ? { ...p, connectorType } : p
      )
      dispatch({
        type: 'UPDATE_USB_CONTROLLER',
        index: controllerIndex,
        controller: { ...controller, ports: updatedPorts },
      })
      invoke('usb:set-type', { controller: controllerIndex, port: portIndex, type: connectorType })
    },
    [state.usbControllers, dispatch, invoke]
  )

  const totalSelected       = state.usbControllers.reduce((acc, c) => acc + c.ports.filter((p) => p.selected).length, 0)
  const totalPorts          = state.usbControllers.reduce((acc, c) => acc + c.ports.length, 0)
  const anyControllerOver15 = state.usbControllers.some((c) => c.ports.filter((p) => p.selected).length > 15)

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" mb={4}>
        <Box>
          <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
            {t('usb.title')}
          </Text>
          <Text color={TS} fontSize="13px">{t('usb.subtitle')}</Text>
        </Box>
        <HStack gap={2}>
          <Box
            px={3} py="5px" borderRadius="7px" fontSize="12px" fontWeight="700"
            bg={anyControllerOver15 ? 'rgba(239,68,68,0.08)' : 'rgba(45,212,191,0.08)'}
            border={`1px solid ${anyControllerOver15 ? 'rgba(239,68,68,0.2)' : 'rgba(45,212,191,0.2)'}`}
            color={anyControllerOver15 ? '#EF4444' : TEAL}
          >
            {totalSelected}/{totalPorts} {t('usb.portsLabel')}
          </Box>
          {anyControllerOver15 && (
            <HStack gap={1} px={2} py="5px" borderRadius="6px" bg="rgba(239,68,68,0.08)">
              <AlertTriangle size={11} color="#EF4444" />
              <Text fontSize="11px" color="#EF4444" fontWeight="600">{t('usb.maxWarning')}</Text>
            </HStack>
          )}
        </HStack>
      </Flex>

      {/* ── Controls ───────────────────────────────────────────────────── */}
      <Flex gap={3} mb={3} align="center">
        <Box
          as="button" px={3} py="6px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="12px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.07)', color: T }}
          onClick={startScan} opacity={isScanning ? 0.6 : 1}
          pointerEvents={isScanning ? 'none' : 'auto'}
          display="flex" alignItems="center" gap={2} transition="all 0.15s"
        >
          <RefreshCw size={12} className={isScanning ? 'spin' : ''} /> {t('usb.refresh')}
        </Box>
      </Flex>

      {/* ── Tip ────────────────────────────────────────────────────────── */}
      <Flex
        bg="rgba(45,212,191,0.04)" border="1px solid rgba(45,212,191,0.1)"
        borderRadius="9px" px={4} py="8px" align="center" gap={3} mb={4}
      >
        <Info size={13} color={TEAL} />
        <Text fontSize="12px" color={TS}>{t('usb.tip')}</Text>
      </Flex>

      {/* ── Controller list ─────────────────────────────────────────────── */}
      <Box flex={1} overflowY="auto" minH={0}>
        {isScanning && state.usbControllers.length === 0 ? (
          <Flex h="200px" align="center" justify="center" gap={3}
            bg={S} border={`1px solid ${B}`} borderRadius="12px" direction="column"
          >
            <Spinner size="md" color={TEAL} borderWidth="2px" />
            <Text color={TS} fontSize="13px">{t('usb.scanning')}</Text>
          </Flex>
        ) : (
          <Flex direction="column" gap={3}>
            {state.usbControllers.map((controller, ci) => {
              const selectedCount = controller.ports.filter((p) => p.selected).length
              return (
                <Box key={ci} bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
                  <Flex justify="space-between" align="center"
                    px={4} py={3} borderBottom={`1px solid ${B}`}
                  >
                    <HStack gap={2}>
                      <Box px="7px" py="2px" borderRadius="4px"
                        bg="rgba(167,139,250,0.08)" fontSize="10px" fontWeight="600" color="#A78BFA">
                        {controller.type}
                      </Box>
                      <Text color={T} fontWeight="500" fontSize="13px">{controller.name}</Text>
                    </HStack>
                    <Box px="7px" py="2px" borderRadius="4px" fontSize="10px" fontWeight="700"
                      bg={selectedCount > 15 ? 'rgba(239,68,68,0.08)' : 'rgba(45,212,191,0.08)'}
                      color={selectedCount > 15 ? '#EF4444' : TEAL}
                    >
                      {selectedCount}/{controller.ports.length}
                    </Box>
                  </Flex>
                  <Flex direction="column" gap={0} px={3} py={2}>
                    {controller.ports.map((port, pi) => (
                      <PortRow
                        key={pi}
                        port={port}
                        onToggle={() => togglePort(ci, pi)}
                        onTypeChange={(type) => setPortType(ci, pi, type)}
                        speedColors={speedColors}
                        speedLabels={speedLabels}
                      />
                    ))}
                  </Flex>
                </Box>
              )
            })}
          </Flex>
        )}
      </Box>

      {/* ── Navigation ─────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" pt={5} mt={4} borderTop={`1px solid ${B}`}>
        <Box as="button" px={4} py="8px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
          onClick={() => navigate('/smbios')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          <ChevronLeft size={14} /> {t('usb.smbiosNav')}
        </Box>
        <Box as="button" px={5} py="8px" borderRadius="8px"
          bg={totalSelected > 0 ? TEAL : 'rgba(255,255,255,0.06)'}
          color={totalSelected > 0 ? '#0A0A0A' : TS}
          fontSize="13px" fontWeight="700"
          _hover={totalSelected > 0 ? { bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' } : {}}
          onClick={() => totalSelected > 0 && navigate('/build')}
          cursor={totalSelected > 0 ? 'pointer' : 'not-allowed'}
          display="flex" alignItems="center" gap={2}
          opacity={totalSelected > 0 ? 1 : 0.4}
          transition="all 0.15s">
          {t('usb.buildEfi')} <ChevronRight size={14} />
        </Box>
      </Flex>
    </Flex>
  )
}

function PortRow({
  port, onToggle, onTypeChange, speedColors, speedLabels,
}: {
  port: UsbPort
  onToggle: () => void
  onTypeChange: (type: number) => void
  speedColors: Record<string, string>
  speedLabels: Record<string, string>
}) {
  const hasDevice = port.devices.length > 0
  const TEAL = '#2DD4BF'
  const T    = '#F5F5F5'
  const TS   = '#888888'
  const TM   = '#444444'

  return (
    <Flex
      px={3} py="8px" mx={1} my="1px" borderRadius="8px"
      bg={hasDevice ? 'rgba(45,212,191,0.03)' : 'transparent'}
      border={port.selected ? '1px solid rgba(45,212,191,0.15)' : '1px solid transparent'}
      align="center" justify="space-between"
      _hover={{ bg: 'rgba(255,255,255,0.02)' }}
      transition="all 0.12s"
    >
      <HStack gap={3} flex={1} minW={0}>
        <Box
          as="button" w="16px" h="16px" borderRadius="4px" flexShrink={0}
          bg={port.selected ? TEAL : 'transparent'}
          border={port.selected ? 'none' : '1.5px solid rgba(255,255,255,0.1)'}
          display="flex" alignItems="center" justifyContent="center"
          onClick={onToggle} transition="all 0.15s"
        >
          {port.selected && <Check size={9} color="#0A0A0A" strokeWidth={3} />}
        </Box>

        <Flex direction="column" gap={0} minW="70px">
          <Text color={T} fontWeight="500" fontSize="12px">{port.name}</Text>
          <Text fontSize="10px" color={speedColors[port.speedClass]} fontWeight="500">
            {speedLabels[port.speedClass]}
          </Text>
        </Flex>

        <Box flex={1} minW={0}>
          {hasDevice ? (
            port.devices.map((device, i) => (
              <HStack key={i} gap={1}>
                <Zap size={9} color="#22C55E" />
                <Text fontSize="11px" color={T} fontWeight="500" truncate>{cleanDeviceName(device.name)}</Text>
              </HStack>
            ))
          ) : (
            <Text color={TM} fontSize="11px" fontStyle="italic">No device</Text>
          )}
        </Box>
      </HStack>

      <select
        style={{
          padding: '3px 7px',
          background: '#161616',
          border: '1px solid rgba(255,255,255,0.07)',
          borderRadius: '6px',
          color: TS,
          fontSize: '11px',
          width: '125px',
          flexShrink: 0,
          outline: 'none',
        }}
        value={port.connectorType ?? port.guessedType ?? 0}
        onChange={(e) => onTypeChange(Number(e.target.value))}
      >
        {USB_CONNECTOR_TYPES.map((t) => (
          <option key={t.value} value={t.value} style={{ background: '#111111', color: T }}>
            {t.label}
          </option>
        ))}
      </select>
    </Flex>
  )
}
