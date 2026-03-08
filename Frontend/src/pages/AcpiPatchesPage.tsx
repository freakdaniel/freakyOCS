import { useEffect, useState } from 'react'
import { Box, Text, HStack, Flex, Spinner, Input } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { AcpiPatch } from '../types'
import { Search, ChevronLeft, ChevronRight, Check, Lock } from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'

const categoryColors: Record<string, string> = {
  Required:            '#EF4444',
  Recommended:         '#EAB308',
  Optional:            '#60A5FA',
  'Hardware-Specific': '#A78BFA',
}

export function AcpiPatchesPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isLoading, setIsLoading] = useState(true)
  const [filter, setFilter]       = useState('')

  usePhotinoEvent<AcpiPatch[]>('acpi:patches', (data) => {
    setIsLoading(false)
    if (data) dispatch({ type: 'SET_ACPI_PATCHES', patches: data })
  })

  useEffect(() => {
    invoke('acpi:list', { report: state.report, macos: state.selectedMacOS })
  }, [invoke, state.report, state.selectedMacOS])

  const filteredPatches = state.acpiPatches.filter(
    (p) =>
      p.name.toLowerCase().includes(filter.toLowerCase()) ||
      p.description.toLowerCase().includes(filter.toLowerCase())
  )

  const groupedPatches = filteredPatches.reduce((acc, patch) => {
    if (!acc[patch.category]) acc[patch.category] = []
    acc[patch.category].push(patch)
    return acc
  }, {} as Record<string, AcpiPatch[]>)

  const enabledCount = state.acpiPatches.filter((p) => p.enabled).length

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" mb={5}>
        <Box>
          <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
            {t('acpi.title')}
          </Text>
          <Text color={TS} fontSize="13px">{t('acpi.subtitle')}</Text>
        </Box>
        <Box px={3} py="5px" borderRadius="7px"
          bg="rgba(45,212,191,0.08)" border="1px solid rgba(45,212,191,0.2)"
          fontSize="11px" fontWeight="700" color={TEAL}>
          {enabledCount} {t('acpi.enabled')}
        </Box>
      </Flex>

      {/* ── Search ─────────────────────────────────────────────────────── */}
      <Box position="relative" mb={4}>
        <Box position="absolute" left={3} top="50%" transform="translateY(-50%)" zIndex={1}>
          <Search size={13} color={TS} />
        </Box>
        <Input
          pl={9} placeholder={t('acpi.searchPlaceholder')}
          value={filter} onChange={(e) => setFilter(e.target.value)}
          bg={S} border={`1px solid ${B}`} borderRadius="9px"
          color={T} fontSize="13px" h="36px"
          _placeholder={{ color: TM }}
          _focus={{ borderColor: 'rgba(45,212,191,0.35)', outline: 'none', boxShadow: 'none' }}
        />
      </Box>

      {/* ── Patch list ─────────────────────────────────────────────────── */}
      <Box flex={1} overflowY="auto" minH={0}>
        {isLoading ? (
          <Flex h="200px" align="center" justify="center" gap={3}
            bg={S} border={`1px solid ${B}`} borderRadius="12px" direction="column"
          >
            <Spinner size="md" color={TEAL} borderWidth="2px" />
            <Text color={TS} fontSize="13px">{t('acpi.loading')}</Text>
          </Flex>
        ) : (
          <Flex direction="column" gap={5}>
            {Object.entries(groupedPatches).map(([category, patches]) => (
              <Box key={category}>
                <HStack gap={2} mb={2}>
                  <Box w="6px" h="6px" borderRadius="full"
                    bg={categoryColors[category] ?? TS} />
                  <Text fontSize="10px" fontWeight="700" color={TS}
                    textTransform="uppercase" letterSpacing="0.09em">
                    {category}
                  </Text>
                  <Text fontSize="10px" color={TM}>({patches.length})</Text>
                </HStack>

                <Flex direction="column" gap={1.5}>
                  {patches.map((patch) => (
                    <Box
                      key={patch.id}
                      bg={patch.enabled ? 'rgba(45,212,191,0.05)' : S}
                      border={`1px solid ${patch.enabled ? 'rgba(45,212,191,0.2)' : B}`}
                      borderRadius="10px" px={4} py="10px"
                      cursor={patch.required ? 'default' : 'pointer'}
                      onClick={() => !patch.required && dispatch({ type: 'TOGGLE_ACPI_PATCH', id: patch.id })}
                      _hover={!patch.required ? {
                        borderColor: patch.enabled ? 'rgba(45,212,191,0.35)' : 'rgba(255,255,255,0.1)',
                      } : {}}
                      transition="all 0.15s ease"
                    >
                      <HStack gap={3}>
                        <Flex
                          w="18px" h="18px" borderRadius="5px" flexShrink={0}
                          bg={patch.enabled ? (patch.required ? 'rgba(45,212,191,0.4)' : TEAL) : 'transparent'}
                          border={patch.enabled ? 'none' : '1.5px solid rgba(255,255,255,0.1)'}
                          align="center" justify="center"
                          transition="all 0.15s"
                          opacity={patch.required ? 0.5 : 1}
                        >
                          {patch.enabled && <Check size={11} color={patch.required ? '#0A0A0A' : '#0A0A0A'} strokeWidth={3} />}
                        </Flex>

                        <Box flex={1} minW={0}>
                          <HStack gap={2} mb="1px">
                            <Text color={patch.enabled ? T : TS} fontWeight="500" fontSize="13px">
                              {patch.name}
                            </Text>
                            {patch.required && <Lock size={10} color={TM} />}
                          </HStack>
                          <Text color={TM} fontSize="11px" truncate>{patch.description}</Text>
                        </Box>
                      </HStack>
                    </Box>
                  ))}
                </Flex>
              </Box>
            ))}
          </Flex>
        )}
      </Box>

      {/* ── Navigation ─────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" pt={5} mt={4} borderTop={`1px solid ${B}`}>
        <Box as="button" px={4} py="8px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
          onClick={() => navigate('/macos')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          <ChevronLeft size={14} /> {t('nav.macos')}
        </Box>
        <Box as="button" px={5} py="8px" borderRadius="8px"
          bg={TEAL} color="#0A0A0A" fontSize="13px" fontWeight="700"
          _hover={{ bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' }}
          onClick={() => navigate('/kexts')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          {t('acpi.configKexts')} <ChevronRight size={14} />
        </Box>
      </Flex>
    </Flex>
  )
}
