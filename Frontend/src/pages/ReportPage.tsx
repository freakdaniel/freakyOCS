import { useCallback, useState } from 'react'
import {
  Box, Text, HStack, Flex, Spinner,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { HardwareReport } from '../types'
import {
  ScanSearch, FileJson, Cpu, MonitorSmartphone, CircuitBoard,
  Volume2, AlertTriangle, ChevronRight, RefreshCw, FolderOpen,
} from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const S2   = '#161616'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'

export function ReportPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isDetecting, setIsDetecting] = useState(false)
  const [isDragOver, setIsDragOver]   = useState(false)
  const [error, setError]             = useState<string | null>(null)
  const [savedPath, setSavedPath]     = useState<string | null>(null)

  usePhotinoEvent<HardwareReport>('hardware:detected', (data) => {
    setIsDetecting(false)
    if (data) { dispatch({ type: 'SET_REPORT', report: data }); setError(null) }
  })

  usePhotinoEvent<string>('hardware:report-saved', (path) => {
    if (path) setSavedPath(path)
  })

  usePhotinoEvent<string>('error', (data) => {
    setIsDetecting(false)
    setError(data ?? 'Unknown error occurred')
  })

  const handleDetect = useCallback(() => {
    setIsDetecting(true); setError(null); setSavedPath(null)
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
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Box mb={5}>
        <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
          {t('report.title')}
        </Text>
        <Text color={TS} fontSize="13px">{t('report.subtitle')}</Text>
      </Box>

      {/* ── Error ──────────────────────────────────────────────────────── */}
      {error && (
        <HStack
          gap={3} px={4} py={3} mb={4} borderRadius="10px"
          bg="rgba(239,68,68,0.06)" border="1px solid rgba(239,68,68,0.16)"
        >
          <AlertTriangle size={15} color="#EF4444" />
          <Text color="#EF4444" fontSize="13px">{error}</Text>
        </HStack>
      )}

      {/* ── Detection options ───────────────────────────────────────────── */}
      <Flex gap={4} mb={5} direction={{ base: 'column', md: 'row' }}>

        {/* Auto Detect card */}
        <Box
          flex={1}
          bg={isDetecting ? 'rgba(45,212,191,0.05)' : S}
          border={`1px solid ${isDetecting ? 'rgba(45,212,191,0.25)' : B}`}
          borderRadius="14px" p={6}
          cursor={isDetecting ? 'default' : 'pointer'}
          onClick={!isDetecting ? handleDetect : undefined}
          _hover={!isDetecting ? { borderColor: 'rgba(45,212,191,0.25)', bg: '#161616' } : {}}
          transition="all 0.2s ease"
          display="flex" alignItems="center" justifyContent="center"
        >
          <Flex direction="column" align="center" gap={4} py={2}>
            {isDetecting ? (
              <>
                <Spinner size="lg" color={TEAL} borderWidth="2px" />
                <Text color={TS} fontSize="13px">{t('report.detectingHw')}</Text>
              </>
            ) : (
              <>
                <Flex w="48px" h="48px" borderRadius="12px"
                  bg="rgba(45,212,191,0.1)" align="center" justify="center">
                  <ScanSearch size={22} color={TEAL} />
                </Flex>
                <Box textAlign="center">
                  <Text color={T} fontWeight="600" fontSize="13px" mb={1}>{t('report.autoDetect')}</Text>
                  <Text color={TS} fontSize="12px" lineHeight="1.6">{t('report.autoDetectDesc')}</Text>
                </Box>
              </>
            )}
          </Flex>
        </Box>

        {/* Drag & Drop card */}
        <Box
          flex={1}
          bg={isDragOver ? 'rgba(45,212,191,0.05)' : S}
          border={`2px dashed ${isDragOver ? 'rgba(45,212,191,0.4)' : B}`}
          borderRadius="14px" p={6}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          transition="all 0.2s ease"
        >
          <Flex direction="column" align="center" gap={4} py={2}>
            <Flex w="48px" h="48px" borderRadius="12px"
              bg={isDragOver ? 'rgba(45,212,191,0.1)' : 'rgba(255,255,255,0.04)'}
              align="center" justify="center">
              <FileJson size={22} color={isDragOver ? TEAL : TS} />
            </Flex>
            <Box textAlign="center">
              <Text color={T} fontWeight="600" fontSize="13px" mb={1}>{t('report.loadFile')}</Text>
              <Text color={TS} fontSize="12px" lineHeight="1.6">{t('report.loadFileDesc')}</Text>
            </Box>
            <Box px={3} py="3px" borderRadius="5px"
              bg="rgba(255,255,255,0.04)" border={`1px solid ${B}`}
              fontSize="10px" color={TM} fontWeight="600" letterSpacing="0.05em">
              .JSON
            </Box>
          </Flex>
        </Box>
      </Flex>

      {/* ── Saved path notice ───────────────────────────────────────────── */}
      {savedPath && (
        <HStack
          gap={3} px={4} py={3} mb={4} borderRadius="10px"
          bg="rgba(45,212,191,0.06)" border="1px solid rgba(45,212,191,0.18)"
          animation="fadeIn 0.3s ease-out"
        >
          <FolderOpen size={14} color={TEAL} />
          <Text color={TEAL} fontSize="12px" fontWeight="500">
            {t('report.savedTo')} <Text as="span" fontWeight="700">{savedPath}</Text>
          </Text>
        </HStack>
      )}

      {/* ── Current report ──────────────────────────────────────────────── */}
      {state.report && (
        <Box flex={1} overflowY="auto" minH={0}>
          <Box bg={S} border={`1px solid ${B}`} borderRadius="14px" overflow="hidden">

            <Flex justify="space-between" align="center"
              px={5} py={3} borderBottom={`1px solid ${B}`}>
              <HStack gap={3}>
                <Text color={T} fontWeight="600" fontSize="13px">{t('report.title')}</Text>
                <Box px="8px" py="2px" borderRadius="5px"
                  bg="rgba(45,212,191,0.08)" border="1px solid rgba(45,212,191,0.2)"
                  fontSize="10px" fontWeight="700" color={TEAL}>
                  {t('report.loadedBadge')}
                </Box>
                <Text color={TM} fontSize="10px">
                  {state.report.platform} · {new Date(state.report.generatedAt).toLocaleTimeString()}
                </Text>
              </HStack>
              <HStack gap={2}>
                <Box
                  as="button" px={3} py="6px" borderRadius="7px"
                  bg="rgba(255,255,255,0.04)" color={TS} fontSize="11px" fontWeight="500"
                  _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
                  onClick={handleDetect} display="flex" alignItems="center" gap={1.5}
                  transition="all 0.15s"
                >
                  <RefreshCw size={11} /> {t('report.rescan')}
                </Box>
                <Box
                  as="button" px={3} py="6px" borderRadius="7px"
                  bg={TEAL} color="#0A0A0A" fontSize="11px" fontWeight="700"
                  _hover={{ bg: '#38E5CE', boxShadow: `0 0 18px rgba(45,212,191,0.3)` }}
                  onClick={() => navigate('/compatibility')}
                  display="flex" alignItems="center" gap={1.5} transition="all 0.15s"
                >
                  {t('report.checkCompatibility')} <ChevronRight size={12} />
                </Box>
              </HStack>
            </Flex>

            <Box
              display="grid"
              gridTemplateColumns="repeat(auto-fill, minmax(240px, 1fr))"
              gap={3} p={5}
            >
              <HStack gap={3} p={3} borderRadius="10px" bg={S2} border={`1px solid ${B}`}>
                <Flex w="32px" h="32px" borderRadius="8px"
                  bg="rgba(45,212,191,0.1)" align="center" justify="center" flexShrink={0}>
                  <Cpu size={14} color={TEAL} />
                </Flex>
                <Box minW={0}>
                  <Text color={T} fontWeight="500" fontSize="12px" truncate>
                    {state.report.cpu.name}
                  </Text>
                  <Text color={TS} fontSize="11px">
                    {state.report.cpu.codename ? `${state.report.cpu.codename} · ` : ''}
                    {state.report.cpu.cores}C/{state.report.cpu.threads}T
                  </Text>
                </Box>
              </HStack>

              {state.report.gpu.map((gpu, i) => (
                <HStack key={i} gap={3} p={3} borderRadius="10px" bg={S2} border={`1px solid ${B}`}>
                  <Flex w="32px" h="32px" borderRadius="8px"
                    bg="rgba(139,92,246,0.1)" align="center" justify="center" flexShrink={0}>
                    <MonitorSmartphone size={14} color="#A78BFA" />
                  </Flex>
                  <Box minW={0}>
                    <Text color={T} fontWeight="500" fontSize="12px" truncate>{gpu.name}</Text>
                    <Text color={TS} fontSize="11px">
                      {gpu.codename ?? gpu.vendor}
                      {gpu.vram ? ` · ${Math.round(gpu.vram / 1024)}GB` : ''}
                    </Text>
                  </Box>
                </HStack>
              ))}

              <HStack gap={3} p={3} borderRadius="10px" bg={S2} border={`1px solid ${B}`}>
                <Flex w="32px" h="32px" borderRadius="8px"
                  bg="rgba(245,158,11,0.1)" align="center" justify="center" flexShrink={0}>
                  <CircuitBoard size={14} color="#F59E0B" />
                </Flex>
                <Box minW={0}>
                  <Text color={T} fontWeight="500" fontSize="12px" truncate>
                    {state.report.motherboard.model}
                  </Text>
                  <Text color={TS} fontSize="11px">
                    {state.report.motherboard.manufacturer}
                    {state.report.motherboard.chipset ? ` · ${state.report.motherboard.chipset}` : ''}
                  </Text>
                </Box>
              </HStack>

              {state.report.audio.length > 0 && (
                <HStack gap={3} p={3} borderRadius="10px" bg={S2} border={`1px solid ${B}`}>
                  <Flex w="32px" h="32px" borderRadius="8px"
                    bg="rgba(99,102,241,0.1)" align="center" justify="center" flexShrink={0}>
                    <Volume2 size={14} color="#818CF8" />
                  </Flex>
                  <Box minW={0}>
                    <Text color={T} fontWeight="500" fontSize="12px" truncate>
                      {state.report.audio[0].name}
                    </Text>
                    <Text color={TS} fontSize="11px">
                      Codec: {state.report.audio[0].codecId ?? state.report.audio[0].deviceId}
                    </Text>
                  </Box>
                </HStack>
              )}
            </Box>


          </Box>
        </Box>
      )}
    </Flex>
  )
}
