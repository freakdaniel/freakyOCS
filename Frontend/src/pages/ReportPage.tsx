import { useCallback, useState } from 'react'
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Card,
  Button,
  Badge,
  SimpleGrid,
  Spinner,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { HardwareReport } from '../types'

export function ReportPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isDetecting, setIsDetecting] = useState(false)
  const [isDragOver, setIsDragOver] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Handle hardware detection result
  usePhotinoEvent<HardwareReport>('hardware:detected', (data) => {
    setIsDetecting(false)
    if (data) {
      dispatch({ type: 'SET_REPORT', report: data })
      setError(null)
    }
  })

  usePhotinoEvent<string>('error', (data) => {
    setIsDetecting(false)
    setError(data ?? 'Unknown error occurred')
  })

  const handleDetect = useCallback(() => {
    setIsDetecting(true)
    setError(null)
    invoke('hardware:detect')
  }, [invoke])

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault()
      setIsDragOver(false)

      const file = e.dataTransfer.files[0]
      if (!file || !file.name.endsWith('.json')) {
        setError('Please drop a valid JSON file')
        return
      }

      const reader = new FileReader()
      reader.onload = (event) => {
        try {
          const report = JSON.parse(event.target?.result as string) as HardwareReport
          if (report.cpu && report.gpu && report.motherboard) {
            dispatch({ type: 'SET_REPORT', report })
            setError(null)
          } else {
            setError('Invalid hardware report format')
          }
        } catch {
          setError('Failed to parse JSON file')
        }
      }
      reader.readAsText(file)
    },
    [dispatch]
  )

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragOver(true)
  }, [])

  const handleDragLeave = useCallback(() => {
    setIsDragOver(false)
  }, [])

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack>
            <Text fontSize="2xl">🖥️</Text>
            <Heading size="xl">Hardware Detection</Heading>
          </HStack>
          <Text color="fg.muted">
            Detect your system hardware automatically or load an existing report.
          </Text>
        </VStack>

        {/* Error */}
        {error && (
          <Card.Root bg="red.subtle" borderColor="red.muted">
            <Card.Body>
              <HStack>
                <Text>⚠️</Text>
                <Text color="red.fg">{error}</Text>
              </HStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* Detection Options */}
        <SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
          {/* Auto Detect */}
          <Card.Root
            variant="outline"
            borderWidth={2}
            borderColor={isDetecting ? 'brand.500' : 'border.muted'}
          >
            <Card.Body>
              <VStack gap={4} align="center" py={4}>
                {isDetecting ? (
                  <>
                    <Spinner size="xl" colorPalette="brand" />
                    <Text>Detecting hardware...</Text>
                  </>
                ) : (
                  <>
                    <Text fontSize="4xl">🔍</Text>
                    <Heading size="md">Auto Detect</Heading>
                    <Text color="fg.muted" textAlign="center" fontSize="sm">
                      Scan your system to detect CPU, GPU, audio, network, and more.
                    </Text>
                    <Button
                      colorPalette="brand"
                      onClick={handleDetect}
                      disabled={isDetecting}
                    >
                      Start Detection
                    </Button>
                  </>
                )}
              </VStack>
            </Card.Body>
          </Card.Root>

          {/* Drag & Drop */}
          <Card.Root
            variant="outline"
            borderWidth={2}
            borderStyle="dashed"
            borderColor={isDragOver ? 'brand.500' : 'border.muted'}
            bg={isDragOver ? 'brand.subtle' : 'transparent'}
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
          >
            <Card.Body>
              <VStack gap={4} align="center" py={4}>
                <Text fontSize="4xl">📄</Text>
                <Heading size="md">Load Report</Heading>
                <Text color="fg.muted" textAlign="center" fontSize="sm">
                  Drag & drop a hardware report JSON file here.
                </Text>
                <Badge colorPalette="gray">JSON only</Badge>
              </VStack>
            </Card.Body>
          </Card.Root>
        </SimpleGrid>

        {/* Current Report */}
        {state.report && (
          <Card.Root>
            <Card.Header>
              <HStack justify="space-between">
                <Heading size="md">Hardware Report</Heading>
                <Badge colorPalette="green">Loaded</Badge>
              </HStack>
            </Card.Header>
            <Card.Body>
              <SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
                {/* CPU */}
                <HStack>
                  <Text fontSize="xl">💻</Text>
                  <Box>
                    <Text fontWeight="semibold">{state.report.cpu.name}</Text>
                    <Text color="fg.muted" fontSize="sm">
                      {state.report.cpu.codename} • {state.report.cpu.cores}C/{state.report.cpu.threads}T
                    </Text>
                  </Box>
                </HStack>

                {/* GPUs */}
                {state.report.gpu.map((gpu, i) => (
                  <HStack key={i}>
                    <Text fontSize="xl">{gpu.discrete ? '🎮' : '🖼️'}</Text>
                    <Box>
                      <Text fontWeight="semibold">{gpu.name}</Text>
                      <Text color="fg.muted" fontSize="sm">
                        {gpu.codename ?? gpu.vendor}
                        {gpu.vram && ` • ${Math.round(gpu.vram / 1024)}GB`}
                      </Text>
                    </Box>
                  </HStack>
                ))}

                {/* Motherboard */}
                <HStack>
                  <Text fontSize="xl">🔲</Text>
                  <Box>
                    <Text fontWeight="semibold">{state.report.motherboard.model}</Text>
                    <Text color="fg.muted" fontSize="sm">
                      {state.report.motherboard.manufacturer}
                      {state.report.motherboard.chipset && ` • ${state.report.motherboard.chipset}`}
                    </Text>
                  </Box>
                </HStack>

                {/* Audio */}
                {state.report.audio.length > 0 && (
                  <HStack>
                    <Text fontSize="xl">🔊</Text>
                    <Box>
                      <Text fontWeight="semibold">{state.report.audio[0].name}</Text>
                      <Text color="fg.muted" fontSize="sm">
                        Codec ID: {state.report.audio[0].codecId}
                      </Text>
                    </Box>
                  </HStack>
                )}
              </SimpleGrid>
            </Card.Body>
            <Card.Footer>
              <HStack justify="end" w="full">
                <Button variant="outline" onClick={handleDetect}>
                  Re-scan
                </Button>
                <Button colorPalette="brand" onClick={() => navigate('/compatibility')}>
                  Check Compatibility →
                </Button>
              </HStack>
            </Card.Footer>
          </Card.Root>
        )}
      </VStack>
    </Box>
  )
}
