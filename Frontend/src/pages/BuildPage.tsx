import { useCallback, useState, useEffect } from 'react'
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Card,
  Button,
  Badge,
  Progress,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { BuildProgress, BuildResult, BuildStage } from '../types'

const stageLabels: Record<BuildStage, string> = {
  idle: 'Ready to build',
  'downloading-opencore': 'Downloading OpenCore...',
  'downloading-kexts': 'Downloading kexts...',
  'generating-acpi': 'Generating ACPI tables...',
  'generating-config': 'Generating config.plist...',
  'generating-usb-map': 'Generating USB map...',
  packaging: 'Packaging EFI folder...',
  complete: 'Build complete!',
  error: 'Build failed',
}

const stageIcons: Record<BuildStage, string> = {
  idle: '⏸️',
  'downloading-opencore': '⬇️',
  'downloading-kexts': '📦',
  'generating-acpi': '⚙️',
  'generating-config': '📝',
  'generating-usb-map': '🔌',
  packaging: '📁',
  complete: '✅',
  error: '❌',
}

export function BuildPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isBuilding, setIsBuilding] = useState(false)

  usePhotinoEvent<BuildProgress>('build:progress', (data) => {
    if (data) {
      dispatch({ type: 'SET_BUILD_PROGRESS', progress: data })
    }
  })

  usePhotinoEvent<BuildResult>('build:complete', (data) => {
    setIsBuilding(false)
    if (data) {
      dispatch({ type: 'SET_BUILD_RESULT', result: data })
      navigate('/result')
    }
  })

  usePhotinoEvent<string>('build:error', (data) => {
    setIsBuilding(false)
    dispatch({
      type: 'SET_BUILD_PROGRESS',
      progress: {
        stage: 'error',
        progress: 0,
        message: data ?? 'Unknown error',
        log: state.buildProgress?.log ?? [],
      },
    })
  })

  const startBuild = useCallback(() => {
    setIsBuilding(true)
    dispatch({
      type: 'SET_BUILD_PROGRESS',
      progress: { stage: 'downloading-opencore', progress: 0, message: 'Starting...', log: [] },
    })

    invoke('build:start', {
      report: state.report,
      macos: state.selectedMacOS,
      acpiPatches: state.acpiPatches.filter((p) => p.enabled),
      kexts: state.kexts.filter((k) => k.enabled),
      smbios: state.smbios,
      usbControllers: state.usbControllers,
    })
  }, [invoke, state, dispatch])

  const cancelBuild = useCallback(() => {
    invoke('build:cancel')
    setIsBuilding(false)
  }, [invoke])

  // Mock build progress for development
  useEffect(() => {
    if (!isBuilding || !state.buildProgress) return

    const stages: BuildStage[] = [
      'downloading-opencore',
      'downloading-kexts',
      'generating-acpi',
      'generating-config',
      'generating-usb-map',
      'packaging',
      'complete',
    ]

    const currentIndex = stages.indexOf(state.buildProgress.stage)
    if (currentIndex === -1 || state.buildProgress.stage === 'complete') return

    const timer = setTimeout(() => {
      const nextStage = stages[currentIndex + 1]
      const newProgress = {
        stage: nextStage,
        progress: ((currentIndex + 2) / stages.length) * 100,
        message: stageLabels[nextStage],
        log: [
          ...state.buildProgress!.log,
          `[${new Date().toLocaleTimeString()}] ${stageLabels[nextStage]}`,
        ],
      }
      dispatch({ type: 'SET_BUILD_PROGRESS', progress: newProgress })

      if (nextStage === 'complete') {
        setIsBuilding(false)
        dispatch({
          type: 'SET_BUILD_RESULT',
          result: {
            success: true,
            outputPath: 'D:\\EFI',
            biosSettings: [
              { name: 'VT-d', recommended: 'Disabled', category: 'CPU', required: true },
              { name: 'CFG Lock', recommended: 'Disabled', category: 'CPU', required: true },
              { name: 'Secure Boot', recommended: 'Disabled', category: 'Boot', required: true },
              { name: 'Fast Boot', recommended: 'Disabled', category: 'Boot', required: false },
              { name: 'XHCI Hand-off', recommended: 'Enabled', category: 'USB', required: true },
            ],
            nextSteps: [
              'Copy EFI folder to your USB installer',
              'Configure BIOS settings as shown',
              'Boot from USB and install macOS',
              'After install, copy EFI to internal drive',
            ],
          },
        })
        navigate('/result')
      }
    }, 1500)

    return () => clearTimeout(timer)
  }, [isBuilding, state.buildProgress, dispatch, navigate])

  const canBuild = state.report && state.selectedMacOS && state.smbios

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack>
            <Text fontSize="2xl">🔨</Text>
            <Heading size="xl">Build EFI</Heading>
          </HStack>
          <Text color="fg.muted">
            Generate your OpenCore EFI folder with all configured options.
          </Text>
        </VStack>

        {/* Pre-build Summary */}
        {!isBuilding && !state.buildProgress && (
          <Card.Root>
            <Card.Header>
              <Heading size="md">Build Summary</Heading>
            </Card.Header>
            <Card.Body>
              <VStack align="stretch" gap={3}>
                <HStack justify="space-between">
                  <Text color="fg.muted">Hardware Report</Text>
                  <Badge colorPalette={state.report ? 'green' : 'red'}>
                    {state.report ? '✅ Loaded' : '❌ Missing'}
                  </Badge>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">macOS Version</Text>
                  <Badge colorPalette={state.selectedMacOS ? 'green' : 'red'}>
                    {state.selectedMacOS?.name ?? '❌ Not selected'}
                  </Badge>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">ACPI Patches</Text>
                  <Badge colorPalette="blue">
                    {state.acpiPatches.filter((p) => p.enabled).length} enabled
                  </Badge>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">Kexts</Text>
                  <Badge colorPalette="purple">
                    {state.kexts.filter((k) => k.enabled).length} enabled
                  </Badge>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">SMBIOS</Text>
                  <Badge colorPalette={state.smbios ? 'green' : 'red'}>
                    {state.smbios?.model ?? '❌ Not generated'}
                  </Badge>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">USB Ports</Text>
                  <Badge colorPalette="orange">
                    {state.usbControllers.reduce(
                      (acc, c) => acc + c.ports.filter((p) => p.selected).length,
                      0
                    )}{' '}
                    mapped
                  </Badge>
                </HStack>
              </VStack>
            </Card.Body>
            <Card.Footer>
              <Button
                colorPalette="brand"
                size="lg"
                w="full"
                onClick={startBuild}
                disabled={!canBuild}
              >
                🚀 Start Build
              </Button>
            </Card.Footer>
          </Card.Root>
        )}

        {/* Build Progress */}
        {(isBuilding || state.buildProgress) && state.buildProgress?.stage !== 'error' && (
          <Card.Root>
            <Card.Header>
              <HStack justify="space-between">
                <HStack>
                  <Text fontSize="xl">{stageIcons[state.buildProgress?.stage ?? 'idle']}</Text>
                  <Heading size="md">{stageLabels[state.buildProgress?.stage ?? 'idle']}</Heading>
                </HStack>
                {isBuilding && (
                  <Button size="sm" colorPalette="red" variant="outline" onClick={cancelBuild}>
                    Cancel
                  </Button>
                )}
              </HStack>
            </Card.Header>
            <Card.Body>
              <VStack align="stretch" gap={4}>
                <Progress.Root value={state.buildProgress?.progress ?? 0}>
                  <Progress.Track>
                    <Progress.Range />
                  </Progress.Track>
                </Progress.Root>
                <Text color="fg.muted" textAlign="center">
                  {state.buildProgress?.message}
                </Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* Build Log */}
        {state.buildProgress && state.buildProgress.log.length > 0 && (
          <Card.Root>
            <Card.Header>
              <Heading size="sm">Build Log</Heading>
            </Card.Header>
            <Card.Body p={0}>
              <Box
                bg="gray.900"
                color="gray.100"
                p={4}
                borderRadius="md"
                fontFamily="mono"
                fontSize="sm"
                maxH="300px"
                overflowY="auto"
              >
                {state.buildProgress.log.map((line, i) => (
                  <Text key={i}>{line}</Text>
                ))}
              </Box>
            </Card.Body>
          </Card.Root>
        )}

        {/* Error */}
        {state.buildProgress?.stage === 'error' && (
          <Card.Root bg="red.subtle">
            <Card.Body>
              <VStack gap={4}>
                <Text fontSize="3xl">❌</Text>
                <Heading size="md" color="red.fg">
                  Build Failed
                </Heading>
                <Text>{state.buildProgress.message}</Text>
                <Button colorPalette="red" onClick={startBuild}>
                  Retry
                </Button>
              </VStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/usb')} disabled={isBuilding}>
            ← USB Mapper
          </Button>
          {state.buildResult && (
            <Button colorPalette="brand" onClick={() => navigate('/result')}>
              View Result →
            </Button>
          )}
        </HStack>
      </VStack>
    </Box>
  )
}
