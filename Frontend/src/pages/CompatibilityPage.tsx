import { useEffect, useCallback, useState } from 'react'
import {
  Box, Text, HStack, Flex, Spinner,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { CompatibilityResult, CompatibilityStatus } from '../types'
import {
  ShieldCheck, CheckCircle2, AlertTriangle, XCircle, HelpCircle, RefreshCw,
  ChevronLeft, ChevronRight, Cpu, MonitorSmartphone, CircuitBoard, Volume2,
  Wifi, HardDrive, Bluetooth, Usb, Keyboard, Mouse, Camera, Fingerprint,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'

const statusConfig: Record<CompatibilityStatus, {
  color: string; bg: string; border: string; icon: LucideIcon; labelKey: string
}> = {
  supported:   { color: TEAL,      bg: 'rgba(45,212,191,0.07)',   border: 'rgba(45,212,191,0.2)',   icon: CheckCircle2,  labelKey: 'compatibility.supported' },
  limited:     { color: '#EAB308', bg: 'rgba(234,179,8,0.07)',    border: 'rgba(234,179,8,0.2)',    icon: AlertTriangle, labelKey: 'compatibility.limited' },
  unsupported: { color: '#EF4444', bg: 'rgba(239,68,68,0.07)',    border: 'rgba(239,68,68,0.2)',    icon: XCircle,       labelKey: 'compatibility.unsupported' },
  unknown:     { color: TS,        bg: 'rgba(136,136,136,0.07)',  border: 'rgba(136,136,136,0.15)', icon: HelpCircle,    labelKey: 'compatibility.unknown' },
}

const categoryIcons: Record<string, LucideIcon> = {
  CPU: Cpu,
  GPU: MonitorSmartphone,
  Motherboard: CircuitBoard,
  Audio: Volume2,
  WiFi: Wifi,
  Ethernet: HardDrive,
  Network: HardDrive,
  Bluetooth: Bluetooth,
  USB: Usb,
  Storage: HardDrive,
  Input: Keyboard,
  Keyboard: Keyboard,
  Mouse: Mouse,
  Trackpad: Mouse,
  Webcam: Camera,
  Camera: Camera,
  Biometric: Fingerprint,
  TouchID: Fingerprint,
}

export function CompatibilityPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isChecking, setIsChecking] = useState(false)

  usePhotinoEvent<CompatibilityResult>('compatibility:result', (data) => {
    setIsChecking(false)
    if (data) dispatch({ type: 'SET_COMPATIBILITY', result: data })
  })

  const checkCompatibility = useCallback(() => {
    if (!state.report) return
    setIsChecking(true)
    invoke('compatibility:check', state.report)
  }, [invoke, state.report])

  useEffect(() => {
    if (state.report && !state.compatibility) checkCompatibility()
  }, [state.report, state.compatibility, checkCompatibility])

  // ── No hardware loaded ──────────────────────────────────────────────────────
  if (!state.report) {
    return (
      <Flex direction="column" h="100vh" bg={BG} px={7} py={6}>
        <Box mb={5}>
          <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
            {t('compatibility.title')}
          </Text>
          <Text color={TS} fontSize="13px">{t('compatibility.subtitle')}</Text>
        </Box>
        <Flex
          flex={1} align="center" justify="center" direction="column" gap={4}
          bg={S} border={`1px solid ${B}`} borderRadius="14px"
        >
          <Flex w="52px" h="52px" borderRadius="14px"
            bg="rgba(136,136,136,0.08)" align="center" justify="center">
            <ShieldCheck size={24} color={TS} />
          </Flex>
          <Box textAlign="center">
            <Text color={T} fontWeight="600" fontSize="14px" mb={1}>{t('compatibility.noHardware')}</Text>
            <Text color={TS} fontSize="13px">{t('compatibility.noHardwareDesc')}</Text>
          </Box>
          <Box
            as="button" px={5} py="9px" borderRadius="9px"
            bg={TEAL} color="#0A0A0A" fontSize="13px" fontWeight="600"
            _hover={{ bg: '#38E5CE', boxShadow: `0 0 20px rgba(45,212,191,0.3)` }}
            onClick={() => navigate('/report')} transition="all 0.15s"
          >
            {t('compatibility.goToDetection')}
          </Box>
        </Flex>
      </Flex>
    )
  }

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Box mb={5}>
        <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
          {t('compatibility.title')}
        </Text>
        <Text color={TS} fontSize="13px">{t('compatibility.subtitle')}</Text>
      </Box>

      {/* ── Content ────────────────────────────────────────────────────── */}
      <Box flex={1} overflowY="auto" minH={0}>
        {isChecking ? (
          <Flex h="200px" align="center" justify="center" gap={3}
            bg={S} border={`1px solid ${B}`} borderRadius="12px" direction="column"
          >
            <Spinner size="md" color={TEAL} borderWidth="2px" />
            <Text color={TS} fontSize="13px">{t('common.loading')}</Text>
          </Flex>
        ) : state.compatibility ? (
          <Flex direction="column" gap={3}>

            {/* Summary bar */}
            <Flex bg={S} border={`1px solid ${B}`} borderRadius="12px" px={4} py={3}
              justify="space-between" align="center"
            >
              <HStack gap={3}>
                {(() => {
                  const cfg = statusConfig[state.compatibility.overallStatus]
                  const Icon = cfg.icon
                  return (
                    <>
                      <Flex w="36px" h="36px" borderRadius="9px"
                        bg={cfg.bg} border={`1px solid ${cfg.border}`}
                        align="center" justify="center"
                      >
                        <Icon size={17} color={cfg.color} />
                      </Flex>
                      <Box>
                        <Text color={TS} fontSize="10px" fontWeight="600"
                          textTransform="uppercase" letterSpacing="0.07em">
                          {t('compatibility.overallStatus')}
                        </Text>
                        <Text color={cfg.color} fontSize="13px" fontWeight="700">
                          {t(cfg.labelKey)}
                        </Text>
                      </Box>
                    </>
                  )
                })()}
              </HStack>
              <Box as="button" px={3} py="6px" borderRadius="8px"
                bg="rgba(255,255,255,0.04)" color={TS} fontSize="12px" fontWeight="500"
                _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
                onClick={checkCompatibility} display="flex" alignItems="center" gap={2}
                transition="all 0.15s"
              >
                <RefreshCw size={12} /> {t('compatibility.reCheck')}
              </Box>
            </Flex>

            {/* Device table */}
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Flex px={4} py={3} borderBottom={`1px solid ${B}`}
                justify="space-between" align="center"
              >
                <Text color={T} fontWeight="600" fontSize="13px">{t('compatibility.deviceCompat')}</Text>
                <Text color={TS} fontSize="11px">{state.compatibility.devices.length} {t('nav.usb').toLowerCase().includes('usb') ? 'devices' : 'devices'}</Text>
              </Flex>

              {/* Header row */}
              <Flex px={4} py={2} borderBottom={`1px solid ${B}`} bg="rgba(255,255,255,0.02)">
                <Box flex="0 0 110px">
                  <Text fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.07em">
                    {t('compatibility.category')}
                  </Text>
                </Box>
                <Text flex={1} fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.07em">
                  {t('compatibility.device')}
                </Text>
                <Box flex="0 0 110px">
                  <Text fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.07em">
                    {t('compatibility.status')}
                  </Text>
                </Box>
                <Text flex={1} fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.07em">
                  {t('compatibility.notes')}
                </Text>
              </Flex>

              {/* Body rows */}
              {state.compatibility.devices.map((device, i) => {
                const cfg = statusConfig[device.status]
                const StatusIcon = cfg.icon
                const CategoryIcon = categoryIcons[device.category] ?? HelpCircle
                return (
                  <Flex key={i} px={4} py={2} align="center"
                    borderBottom={i < state.compatibility!.devices.length - 1
                      ? `1px solid rgba(255,255,255,0.03)` : 'none'}
                    _hover={{ bg: 'rgba(255,255,255,0.02)' }}
                  >
                    <Box flex="0 0 110px">
                      <HStack display="inline-flex" gap={1.5} px="7px" py="2px" borderRadius="4px"
                        bg="rgba(255,255,255,0.04)" fontSize="10px" color={TS} fontWeight="600">
                        <CategoryIcon size={10} color="#666666" />
                        <Text>{device.category}</Text>
                      </HStack>
                    </Box>
                    <Text flex={1} color={T} fontWeight="500" fontSize="12px">{device.name}</Text>
                    <HStack flex="0 0 110px" gap={1.5}>
                      <StatusIcon size={12} color={cfg.color} />
                      <Text color={cfg.color} fontSize="11px" fontWeight="600">{t(cfg.labelKey)}</Text>
                    </HStack>
                    <Text flex={1} color={TS} fontSize="11px">{device.notes}</Text>
                  </Flex>
                )
              })}
            </Box>
          </Flex>
        ) : null}
      </Box>

      {/* ── Navigation ─────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" pt={5} mt={4} borderTop={`1px solid ${B}`}>
        <Box as="button" px={4} py="8px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
          onClick={() => navigate('/report')}
          display="flex" alignItems="center" gap={2} transition="all 0.15s"
        >
          <ChevronLeft size={14} /> {t('nav.report')}
        </Box>

        <Box as="button" px={5} py="8px" borderRadius="8px"
          bg={state.compatibility?.overallStatus !== 'unsupported' ? TEAL : 'rgba(255,255,255,0.06)'}
          color={state.compatibility?.overallStatus !== 'unsupported' ? '#0A0A0A' : TS}
          fontSize="13px" fontWeight="600"
          _hover={state.compatibility?.overallStatus !== 'unsupported' ? {
            bg: '#38E5CE', boxShadow: `0 0 20px rgba(45,212,191,0.3)`,
          } : {}}
          onClick={() => state.compatibility?.overallStatus !== 'unsupported' && navigate('/macos')}
          display="flex" alignItems="center" gap={2}
          opacity={state.compatibility ? 1 : 0.4}
          cursor={!state.compatibility || state.compatibility.overallStatus === 'unsupported' ? 'not-allowed' : 'pointer'}
          transition="all 0.15s"
        >
          {t('compatibility.selectMacOS')} <ChevronRight size={14} />
        </Box>
      </Flex>
    </Flex>
  )
}
