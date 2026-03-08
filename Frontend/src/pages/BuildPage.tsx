import { useCallback, useState } from 'react'
import { Box, Text, HStack, Flex } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { BuildProgress, BuildResult, BuildStage } from '../types'
import {
  Download, Package, Settings, FileText, Usb,
  FolderArchive, CheckCircle2, XCircle, ChevronLeft, ChevronRight, Rocket, Ban,
} from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'

const stageIcons: Record<BuildStage, React.ReactNode> = {
  idle:                   <Settings size={14} color={TS} />,
  'downloading-opencore': <Download size={14} color="#38BDF8" />,
  'downloading-kexts':    <Package size={14} color="#A78BFA" />,
  'generating-acpi':      <Settings size={14} color="#EAB308" />,
  'generating-config':    <FileText size={14} color={TEAL} />,
  'generating-usb-map':   <Usb size={14} color="#22C55E" />,
  packaging:              <FolderArchive size={14} color="#f97316" />,
  complete:               <CheckCircle2 size={14} color="#22C55E" />,
  error:                  <XCircle size={14} color="#EF4444" />,
}

const stages: BuildStage[] = [
  'downloading-opencore', 'downloading-kexts', 'generating-acpi',
  'generating-config', 'generating-usb-map', 'packaging', 'complete',
]

