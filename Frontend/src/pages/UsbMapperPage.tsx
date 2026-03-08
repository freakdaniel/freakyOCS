import { useEffect, useCallback, useState } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex, Spinner } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { UsbController, UsbPort } from '../types'
import { USB_CONNECTOR_TYPES } from '../types'
import { Usb, RefreshCw, ChevronLeft, ChevronRight, Check, AlertTriangle, Zap, Info, MousePointer2 } from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'

const speedColors: Record<string, string> = {
  SuperSpeedPlus: '#A78BFA',
  SuperSpeed:     '#38BDF8',
  HighSpeed:      '#22C55E',
  FullSpeed:      '#EAB308',
  LowSpeed:       '#f97316',
  Unknown:        '#475569',
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
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isScanning, setIsScanning] = useState(false)
  const [autoRefresh, setAutoRefresh] = useState(false)

  usePhotinoEvent<UsbController[]>('usb:controllers', (data) => {
    setIsScanning(false)
    if (data) dispatch({ type: 'SET_USB_CONTROLLERS', controllers: data })
  })

  usePhotinoEvent<UsbController[]>('usb:updated', (data) => {
    if (data) dispatch({ type: 'SET_USB_CONTROLLERS', controllers: data })
  })

  const startScan = useCallback(() => {
    setIsScanning(true)
    invoke('usb:scan')
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

  const selectAllPopulated = useCallback(() => {
    state.usbControllers.forEach((controller, ci) => {
      const updatedPorts = controller.ports.map((p) => ({ ...p, selected: p.devices.length > 0 }))
      dispatch({
        type: 'UPDATE_USB_CONTROLLER',
        index: ci,
        controller: { ...controller, ports: updatedPorts, selectedCount: updatedPorts.filter((p) => p.selected).length },
      })
    })
  }, [state.usbControllers, dispatch])

  const totalSelected = state.usbControllers.reduce((acc, c) => acc + c.ports.filter((p) => p.selected).length, 0)
  const totalPorts    = state.usbControllers.reduce((acc, c) => acc + c.ports.length, 0)

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Flex justify="space-between" align="start">
          <Box>
            <HStack gap={3} mb={1.5}>
              <Flex w="34px" h="34px" borderRadius="8px" bg={`${AD}0.12)`} align="center" justify="center">
                <Usb size={17} color={A} />
              </Flex>
              <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">USB Mapper</Heading>
            </HStack>
            <Text color={TS} fontSize="sm">Plug devices into each port to detect them. Enable ports you want to use.</Text>
          </Box>
          <HStack gap={2}>
            <Box
              px={3} py={1} borderRadius="7px" fontSize="sm" fontWeight="600"
              bg={totalSelected > 15 ? 'rgba(239,68,68,0.1)' : `${AD}0.1)`}
              color={totalSelected > 15 ? '#EF4444' : A}
            >
              {totalSelected}/{totalPorts} ports
            </Box>
            {totalSelected > 15 && (
              <Flex px={2} py={1} borderRadius="6px" bg="rgba(239,68,68,0.1)" align="center" gap={1}>
                <AlertTriangle size={12} color="#EF4444" />
                <Text fontSize="xs" color="#EF4444" fontWeight="600">Max 15!</Text>
              </Flex>
            )}
          </HStack>
        </Flex>

        {/* Controls */}
        <Flex gap={3} flexWrap="wrap" align="center" justify="space-between">
          <HStack gap={2}>
            <Box
              as="button" px={3} py="7px" borderRadius="8px"
              bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
              _hover={{ bg: 'rgba(255,255,255,0.07)' }}
              onClick={startScan} opacity={isScanning ? 0.6 : 1}
              display="flex" alignItems="center" gap={2}
            >
              <RefreshCw size={13} className={isScanning ? 'spin' : ''} /> Refresh
            </Box>
            <Box
              as="button" px={3} py="7px" borderRadius="8px"
              bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
              _hover={{ bg: 'rgba(255,255,255,0.07)' }}
              onClick={selectAllPopulated} display="flex" alignItems="center" gap={2}
            >
              <MousePointer2 size={13} /> Select Populated
            </Box>
          </HStack>
          <Box
            as="button" px={3} py="7px" borderRadius="8px"
            bg={autoRefresh ? 'rgba(34,197,94,0.08)' : 'rgba(255,255,255,0.03)'}
            border={autoRefresh ? '1px solid rgba(34,197,94,0.2)' : `1px solid ${B}`}
            color={autoRefresh ? '#22C55E' : TS}
            fontSize="xs" fontWeight="600"
            onClick={() => setAutoRefresh(!autoRefresh)}
            display="flex" alignItems="center" gap={1.5}
          >
            <Zap size={11} /> Auto-refresh (3s)
          </Box>
        </Flex>

        {/* Tip */}
        <Flex bg="rgba(56,189,248,0.05)" border="1px solid rgba(56,189,248,0.1)" borderRadius="10px" px={4} py={3} align="center" gap={3}>
          <Info size={15} color="#38BDF8" />
          <Text fontSize="xs" color={TS}>
            All USB ports are detected automatically. Select up to{' '}
            <Text as="span" color="#EF4444" fontWeight="600">15 ports</Text>{' '}
            per controller. Plug devices in briefly to identify physical port locations.
          </Text>
        </Flex>

        {isScanning && state.usbControllers.length === 0 ? (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
            <VStack gap={4}><Spinner size="xl" color={A} /><Text color={TS} fontSize="sm">Scanning USB controllers…</Text></VStack>
          </Box>
        ) : (
          state.usbControllers.map((controller, ci) => {
            const selectedCount = controller.ports.filter((p) => p.selected).length
            return (
              <Box key={ci} bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
                <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
                  <HStack gap={3}>
                    <Box px={2} py={0.5} borderRadius="5px" bg="rgba(167,139,250,0.1)" fontSize="xs" fontWeight="600" color="#A78BFA">
                      {controller.type}
                    </Box>
                    <Text color={T} fontWeight="500" fontSize="sm">{controller.name}</Text>
                  </HStack>
                  <Box
                    px={2} py={0.5} borderRadius="5px" fontSize="xs" fontWeight="600"
                    bg={selectedCount > 15 ? 'rgba(239,68,68,0.1)' : 'rgba(34,197,94,0.1)'}
                    color={selectedCount > 15 ? '#EF4444' : '#22C55E'}
                  >
                    {selectedCount}/{controller.ports.length}
                  </Box>
                </Flex>
                <VStack align="stretch" gap={0} px={3} py={2}>
                  {controller.ports.map((port, pi) => (
                    <PortRow
                      key={pi}
                      port={port}
                      onToggle={() => togglePort(ci, pi)}
                      onTypeChange={(type) => setPortType(ci, pi, type)}
                      accentColor={A}
                      speedColors={speedColors}
                      speedLabels={speedLabels}
                    />
                  ))}
                </VStack>
              </Box>
            )
          })
        )}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box
            as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.07)', color: T }}
            onClick={() => navigate('/smbios')} display="flex" alignItems="center" gap={2}
          >
            <ChevronLeft size={14} /> SMBIOS
          </Box>
          <Box
            as="button" px={4} py="8px" borderRadius="8px"
            bg={totalSelected > 0 ? A : `${AD}0.3)`}
            color="white" fontSize="sm" fontWeight="600"
            _hover={totalSelected > 0 ? { bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' } : {}}
            onClick={() => totalSelected > 0 && navigate('/build')}
            cursor={totalSelected > 0 ? 'pointer' : 'not-allowed'}
            display="flex" alignItems="center" gap={2}
          >
            Build EFI <ChevronRight size={14} />
          </Box>
        </Flex>
      </VStack>
    </Box>
  )
}

function PortRow({
  port, onToggle, onTypeChange, accentColor, speedColors, speedLabels,
}: {
  port: UsbPort
  onToggle: () => void
  onTypeChange: (type: number) => void
  accentColor: string
  speedColors: Record<string, string>
  speedLabels: Record<string, string>
}) {
  const hasDevice = port.devices.length > 0

  return (
    <Flex
      px={3} py={2.5} mx={1} my={0.5} borderRadius="8px"
      bg={hasDevice ? 'rgba(34,197,94,0.03)' : 'transparent'}
      border={port.selected ? `1px solid rgba(123,127,255,0.18)` : '1px solid transparent'}
      align="center" justify="space-between"
      _hover={{ bg: 'rgba(255,255,255,0.02)' }}
      transition="all 0.1s"
    >
      <HStack gap={3} flex={1} minW={0}>
        <Box
          as="button" w="17px" h="17px" borderRadius="4px" flexShrink={0}
          bg={port.selected ? accentColor : 'transparent'}
          border={port.selected ? 'none' : '2px solid rgba(255,255,255,0.12)'}
          display="flex" alignItems="center" justifyContent="center"
          onClick={onToggle}
        >
          {port.selected && <Check size={10} color="white" strokeWidth={3} />}
        </Box>

        <VStack align="start" gap={0} minW="70px">
          <Text color="#EDF0FF" fontWeight="500" fontSize="sm">{port.name}</Text>
          <Text fontSize="xs" color={speedColors[port.speedClass]} fontWeight="500">
            {speedLabels[port.speedClass]}
          </Text>
        </VStack>

        <Box flex={1} minW={0}>
          {hasDevice ? (
            port.devices.map((device, i) => (
              <HStack key={i} gap={1}>
                <Zap size={10} color="#22C55E" />
                <Text fontSize="xs" color="#EDF0FF" fontWeight="500" truncate>{device.name}</Text>
              </HStack>
            ))
          ) : (
            <Text color="#363B52" fontSize="xs" fontStyle="italic">No device</Text>
          )}
        </Box>
      </HStack>

      <select
        style={{
          padding: '4px 8px',
          background: '#0D0D1C',
          border: '1px solid rgba(255,255,255,0.07)',
          borderRadius: '6px',
          color: '#7A829E',
          fontSize: '12px',
          width: '130px',
          flexShrink: 0,
          outline: 'none',
        }}
        value={port.connectorType ?? port.guessedType ?? 0}
        onChange={(e) => onTypeChange(Number(e.target.value))}
      >
        {USB_CONNECTOR_TYPES.map((t) => (
          <option key={t.value} value={t.value} style={{ background: '#0D0D1C', color: '#EDF0FF' }}>
            {t.label}
          </option>
        ))}
      </select>
    </Flex>
  )
}

