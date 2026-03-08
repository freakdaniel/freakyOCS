import { useCallback, useState } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { BuildProgress, BuildResult, BuildStage } from '../types'
import {
  Hammer, Download, Package, Settings, FileText, Usb,
  FolderArchive, CheckCircle2, XCircle, ChevronLeft, ChevronRight, Rocket, Ban,
} from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'

const stageLabels: Record<BuildStage, string> = {
  idle:                   'Ready to build',
  'downloading-opencore': 'Downloading OpenCore…',
  'downloading-kexts':    'Downloading kexts…',
  'generating-acpi':      'Generating ACPI tables…',
  'generating-config':    'Generating config.plist…',
  'generating-usb-map':   'Generating USB map…',
  packaging:              'Packaging EFI folder…',
  complete:               'Build complete!',
  error:                  'Build failed',
}

const stageIcons: Record<BuildStage, React.ReactNode> = {
  idle:                   <Settings size={15} color={TS} />,
  'downloading-opencore': <Download size={15} color="#38BDF8" />,
  'downloading-kexts':    <Package size={15} color="#A78BFA" />,
  'generating-acpi':      <Settings size={15} color="#EAB308" />,
  'generating-config':    <FileText size={15} color={A} />,
  'generating-usb-map':   <Usb size={15} color="#22C55E" />,
  packaging:              <FolderArchive size={15} color="#f97316" />,
  complete:               <CheckCircle2 size={15} color="#22C55E" />,
  error:                  <XCircle size={15} color="#EF4444" />,
}

const stages: BuildStage[] = [
  'downloading-opencore', 'downloading-kexts', 'generating-acpi',
  'generating-config', 'generating-usb-map', 'packaging', 'complete',
]

