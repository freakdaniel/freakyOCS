import { useEffect, useCallback, useState } from 'react'
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Card,
  Button,
  Badge,
  Spinner,
  Checkbox,
  Input,
  NativeSelect,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { UsbController, UsbPort } from '../types'
import { USB_CONNECTOR_TYPES } from '../types'

const speedColors: Record<string, string> = {
  SuperSpeedPlus: 'purple',
  SuperSpeed: 'blue',
  HighSpeed: 'green',
  FullSpeed: 'yellow',
  LowSpeed: 'orange',
  Unknown: 'gray',
}

const speedLabels: Record<string, string> = {
  SuperSpeedPlus: 'SS+ (10Gbps)',
  SuperSpeed: 'SS (5Gbps)',
  HighSpeed: 'HS (480Mbps)',
  FullSpeed: 'FS (12Mbps)',
  LowSpeed: 'LS (1.5Mbps)',
  Unknown: 'Unknown',
}

export function UsbMapperPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isScanning, setIsScanning] = useState(false)
  const [autoRefresh, setAutoRefresh] = useState(true)

  usePhotinoEvent<UsbController[]>('usb:controllers', (data) => {
    setIsScanning(false)
    if (data) {
      dispatch({ type: 'SET_USB_CONTROLLERS', controllers: data })
    }
  })

  usePhotinoEvent<UsbController[]>('usb:updated', (data) => {
    if (data) {
      dispatch({ type: 'SET_USB_CONTROLLERS', controllers: data })
    }
  })

  const startScan = useCallback(() => {
    setIsScanning(true)
    invoke('usb:scan')
  }, [invoke])

  useEffect(() => {
    if (state.usbControllers.length === 0) {
      startScan()
    }
  }, [])

  // Auto-refresh polling
  useEffect(() => {
    if (!autoRefresh) return
    const interval = setInterval(() => {
      invoke('usb:scan')
    }, 3000)
    return () => clearInterval(interval)
  }, [autoRefresh, invoke])

  // Mock data for development
  useEffect(() => {
    if (state.usbControllers.length > 0) return
    const mockControllers: UsbController[] = [
      {
        name: 'Intel USB 3.1 xHCI Host Controller',
        type: 'XHCI',
        selectedCount: 0,
        ports: [
          { name: 'HS01', index: 1, speedClass: 'HighSpeed', devices: [], selected: false, guessedType: 0 },
          { name: 'HS02', index: 2, speedClass: 'HighSpeed', devices: [{ name: 'USB Keyboard' }], selected: true, guessedType: 0 },
          { name: 'HS03', index: 3, speedClass: 'HighSpeed', devices: [], selected: false, guessedType: 0 },
          { name: 'HS04', index: 4, speedClass: 'HighSpeed', devices: [{ name: 'USB Mouse' }], selected: true, guessedType: 0 },
          { name: 'SS01', index: 5, speedClass: 'SuperSpeed', devices: [], selected: false, guessedType: 3 },
          { name: 'SS02', index: 6, speedClass: 'SuperSpeed', devices: [{ name: 'USB 3.0 Flash Drive' }], selected: true, guessedType: 3 },
          { name: 'SS03', index: 7, speedClass: 'SuperSpeed', devices: [], selected: false, guessedType: 3 },
          { name: 'SS04', index: 8, speedClass: 'SuperSpeed', devices: [], selected: false, guessedType: 3 },
        ],
      },
    ]
    setTimeout(() => {
      dispatch({ type: 'SET_USB_CONTROLLERS', controllers: mockControllers })
      setIsScanning(false)
    }, 500)
  }, [dispatch, state.usbControllers.length])

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
        controller: {
          ...controller,
          ports: updatedPorts,
          selectedCount: updatedPorts.filter((p) => p.selected).length,
        },
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
      const updatedPorts = controller.ports.map((p) => ({
        ...p,
        selected: p.devices.length > 0,
      }))
      dispatch({
        type: 'UPDATE_USB_CONTROLLER',
        index: ci,
        controller: {
          ...controller,
          ports: updatedPorts,
          selectedCount: updatedPorts.filter((p) => p.selected).length,
        },
      })
    })
  }, [state.usbControllers, dispatch])

  const totalSelected = state.usbControllers.reduce(
    (acc, c) => acc + c.ports.filter((p) => p.selected).length,
    0
  )

  const totalPorts = state.usbControllers.reduce((acc, c) => acc + c.ports.length, 0)

  return (
    <Box maxW="5xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack justify="space-between" w="full">
            <HStack>
              <Text fontSize="2xl">🔌</Text>
              <Heading size="xl">USB Mapper</Heading>
            </HStack>
            <HStack>
              <Badge colorPalette={totalSelected > 15 ? 'red' : 'brand'} size="lg">
                {totalSelected}/{totalPorts} ports
              </Badge>
              {totalSelected > 15 && (
                <Badge colorPalette="red">Max 15 per controller!</Badge>
              )}
            </HStack>
          </HStack>
          <Text color="fg.muted">
            Plug USB devices into each port to detect them. Enable ports you want to use.
          </Text>
        </VStack>

        {/* Controls */}
        <HStack justify="space-between" wrap="wrap" gap={2}>
          <HStack>
            <Button
              variant="outline"
              onClick={startScan}
              loading={isScanning}
              loadingText="Scanning..."
            >
              🔄 Refresh
            </Button>
            <Button variant="outline" onClick={selectAllPopulated}>
              ✅ Select Populated
            </Button>
          </HStack>
          <HStack>
            <Checkbox.Root
              checked={autoRefresh}
              onCheckedChange={(e) => setAutoRefresh(!!e.checked)}
            >
              <Checkbox.HiddenInput />
              <Checkbox.Control />
              <Checkbox.Label>Auto-refresh (3s)</Checkbox.Label>
            </Checkbox.Root>
          </HStack>
        </HStack>

        {/* Tip */}
        <Card.Root variant="subtle" bg="blue.subtle">
          <Card.Body py={3}>
            <HStack>
              <Text>💡</Text>
              <Text fontSize="sm">
                Plug a USB device into each port you want to map. The device will appear next to the port.
                macOS has a 15-port limit per controller!
              </Text>
            </HStack>
          </Card.Body>
        </Card.Root>

        {isScanning && state.usbControllers.length === 0 ? (
          <Card.Root>
            <Card.Body>
              <VStack gap={4} py={8}>
                <Spinner size="xl" colorPalette="brand" />
                <Text>Scanning USB controllers...</Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        ) : (
          state.usbControllers.map((controller, ci) => (
            <Card.Root key={ci}>
              <Card.Header>
                <HStack justify="space-between">
                  <HStack>
                    <Badge colorPalette="purple">{controller.type}</Badge>
                    <Heading size="md">{controller.name}</Heading>
                  </HStack>
                  <Badge
                    colorPalette={
                      controller.ports.filter((p) => p.selected).length > 15
                        ? 'red'
                        : 'green'
                    }
                  >
                    {controller.ports.filter((p) => p.selected).length}/{controller.ports.length}
                  </Badge>
                </HStack>
              </Card.Header>
              <Card.Body pt={0}>
                <VStack align="stretch" gap={2}>
                  {controller.ports.map((port, pi) => (
                    <PortRow
                      key={pi}
                      port={port}
                      onToggle={() => togglePort(ci, pi)}
                      onTypeChange={(type) => setPortType(ci, pi, type)}
                    />
                  ))}
                </VStack>
              </Card.Body>
            </Card.Root>
          ))
        )}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/smbios')}>
            ← SMBIOS
          </Button>
          <Button
            colorPalette="brand"
            onClick={() => navigate('/build')}
            disabled={totalSelected === 0}
          >
            Build EFI →
          </Button>
        </HStack>
      </VStack>
    </Box>
  )
}

