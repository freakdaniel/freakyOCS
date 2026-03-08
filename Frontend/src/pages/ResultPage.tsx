import { useCallback } from 'react'
import { Box, Text, HStack, Flex } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke } from '../bridge/usePhotino'
import {
  PartyPopper, XCircle, FolderOpen, Settings, ListChecks,
  BarChart3, ChevronLeft, RotateCcw, AlertTriangle,
} from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'

export function ResultPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()

  const openFolder = useCallback(() => {
    if (state.buildResult?.outputPath) {
      invoke('result:open-folder', { path: state.buildResult.outputPath })
    }
  }, [invoke, state.buildResult])

  const startNew = useCallback(() => {
    dispatch({ type: 'RESET' })
    navigate('/')
  }, [dispatch, navigate])

  if (!state.buildResult) {
    return (
      <Flex direction="column" h="100vh" bg={BG} px={7} py={6}>
        <Box mb={5}>
          <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
            {t('result.title')}
          </Text>
        </Box>
        <Flex flex={1} align="center" justify="center" direction="column" gap={4}
          bg={S} border={`1px solid ${B}`} borderRadius="14px"
        >
          <FolderOpen size={32} color={TS} />
          <Text color={T} fontWeight="600" fontSize="14px">{t('result.noBuildResult')}</Text>
          <Text color={TS} fontSize="13px">{t('result.completeFirst')}</Text>
          <Box as="button" px={5} py="9px" borderRadius="9px"
            bg={TEAL} color="#0A0A0A" fontSize="13px" fontWeight="700"
            _hover={{ bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' }}
            onClick={() => navigate('/build')} transition="all 0.15s"
          >
            {t('result.goToBuild')}
          </Box>
        </Flex>
      </Flex>
    )
  }

  const result = state.buildResult

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Box mb={5}>
        <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
          {t('result.title')}
        </Text>
      </Box>

      <Box flex={1} overflowY="auto" minH={0}>
        <Flex direction="column" gap={4}>

          {/* ── Status banner ──────────────────────────────────────────── */}
          <Box
            bg={result.success ? 'rgba(45,212,191,0.05)' : 'rgba(239,68,68,0.05)'}
            border={result.success ? '1px solid rgba(45,212,191,0.2)' : '1px solid rgba(239,68,68,0.2)'}
            borderRadius="14px" py={7}
          >
            <Flex direction="column" align="center" gap={3}>
              {result.success
                ? <PartyPopper size={34} color={TEAL} />
                : <XCircle size={34} color="#EF4444" />
              }
              <Text
                fontSize="20px" fontWeight="700" letterSpacing="-0.02em"
                color={result.success ? TEAL : '#EF4444'}
              >
                {result.success ? t('result.success') : t('result.failed')}
              </Text>
              {result.success && (
                <Text color={TS} fontSize="13px">{t('result.successDesc')}</Text>
              )}
            </Flex>
          </Box>

          {/* ── Output path ─────────────────────────────────────────────── */}
          {result.success && (
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
                <HStack gap={2}>
                  <FolderOpen size={14} color={TEAL} />
                  <Text color={T} fontWeight="600" fontSize="13px">{t('result.outputLocation')}</Text>
                </HStack>
                <Box as="button" px={3} py="5px" borderRadius="6px"
                  bg="rgba(45,212,191,0.08)" color={TEAL}
                  fontSize="11px" fontWeight="600"
                  _hover={{ bg: 'rgba(45,212,191,0.14)' }}
                  onClick={openFolder} transition="all 0.15s"
                >
                  {t('result.openFolder')}
                </Box>
              </Flex>
              <Box px={5} py={3}>
                <Text color={T} fontFamily="mono" fontSize="12px"
                  bg="rgba(255,255,255,0.03)" px={3} py={2} borderRadius="6px">
                  {result.outputPath}
                </Text>
              </Box>
            </Box>
          )}

          {/* ── BIOS Settings ───────────────────────────────────────────── */}
          {result.biosSettings && result.biosSettings.length > 0 && (
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
                <HStack gap={2}>
                  <Settings size={14} color={TEAL} />
                  <Text color={T} fontWeight="600" fontSize="13px">{t('result.biosSettings')}</Text>
                </HStack>
                <HStack gap={1} px={2} py="3px" borderRadius="5px" bg="rgba(234,179,8,0.08)">
                  <AlertTriangle size={10} color="#EAB308" />
                  <Text fontSize="10px" color="#EAB308" fontWeight="600">{t('result.requiredForMacos')}</Text>
                </HStack>
              </Flex>
              <Flex direction="column">
                <Flex px={5} py={2} borderBottom={`1px solid rgba(255,255,255,0.04)`}
                  bg="rgba(255,255,255,0.02)">
                  <Text flex={2} fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.06em">Setting</Text>
                  <Text flex={1} fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.06em">Category</Text>
                  <Text flex={1} fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.06em">Value</Text>
                  <Text w="70px" fontSize="10px" color={TS} fontWeight="700" textTransform="uppercase" letterSpacing="0.06em" textAlign="right">Status</Text>
                </Flex>
                {result.biosSettings.map((setting, i) => (
                  <Flex key={i} px={5} py="9px" align="center"
                    borderBottom={`1px solid rgba(255,255,255,0.03)`}
                    _hover={{ bg: 'rgba(255,255,255,0.01)' }}
                  >
                    <Text flex={2} color={T} fontSize="12px" fontWeight="500">{setting.name}</Text>
                    <Box flex={1}>
                      <Box display="inline-block" px="6px" py="1px" borderRadius="4px"
                        bg="rgba(255,255,255,0.04)" fontSize="10px" color={TS}>
                        {setting.category}
                      </Box>
                    </Box>
                    <Box flex={1}>
                      <Box display="inline-block" px="6px" py="1px" borderRadius="4px"
                        bg={setting.recommended === 'Enabled' ? 'rgba(45,212,191,0.08)' : 'rgba(239,68,68,0.08)'}
                        fontSize="10px" fontWeight="600"
                        color={setting.recommended === 'Enabled' ? TEAL : '#EF4444'}
                      >
                        {setting.recommended}
                      </Box>
                    </Box>
                    <Text w="70px" textAlign="right" fontSize="10px" fontWeight="600"
                      color={setting.required ? '#EF4444' : TS}>
                      {setting.required ? t('result.required') : t('result.optional')}
                    </Text>
                  </Flex>
                ))}
              </Flex>
            </Box>
          )}

          {/* ── Next Steps ──────────────────────────────────────────────── */}
          {result.nextSteps && result.nextSteps.length > 0 && (
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
                <HStack gap={2}>
                  <ListChecks size={14} color={TEAL} />
                  <Text color={T} fontWeight="600" fontSize="13px">{t('result.nextSteps')}</Text>
                </HStack>
              </Box>
              <Flex direction="column" px={5} py={2}>
                {result.nextSteps.map((step, i, arr) => (
                  <HStack key={i} gap={3} py="9px"
                    borderBottom={i < arr.length - 1 ? `1px solid rgba(255,255,255,0.04)` : 'none'}>
                    <Flex w="20px" h="20px" borderRadius="full" flexShrink={0}
                      bg="rgba(45,212,191,0.1)" align="center" justify="center">
                      <Text fontSize="10px" color={TEAL} fontWeight="700">{i + 1}</Text>
                    </Flex>
                    <Text color={T} fontSize="13px">{step}</Text>
                  </HStack>
                ))}
              </Flex>
            </Box>
          )}

          {/* ── Config Summary ──────────────────────────────────────────── */}
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
              <HStack gap={2}>
                <BarChart3 size={14} color={TEAL} />
                <Text color={T} fontWeight="600" fontSize="13px">{t('result.configSummary')}</Text>
              </HStack>
            </Box>
            <Box display="grid" gridTemplateColumns="repeat(2, 1fr)" gap={2} p={4}>
              {[
                { label: 'macOS',        value: state.selectedMacOS?.name,      color: TEAL },
                { label: 'SMBIOS',       value: state.smbios?.model,            color: '#38BDF8' },
                { label: t('build.acpiPatches'), value: `${state.acpiPatches.filter((p) => p.enabled).length}`, color: '#22C55E' },
                { label: t('build.kexts'),       value: `${state.kexts.filter((k) => k.enabled).length}`,       color: '#f97316' },
                { label: t('build.usbPorts'),    value: `${state.usbControllers.reduce((a, c) => a + c.ports.filter((p) => p.selected).length, 0)}`, color: '#EAB308' },
                { label: 'CPU',          value: state.report?.cpu.codename,     color: TS },
              ].map((item) => (
                <Flex key={item.label} justify="space-between" align="center"
                  px={3} py="8px" bg="rgba(255,255,255,0.02)" borderRadius="8px">
                  <Text color={TS} fontSize="12px">{item.label}</Text>
                  <Box px="7px" py="2px" borderRadius="4px"
                    bg={`${item.color}15`} fontSize="10px" fontWeight="600" color={item.color}>
                    {item.value ?? t('common.na')}
                  </Box>
                </Flex>
              ))}
            </Box>
          </Box>
        </Flex>
      </Box>

      {/* ── Actions ────────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" pt={5} mt={4} borderTop={`1px solid ${B}`}>
        <Box as="button" px={4} py="8px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
          onClick={() => navigate('/build')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          <ChevronLeft size={14} /> {t('result.backToBuild')}
        </Box>
        <HStack gap={2}>
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={startNew} display="flex" alignItems="center" gap={2}
            transition="all 0.15s">
            <RotateCcw size={12} /> {t('result.newBuild')}
          </Box>
          {result.success && (
            <Box as="button" px={5} py="8px" borderRadius="8px"
              bg={TEAL} color="#0A0A0A" fontSize="13px" fontWeight="700"
              _hover={{ bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' }}
              onClick={openFolder} display="flex" alignItems="center" gap={2}
              transition="all 0.15s">
              <FolderOpen size={13} /> {t('result.openEfi')}
            </Box>
          )}
        </HStack>
      </Flex>
    </Flex>
  )
}