export function BuildPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isBuilding, setIsBuilding] = useState(false)

  usePhotinoEvent<BuildProgress>('build:progress', (data) => {
    if (data) dispatch({ type: 'SET_BUILD_PROGRESS', progress: data })
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
      progress: { stage: 'error', progress: 0, message: data ?? 'Unknown error', log: state.buildProgress?.log ?? [] },
    })
  })

  const startBuild = useCallback(() => {
    setIsBuilding(true)
    dispatch({
      type: 'SET_BUILD_PROGRESS',
      progress: { stage: 'downloading-opencore', progress: 0, message: 'Starting…', log: [] },
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

  const canBuild = state.report && state.selectedMacOS && state.smbios
  const currentStageIndex = state.buildProgress ? stages.indexOf(state.buildProgress.stage) : -1
  const progressPercent   = state.buildProgress?.progress ?? 0

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Box>
          <HStack gap={3} mb={1.5}>
            <Flex w="34px" h="34px" borderRadius="8px" bg={`${AD}0.12)`} align="center" justify="center">
              <Hammer size={17} color={A} />
            </Flex>
            <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">Build EFI</Heading>
          </HStack>
          <Text color={TS} fontSize="sm">Generate your OpenCore EFI folder with all configured options.</Text>
        </Box>

        {/* Pre-build Summary */}
        {!isBuilding && !state.buildProgress && (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
              <Text color={T} fontWeight="600" fontSize="sm">Build Summary</Text>
            </Box>
            <VStack align="stretch" gap={0} px={5} py={3}>
              {[
                { label: 'Hardware Report', value: state.report ? 'Loaded' : 'Missing',          ok: !!state.report },
                { label: 'macOS Version',   value: state.selectedMacOS?.name ?? 'Not selected',   ok: !!state.selectedMacOS },
                { label: 'ACPI Patches',    value: `${state.acpiPatches.filter((p) => p.enabled).length} enabled`, ok: true },
                { label: 'Kexts',           value: `${state.kexts.filter((k) => k.enabled).length} enabled`,       ok: true },
                { label: 'SMBIOS',          value: state.smbios?.model ?? 'Not generated',        ok: !!state.smbios },
                { label: 'USB Ports',       value: `${state.usbControllers.reduce((a, c) => a + c.ports.filter((p) => p.selected).length, 0)} mapped`, ok: true },
              ].map((item) => (
                <Flex key={item.label} justify="space-between" align="center" py={2.5} borderBottom={`1px solid rgba(255,255,255,0.04)`}>
                  <Text color={TS} fontSize="sm">{item.label}</Text>
                  <Box
                    px={2} py={0.5} borderRadius="5px" fontSize="xs" fontWeight="600"
                    bg={item.ok ? 'rgba(34,197,94,0.1)' : 'rgba(239,68,68,0.1)'}
                    color={item.ok ? '#22C55E' : '#EF4444'}
                  >
                    {item.value}
                  </Box>
                </Flex>
              ))}
            </VStack>
            <Box px={5} py={4}>
              <Box
                as="button" w="100%" py="10px" borderRadius="10px"
                bg={canBuild ? A : `${AD}0.3)`}
                color="white" fontWeight="600" fontSize="sm"
                _hover={canBuild ? { bg: '#8F93FF', boxShadow: '0 0 20px rgba(123,127,255,0.3)' } : {}}
                cursor={canBuild ? 'pointer' : 'not-allowed'}
                onClick={() => canBuild && startBuild()}
                display="flex" alignItems="center" justifyContent="center" gap={2}
                transition="all 0.2s"
              >
                <Rocket size={15} /> Start Build
              </Box>
            </Box>
          </Box>
        )}

        {/* Build Progress */}
        {(isBuilding || state.buildProgress) && state.buildProgress?.stage !== 'error' && (
          <Box bg={S} border={`1px solid ${AD}0.18)`} borderRadius="12px" overflow="hidden">
            <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
              <HStack gap={2}>
                {stageIcons[state.buildProgress?.stage ?? 'idle']}
                <Text color={T} fontWeight="600" fontSize="sm">{stageLabels[state.buildProgress?.stage ?? 'idle']}</Text>
              </HStack>
              {isBuilding && (
                <Box
                  as="button" px={3} py={1.5} borderRadius="6px"
                  bg="rgba(239,68,68,0.08)" color="#EF4444"
                  fontSize="xs" fontWeight="500"
                  _hover={{ bg: 'rgba(239,68,68,0.14)' }}
                  onClick={cancelBuild} display="flex" alignItems="center" gap={1}
                >
                  <Ban size={11} /> Cancel
                </Box>
              )}
            </Flex>
            <Box px={5} py={4}>
              {/* Progress bar */}
              <Box w="100%" bg="rgba(255,255,255,0.05)" borderRadius="full" h="5px" mb={3}>
                <Box bg={A} borderRadius="full" h="100%" w={`${progressPercent}%`} transition="width 0.4s ease" boxShadow={`0 0 8px ${AD}0.5)`} />
              </Box>
              <Text color={TS} fontSize="sm" textAlign="center">{state.buildProgress?.message}</Text>

              {/* Stage indicators */}
              <Flex justify="space-between" mt={4} px={2}>
                {stages.slice(0, -1).map((stage, i) => {
                  const isDone    = i < currentStageIndex
                  const isCurrent = i === currentStageIndex
                  return (
                    <VStack key={stage} gap={1}>
                      <Flex
                        w="26px" h="26px" borderRadius="full"
                        bg={isDone ? 'rgba(34,197,94,0.1)' : isCurrent ? `${AD}0.13)` : 'rgba(255,255,255,0.04)'}
                        border={isCurrent ? `2px solid ${AD}0.35)` : 'none'}
                        align="center" justify="center"
                      >
                        {isDone ? <CheckCircle2 size={13} color="#22C55E" /> : stageIcons[stage]}
                      </Flex>
                    </VStack>
                  )
                })}
              </Flex>
            </Box>
          </Box>
        )}

        {/* Build Log */}
        {state.buildProgress && state.buildProgress.log.length > 0 && (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
              <Text color={T} fontWeight="600" fontSize="sm">Build Log</Text>
            </Box>
            <Box px={4} py={3} maxH="240px" overflowY="auto" fontFamily="mono" fontSize="xs">
              {state.buildProgress.log.map((line, i) => (
                <Text key={i} color={TS} py={0.5}>{line}</Text>
              ))}
            </Box>
          </Box>
        )}

        {/* Error */}
        {state.buildProgress?.stage === 'error' && (
          <Box bg="rgba(239,68,68,0.05)" border="1px solid rgba(239,68,68,0.18)" borderRadius="12px" p={6}>
            <VStack gap={4}>
              <XCircle size={32} color="#EF4444" />
              <Heading size="md" color="#EF4444">Build Failed</Heading>
              <Text color={TS} fontSize="sm" textAlign="center">{state.buildProgress.message}</Text>
              <Box
                as="button" px={5} py={2} borderRadius="8px"
                bg="rgba(239,68,68,0.12)" color="#EF4444"
                fontSize="sm" fontWeight="600"
                _hover={{ bg: 'rgba(239,68,68,0.18)' }}
                onClick={startBuild}
              >
                Retry Build
              </Box>
            </VStack>
          </Box>
        )}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box
            as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.07)', color: T }}
            onClick={() => navigate('/usb')} display="flex" alignItems="center" gap={2}
            opacity={isBuilding ? 0.4 : 1} pointerEvents={isBuilding ? 'none' : 'auto'}
          >
            <ChevronLeft size={14} /> USB Mapper
          </Box>
          {state.buildResult && (
            <Box
              as="button" px={4} py="8px" borderRadius="8px"
              bg={A} color="white" fontSize="sm" fontWeight="600"
              _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
              onClick={() => navigate('/result')} display="flex" alignItems="center" gap={2}
            >
              View Result <ChevronRight size={14} />
            </Box>
          )}
        </Flex>
      </VStack>
    </Box>
  )
}