function PortRow({
  port,
  onToggle,
  onTypeChange,
}: {
  port: UsbPort
  onToggle: () => void
  onTypeChange: (type: number) => void
}) {
  const hasDevice = port.devices.length > 0

  return (
    <HStack
      p={3}
      bg={hasDevice ? 'green.subtle' : 'bg.subtle'}
      borderRadius="md"
      borderWidth={port.selected ? 2 : 1}
      borderColor={port.selected ? 'brand.500' : 'border.muted'}
      justify="space-between"
    >
      <HStack gap={3} flex={1}>
        <Checkbox.Root checked={port.selected} onCheckedChange={onToggle}>
          <Checkbox.HiddenInput />
          <Checkbox.Control />
        </Checkbox.Root>
        <VStack align="start" gap={0} minW="80px">
          <Text fontWeight="semibold">{port.name}</Text>
          <Badge colorPalette={speedColors[port.speedClass]} size="sm">
            {speedLabels[port.speedClass]}
          </Badge>
        </VStack>
        <Box flex={1}>
          {hasDevice ? (
            <VStack align="start" gap={0}>
              {port.devices.map((device, i) => (
                <HStack key={i}>
                  <Text>🔗</Text>
                  <Text fontSize="sm" fontWeight="medium">
                    {device.name}
                  </Text>
                </HStack>
              ))}
            </VStack>
          ) : (
            <Text color="fg.subtle" fontSize="sm" fontStyle="italic">
              No device connected
            </Text>
          )}
        </Box>
      </HStack>
      <HStack>
        <NativeSelect.Root size="sm" w="150px">
          <NativeSelect.Field
            value={port.connectorType ?? port.guessedType ?? 0}
            onChange={(e) => onTypeChange(Number(e.target.value))}
          >
            {USB_CONNECTOR_TYPES.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </NativeSelect.Field>
        </NativeSelect.Root>
        <Input
          size="sm"
          w="120px"
          placeholder="Comment"
          value={port.comment ?? ''}
          onChange={() => {}}
        />
      </HStack>
    </HStack>
  )
}
