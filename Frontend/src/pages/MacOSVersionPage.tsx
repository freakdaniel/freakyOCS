import { useEffect, useCallback, useState } from 'react'
import {
  Box, Text, HStack, Flex, Spinner, Badge,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { MacOSVersion } from '../types'
import {
  CheckCircle2, AlertTriangle, XCircle, ChevronLeft, ChevronRight,
} from 'lucide-react'
import { MacOSIcon } from '../components/MacOSIcon'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'

export function MacOSVersionPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [versions, setVersions] = useState<MacOSVersion[]>([])
  const [isLoading, setIsLoading] = useState(true)

  usePhotinoEvent<MacOSVersion[]>('macos:versions', (data) => {
    setIsLoading(false)
    if (data) setVersions(data)
  })

  useEffect(() => {
    invoke('macos:list', { report: state.report })
  }, [invoke, state.report])

  const handleSelect = useCallback((version: MacOSVersion) => {
    dispatch({ type: 'SET_MACOS', version })
  }, [dispatch])

  const hasHardware = !!state.report

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Box mb={5}>
        <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
          {t('macos.title')}
        </Text>
        <Text color={TS} fontSize="13px">
          {hasHardware ? t('macos.compatBased') : t('macos.scanFirst')}
        </Text>
      </Box>

      {/* ── Version list ───────────────────────────────────────────────── */}
      {/* px={1} gives room for box-shadow not to be clipped */}
      <Box flex={1} overflowY="auto" minH={0} px={1}>
        {isLoading ? (
          <Flex
            h="200px" align="center" justify="center" gap={3}
            bg={S} border={`1px solid ${B}`} borderRadius="12px" direction="column"
          >
            <Spinner size="md" color={TEAL} borderWidth="2px" />
            <Text color={TS} fontSize="13px">{t('macos.loading')}</Text>
          </Flex>
        ) : (
          <Box
            display="grid"
            gridTemplateColumns="repeat(auto-fill, minmax(260px, 1fr))"
            gap={3} pb={2}
          >
            {[...versions].reverse().map((version) => {
              const isSelected  = state.selectedMacOS?.version === version.version
              const isDisabled  = !version.supported
              const hasWarnings = (version.warnings?.length ?? 0) > 0

              return (
                <Box
                  key={version.version}
                  bg={isSelected ? 'rgba(45,212,191,0.07)' : S}
                  border={`1px solid ${
                    isSelected
                      ? 'rgba(45,212,191,0.35)'
                      : isDisabled
                        ? 'rgba(239,68,68,0.15)'
                        : B
                  }`}
                  borderRadius="12px"
                  p={4}
                  opacity={isDisabled ? 0.45 : 1}
                  cursor={isDisabled ? 'not-allowed' : 'pointer'}
                  onClick={() => !isDisabled && handleSelect(version)}
                  _hover={!isDisabled ? {
                    borderColor: isSelected
                      ? 'rgba(45,212,191,0.55)'
                      : 'rgba(255,255,255,0.12)',
                    bg: isSelected ? 'rgba(45,212,191,0.1)' : '#161616',
                  } : {}}
                  transition="all 0.18s ease"
                  position="relative"
                >
                  <HStack gap={3} align="center">
                    <MacOSIcon name={version.name} size={44} radius="11px" />

                    <Box flex={1} minW={0}>
                      <HStack gap={2} mb="2px">
                        <Text fontSize="14px" fontWeight="600"
                          color={isSelected ? TEAL : T} lineHeight={1}>
                          macOS {version.name}
                        </Text>
                        <Text fontSize="11px" fontWeight="500" color={TS}
                          bg="rgba(255,255,255,0.05)" px="6px" py="1px" borderRadius="4px">
                          {version.version}
                        </Text>
                      </HStack>

                      <HStack gap={1} mt="5px" flexWrap="wrap">
                        {!hasHardware ? (
                          <Badge
                            size="sm" px="6px" py="2px" borderRadius="4px"
                            bg="rgba(255,255,255,0.04)" color={TS}
                            fontSize="10px" fontWeight="500" border="none"
                          >
                            {t('compatibility.unknown')}
                          </Badge>
                        ) : isDisabled ? (
                          <HStack gap={1} px={2} py="2px" borderRadius="4px"
                            bg="rgba(239,68,68,0.08)" border="1px solid rgba(239,68,68,0.15)">
                            <XCircle size={10} color="#EF4444" />
                            <Text color="#EF4444" fontSize="10px" fontWeight="600">{t('macos.unsupported')}</Text>
                          </HStack>
                        ) : hasWarnings ? (
                          <HStack gap={1} px={2} py="2px" borderRadius="4px"
                            bg="rgba(234,179,8,0.08)" border="1px solid rgba(234,179,8,0.15)">
                            <AlertTriangle size={10} color="#EAB308" />
                            <Text color="#EAB308" fontSize="10px" fontWeight="600">{t('macos.partial')}</Text>
                          </HStack>
                        ) : (
                          <HStack gap={1} px={2} py="2px" borderRadius="4px"
                            bg="rgba(45,212,191,0.08)" border="1px solid rgba(45,212,191,0.2)">
                            <CheckCircle2 size={10} color={TEAL} />
                            <Text color={TEAL} fontSize="10px" fontWeight="600">{t('macos.compatible')}</Text>
                          </HStack>
                        )}
                      </HStack>
                    </Box>

                    {/* Selected radio indicator */}
                    <Box
                      w="18px" h="18px" borderRadius="full" flexShrink={0}
                      border={`2px solid ${isSelected ? TEAL : 'rgba(255,255,255,0.1)'}`}
                      bg={isSelected ? TEAL : 'transparent'}
                      display="flex" alignItems="center" justifyContent="center"
                      transition="all 0.15s"
                    >
                      {isSelected && <Box w="6px" h="6px" borderRadius="full" bg="#0A0A0A" />}
                    </Box>
                  </HStack>
                </Box>
              )
            })}
          </Box>
        )}
      </Box>

      {/* ── Selected confirmation + Navigation ─────────────────────────── */}
      <Box pt={5} borderTop={`1px solid ${B}`} mt={4}>
        {state.selectedMacOS && (
          <HStack gap={2} mb={4} px={3} py="8px" borderRadius="8px"
            bg="rgba(45,212,191,0.06)" border="1px solid rgba(45,212,191,0.2)"
            display="inline-flex"
          >
            <CheckCircle2 size={13} color={TEAL} />
            <Text color={TEAL} fontSize="12px" fontWeight="500">
              {t('macos.selectedLabel')}:{' '}
              <Text as="span" fontWeight="700">macOS {state.selectedMacOS.name}</Text>
              {' '}({state.selectedMacOS.version})
            </Text>
          </HStack>
        )}

        <Flex justify="space-between" align="center">
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={() => navigate('/compatibility')}
            display="flex" alignItems="center" gap={2} transition="all 0.15s"
          >
            <ChevronLeft size={14} /> {t('nav.compatibility')}
          </Box>

          <Box as="button" px={5} py="8px" borderRadius="8px"
            bg={state.selectedMacOS ? TEAL : 'rgba(255,255,255,0.06)'}
            color={state.selectedMacOS ? '#0A0A0A' : TS}
            fontSize="13px" fontWeight="600"
            _hover={state.selectedMacOS ? {
              bg: '#38E5CE', boxShadow: `0 0 20px rgba(45,212,191,0.3)`,
            } : {}}
            onClick={() => state.selectedMacOS && navigate('/acpi')}
            display="flex" alignItems="center" gap={2}
            opacity={state.selectedMacOS ? 1 : 0.4}
            cursor={state.selectedMacOS ? 'pointer' : 'not-allowed'}
            transition="all 0.15s"
          >
            {t('macos.configAcpi')} <ChevronRight size={14} />
          </Box>
        </Flex>
      </Box>
    </Flex>
  )
}
