import { useCallback, useState } from 'react'
import {
  Box, Heading, Text, VStack, HStack, Flex, SimpleGrid, Spinner,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { HardwareReport } from '../types'
import {
  Search, FileJson, Cpu, MonitorSmartphone, CircuitBoard,
  Volume2, AlertTriangle, ChevronRight, RefreshCw,
} from 'lucide-react'

// ── Shared tokens ─────────────────────────────────────────────────
const S  = '#0D0D1C'             // surface
const B  = 'rgba(255,255,255,0.07)' // border
//const BH = 'rgba(255,255,255,0.13)' // border hover
const A  = '#7B7FFF'             // accent
const AD = 'rgba(123,127,255,'   // accent dim prefix
const T  = '#EDF0FF'             // text primary
const TS = '#7A829E'             // text secondary
const TM = '#363B52'             // text muted

export function ReportPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isDetecting, setIsDetecting] = useState(false)
  const [isDragOver, setIsDragOver] = useState(false)
  const [error, setError] = useState<string | null>(null)

  usePhotinoEvent<HardwareReport>('hardware:detected', (data) => {
    setIsDetecting(false)
    if (data) { dispatch({ type: 'SET_REPORT', report: data }); setError(null) }
  })

  usePhotinoEvent<string>('error', (data) => {
    setIsDetecting(false)
    setError(data ?? 'Unknown error occurred')
  })

  const handleDetect = useCallback(() => {
    setIsDetecting(true); setError(null)
    invoke('hardware:detect').catch((err: unknown) => {
      setIsDetecting(false)
      setError(err instanceof Error ? err.message : 'Detection failed')
    })
  }, [invoke])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault(); setIsDragOver(false)
    const file = e.dataTransfer.files[0]
    if (!file || !file.name.endsWith('.json')) { setError('Please drop a valid JSON file'); return }
    const reader = new FileReader()
    reader.onload = (event) => {
      try {
        const report = JSON.parse(event.target?.result as string) as HardwareReport
        if (report.cpu && report.gpu && report.motherboard) {
          dispatch({ type: 'SET_REPORT', report }); setError(null)
        } else { setError('Invalid hardware report format') }
      } catch { setError('Failed to parse JSON file') }
    }
    reader.readAsText(file)
  }, [dispatch])

  const handleDragOver  = useCallback((e: React.DragEvent) => { e.preventDefault(); setIsDragOver(true) }, [])
  const handleDragLeave = useCallback(() => setIsDragOver(false), [])

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">

        {/* Header */}
        <Box>
          <HStack gap={3} mb={1.5}>
            <Flex w="34px" h="34px" borderRadius="8px" bg={`${AD}0.12)`} align="center" justify="center">
              <Cpu size={17} color={A} />
            </Flex>
            <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">
              Hardware Detection
            </Heading>
          </HStack>
          <Text color={TS} fontSize="sm">
            Detect your system hardware automatically or load an existing report.
          </Text>
        </Box>

        {/* Error */}
        {error && (
          <HStack
            gap={3} p={4} borderRadius="10px"
            bg="rgba(239,68,68,0.07)" border="1px solid rgba(239,68,68,0.18)"
          >
            <AlertTriangle size={16} color="#EF4444" />
            <Text color="#EF4444" fontSize="sm">{error}</Text>
          </HStack>
        )}

        {/* Detection Options */}
        <SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
          {/* Auto Detect */}
          <Box
            bg={isDetecting ? `${AD}0.07)` : S}
            border={isDetecting ? `1px solid ${AD}0.28)` : `1px solid ${B}`}
            borderRadius="12px" p={6}
            cursor={isDetecting ? 'default' : 'pointer'}
            onClick={!isDetecting ? handleDetect : undefined}
            _hover={!isDetecting ? { borderColor: `${AD}0.28)`, bg: '#0F0F22' } : {}}
            transition="all 0.2s ease"
          >
            <VStack gap={4} align="center" py={3}>
              {isDetecting ? (
                <><Spinner size="xl" color={A} /><Text color={TS} fontSize="sm">Detecting hardware…</Text></>
              ) : (
                <>
                  <Flex w="48px" h="48px" borderRadius="12px" bg={`${AD}0.12)`} align="center" justify="center">
                    <Search size={22} color={A} />
                  </Flex>
                  <Text color={T} fontWeight="600" fontSize="sm">Auto Detect</Text>
                  <Text color={TS} textAlign="center" fontSize="xs" lineHeight="1.6">
                    Scan your system to detect CPU, GPU, audio, network, and more.
                  </Text>
                </>
              )}
            </VStack>
          </Box>

          {/* Drag & Drop */}
          <Box
            bg={isDragOver ? `${AD}0.07)` : S}
            border={`2px dashed ${isDragOver ? `${AD}0.4)` : B}`}
            borderRadius="12px" p={6}
            onDrop={handleDrop} onDragOver={handleDragOver} onDragLeave={handleDragLeave}
            transition="all 0.2s ease"
          >
            <VStack gap={4} align="center" py={3}>
              <Flex w="48px" h="48px" borderRadius="12px" bg="rgba(255,255,255,0.04)" align="center" justify="center">
                <FileJson size={22} color={TS} />
              </Flex>
              <Text color={T} fontWeight="600" fontSize="sm">Load Report</Text>
              <Text color={TS} textAlign="center" fontSize="xs" lineHeight="1.6">
                Drag & drop a hardware report JSON file here.
              </Text>
              <Box px={2.5} py={1} borderRadius="5px" bg="rgba(255,255,255,0.04)"
                fontSize="10px" color={TM} fontWeight="600" letterSpacing="0.04em">
                JSON only
              </Box>
            </VStack>
          </Box>
        </SimpleGrid>

        {/* Current Report */}
        {state.report && (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Flex justify="space-between" align="center" px={5} py={3.5}
              borderBottom={`1px solid ${B}`}
            >
              <Text color={T} fontWeight="600" fontSize="sm">Hardware Report</Text>
              <Box px={2.5} py={1} borderRadius="5px"
                bg="rgba(34,197,94,0.1)" fontSize="xs" fontWeight="600" color="#22C55E"
              >
                Loaded
              </Box>
            </Flex>

            <SimpleGrid columns={{ base: 1, md: 2 }} gap={4} p={5}>
              {/* CPU */}
              <HStack gap={3}>
                <Flex w="34px" h="34px" borderRadius="8px" bg={`${AD}0.1)`} align="center" justify="center" flexShrink={0}>
                  <Cpu size={15} color={A} />
                </Flex>
                <Box>
                  <Text color={T} fontWeight="500" fontSize="sm">{state.report.cpu.name}</Text>
                  <Text color={TS} fontSize="xs">
                    {state.report.cpu.codename} · {state.report.cpu.cores}C/{state.report.cpu.threads}T
                  </Text>
                </Box>
              </HStack>

              {/* GPUs */}
              {state.report.gpu.map((gpu, i) => (
                <HStack key={i} gap={3}>
                  <Flex w="34px" h="34px" borderRadius="8px" bg="rgba(167,139,250,0.1)" align="center" justify="center" flexShrink={0}>
                    <MonitorSmartphone size={15} color="#A78BFA" />
                  </Flex>
                  <Box>
                    <Text color={T} fontWeight="500" fontSize="sm">{gpu.name}</Text>
                    <Text color={TS} fontSize="xs">
                      {gpu.codename ?? gpu.vendor}{gpu.vram ? ` · ${Math.round(gpu.vram / 1024)}GB` : ''}
                    </Text>
                  </Box>
                </HStack>
              ))}

              {/* Motherboard */}
              <HStack gap={3}>
                <Flex w="34px" h="34px" borderRadius="8px" bg="rgba(45,212,191,0.1)" align="center" justify="center" flexShrink={0}>
                  <CircuitBoard size={15} color="#2DD4BF" />
                </Flex>
                <Box>
                  <Text color={T} fontWeight="500" fontSize="sm">{state.report.motherboard.model}</Text>
                  <Text color={TS} fontSize="xs">
                    {state.report.motherboard.manufacturer}{state.report.motherboard.chipset ? ` · ${state.report.motherboard.chipset}` : ''}
                  </Text>
                </Box>
              </HStack>

              {/* Audio */}
              {state.report.audio.length > 0 && (
                <HStack gap={3}>
                  <Flex w="34px" h="34px" borderRadius="8px" bg="rgba(234,179,8,0.1)" align="center" justify="center" flexShrink={0}>
                    <Volume2 size={15} color="#EAB308" />
                  </Flex>
                  <Box>
                    <Text color={T} fontWeight="500" fontSize="sm">{state.report.audio[0].name}</Text>
                    <Text color={TS} fontSize="xs">Codec ID: {state.report.audio[0].codecId}</Text>
                  </Box>
                </HStack>
              )}
            </SimpleGrid>

            <Flex justify="flex-end" gap={3} px={5} py={3.5} borderTop={`1px solid ${B}`}>
              <Box as="button" px={4} py="7px" borderRadius="8px"
                bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
                _hover={{ bg: 'rgba(255,255,255,0.08)' }} onClick={handleDetect}
                display="flex" alignItems="center" gap={2}
              >
                <RefreshCw size={13} /> Re-scan
              </Box>
              <Box as="button" px={4} py="7px" borderRadius="8px"
                bg={A} color="white" fontSize="sm" fontWeight="600"
                _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
                onClick={() => navigate('/compatibility')}
                display="flex" alignItems="center" gap={2}
              >
                Check Compatibility <ChevronRight size={14} />
              </Box>
            </Flex>
          </Box>
        )}
      </VStack>
    </Box>
  )
}