export function BuildPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
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

  const canBuild = !!(state.report && state.selectedMacOS && state.smbios)
  const currentStageIndex = state.buildProgress ? stages.indexOf(state.buildProgress.stage) : -1
  const progressPercent   = state.buildProgress?.progress ?? 0

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Box mb={5}>
        <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
          {t('build.title')}
        </Text>
        <Text color={TS} fontSize="13px">{t('build.subtitle')}</Text>
      </Box>

      <Box flex={1} overflowY="auto" minH={0}>
        <Flex direction="column" gap={4}>

          {/* ── Pre-build Summary ──────────────────────────────────────── */}
          {!isBuilding && !state.buildProgress && (
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
                <Text color={T} fontWeight="600" fontSize="13px">{t('build.summary')}</Text>
              </Box>
              <Flex direction="column" px={5} py={2}>
                {[
                  { label: t('build.hardwareReport'), value: state.report ? t('build.loaded') : t('build.missing'),          ok: !!state.report },
                  { label: t('build.macosVersion'),   value: state.selectedMacOS?.name ?? t('build.notSelected'),             ok: !!state.selectedMacOS },
                  { label: t('build.acpiPatches'),    value: `${state.acpiPatches.filter((p) => p.enabled).length} ${t('build.enabled')}`, ok: true },
                  { label: t('build.kexts'),          value: `${state.kexts.filter((k) => k.enabled).length} ${t('build.enabled')}`,       ok: true },
                  { label: t('build.smbios'),         value: state.smbios?.model ?? t('build.notGenerated'),                  ok: !!state.smbios },
                  { label: t('build.usbPorts'),       value: `${state.usbControllers.reduce((a, c) => a + c.ports.filter((p) => p.selected).length, 0)} ${t('build.mapped')}`, ok: true },
                ].map((item, i, arr) => (
                  <Flex key={item.label} justify="space-between" align="center" py="10px"
                    borderBottom={i < arr.length - 1 ? `1px solid rgba(255,255,255,0.04)` : 'none'}
                  >
                    <Text color={TS} fontSize="13px">{item.label}</Text>
                    <Box px="8px" py="2px" borderRadius="5px" fontSize="11px" fontWeight="600"
                      bg={item.ok ? 'rgba(45,212,191,0.08)' : 'rgba(239,68,68,0.08)'}
                      border={`1px solid ${item.ok ? 'rgba(45,212,191,0.2)' : 'rgba(239,68,68,0.2)'}`}
                      color={item.ok ? TEAL : '#EF4444'}
                    >
                      {item.value}
                    </Box>
                  </Flex>
                ))}
              </Flex>
              <Box px={5} py={4}>
                <Box
                  as="button" w="100%" py="10px" borderRadius="10px"
                  bg={canBuild ? TEAL : 'rgba(255,255,255,0.06)'}
                  color={canBuild ? '#0A0A0A' : TS}
                  fontWeight="700" fontSize="13px"
                  _hover={canBuild ? { bg: '#38E5CE', boxShadow: '0 0 24px rgba(45,212,191,0.3)' } : {}}
                  cursor={canBuild ? 'pointer' : 'not-allowed'}
                  onClick={() => canBuild && startBuild()}
                  display="flex" alignItems="center" justifyContent="center" gap={2}
                  transition="all 0.2s" opacity={canBuild ? 1 : 0.5}
                >
                  <Rocket size={15} /> {t('build.startBuild')}
                </Box>
              </Box>
            </Box>
          )}

          {/* ── Build Progress ──────────────────────────────────────────── */}
          {(isBuilding || state.buildProgress) && state.buildProgress?.stage !== 'error' && (
            <Box bg={S} border="1px solid rgba(45,212,191,0.15)" borderRadius="12px" overflow="hidden">
              <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
                <HStack gap={2}>
                  {stageIcons[state.buildProgress?.stage ?? 'idle']}
                  <Text color={T} fontWeight="600" fontSize="13px">
                    {t(`build.stages.${state.buildProgress?.stage ?? 'idle'}`)}
                  </Text>
                </HStack>
                {isBuilding && (
                  <Box
                    as="button" px={3} py="5px" borderRadius="6px"
                    bg="rgba(239,68,68,0.07)" color="#EF4444"
                    fontSize="11px" fontWeight="500"
                    _hover={{ bg: 'rgba(239,68,68,0.12)' }}
                    onClick={cancelBuild} display="flex" alignItems="center" gap={1}
                    transition="all 0.15s"
                  >
                    <Ban size={11} /> {t('build.cancel')}
                  </Box>
                )}
              </Flex>
              <Box px={5} py={5}>
                <Box w="100%" bg="rgba(255,255,255,0.05)" borderRadius="full" h="4px" mb={4}>
                  <Box
                    bg={TEAL} borderRadius="full" h="100%"
                    w={`${progressPercent}%`}
                    transition="width 0.4s ease"
                    boxShadow={`0 0 10px rgba(45,212,191,0.5)`}
                  />
                </Box>
                <Text color={TS} fontSize="13px" textAlign="center" mb={5}>
                  {state.buildProgress?.message}
                </Text>

                <Flex justify="space-between" px={2}>
                  {stages.slice(0, -1).map((stage, i) => {
                    const isDone    = i < currentStageIndex
                    const isCurrent = i === currentStageIndex
                    return (
                      <Flex
                        key={stage}
                        w="28px" h="28px" borderRadius="full"
                        bg={isDone ? 'rgba(45,212,191,0.12)' : isCurrent ? 'rgba(45,212,191,0.08)' : 'rgba(255,255,255,0.04)'}
                        border={isCurrent ? '2px solid rgba(45,212,191,0.4)' : 'none'}
                        align="center" justify="center"
                        transition="all 0.3s"
                      >
                        {isDone ? <CheckCircle2 size={13} color={TEAL} /> : stageIcons[stage]}
                      </Flex>
                    )
                  })}
                </Flex>
              </Box>
            </Box>
          )}

          {/* ── Build Log ──────────────────────────────────────────────── */}
          {state.buildProgress && state.buildProgress.log.length > 0 && (
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
                <Text color={T} fontWeight="600" fontSize="13px">{t('build.buildLog')}</Text>
              </Box>
              <Box px={4} py={3} maxH="200px" overflowY="auto" fontFamily="mono" fontSize="11px">
                {state.buildProgress.log.map((line, i) => (
                  <Text key={i} color={TM} py="1px">{line}</Text>
                ))}
              </Box>
            </Box>
          )}

          {/* ── Error ──────────────────────────────────────────────────── */}
          {state.buildProgress?.stage === 'error' && (
            <Box bg="rgba(239,68,68,0.05)" border="1px solid rgba(239,68,68,0.16)"
              borderRadius="12px" p={6}
            >
              <Flex direction="column" align="center" gap={4}>
                <XCircle size={32} color="#EF4444" />
                <Text color="#EF4444" fontWeight="700" fontSize="16px">{t('build.buildFailed')}</Text>
                <Text color={TS} fontSize="13px" textAlign="center">
                  {state.buildProgress.message}
                </Text>
                <Box
                  as="button" px={5} py="8px" borderRadius="8px"
                  bg="rgba(239,68,68,0.1)" color="#EF4444"
                  fontSize="13px" fontWeight="600"
                  _hover={{ bg: 'rgba(239,68,68,0.16)' }}
                  onClick={startBuild} transition="all 0.15s"
                >
                  {t('build.retryBuild')}
                </Box>
              </Flex>
            </Box>
          )}
        </Flex>
      </Box>

      {/* ── Navigation ─────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" pt={5} mt={4} borderTop={`1px solid ${B}`}>
        <Box as="button" px={4} py="8px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
          onClick={() => navigate('/usb')} display="flex" alignItems="center" gap={2}
          opacity={isBuilding ? 0.4 : 1} pointerEvents={isBuilding ? 'none' : 'auto'}
          transition="all 0.15s">
          <ChevronLeft size={14} /> {t('build.usbMapper')}
        </Box>
        {state.buildResult && (
          <Box as="button" px={5} py="8px" borderRadius="8px"
            bg={TEAL} color="#0A0A0A" fontSize="13px" fontWeight="700"
            _hover={{ bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' }}
            onClick={() => navigate('/result')} display="flex" alignItems="center" gap={2}
            transition="all 0.15s">
            {t('build.viewResult')} <ChevronRight size={14} />
          </Box>
        )}
      </Flex>
    </Flex>
  )
}
