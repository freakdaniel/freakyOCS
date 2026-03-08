import { useEffect, useState } from 'react'
import { Box, Text, HStack, Flex, Spinner, Input } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { KextInfo } from '../types'
import { Search, ChevronLeft, ChevronRight, Check, Lock } from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'

const categoryColors: Record<string, string> = {
  Core:     TEAL,
  Audio:    '#EAB308',
  Graphics: '#A78BFA',
  Network:  '#2DD4BF',
  Storage:  '#F97316',
  USB:      '#22C55E',
  Other:    '#888888',
}

export function KextsPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isLoading, setIsLoading] = useState(true)
  const [filter, setFilter]       = useState('')
  const [activeTab, setActiveTab] = useState('all')

  usePhotinoEvent<KextInfo[]>('kexts:list', (data) => {
    setIsLoading(false)
    if (data) dispatch({ type: 'SET_KEXTS', kexts: data })
  })

  useEffect(() => {
    invoke('kexts:list', { report: state.report, macos: state.selectedMacOS })
  }, [invoke, state.report, state.selectedMacOS])

  const categories    = ['all', ...new Set(state.kexts.map((k) => k.category))]
  const filteredKexts = state.kexts.filter((k) => {
    const matchesFilter   = k.name.toLowerCase().includes(filter.toLowerCase()) ||
                            k.description.toLowerCase().includes(filter.toLowerCase())
    const matchesCategory = activeTab === 'all' || k.category === activeTab
    return matchesFilter && matchesCategory
  })
  const enabledCount = state.kexts.filter((k) => k.enabled).length

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" mb={4}>
        <Box>
          <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
            {t('kexts.title')}
          </Text>
          <Text color={TS} fontSize="13px">{t('kexts.subtitle')}</Text>
        </Box>
        <Box px={3} py="5px" borderRadius="7px"
          bg="rgba(45,212,191,0.08)" border="1px solid rgba(45,212,191,0.2)"
          fontSize="11px" fontWeight="700" color={TEAL}>
          {enabledCount} {t('kexts.enabled')}
        </Box>
      </Flex>

      {/* ── Search ─────────────────────────────────────────────────────── */}
      <Box position="relative" mb={3}>
        <Box position="absolute" left={3} top="50%" transform="translateY(-50%)" zIndex={1}>
          <Search size={13} color={TS} />
        </Box>
        <Input
          pl={9} placeholder={t('kexts.searchPlaceholder')}
          value={filter} onChange={(e) => setFilter(e.target.value)}
          bg={S} border={`1px solid ${B}`} borderRadius="9px"
          color={T} fontSize="13px" h="36px"
          _placeholder={{ color: TM }}
          _focus={{ borderColor: 'rgba(45,212,191,0.35)', outline: 'none', boxShadow: 'none' }}
        />
      </Box>

      {/* ── Category tabs ───────────────────────────────────────────────── */}
      <HStack gap={1.5} mb={4} flexWrap="wrap">
        {categories.map((cat) => (
          <Box
            key={cat} as="button"
            px={3} py="4px" borderRadius="6px"
            bg={activeTab === cat ? 'rgba(45,212,191,0.1)' : 'rgba(255,255,255,0.03)'}
            border={`1px solid ${activeTab === cat ? 'rgba(45,212,191,0.25)' : 'transparent'}`}
            color={activeTab === cat ? TEAL : TS}
            fontSize="11px" fontWeight="600" textTransform="capitalize"
            onClick={() => setActiveTab(cat)}
            _hover={{ color: T }} transition="all 0.15s"
          >
            {cat === 'all' ? t('kexts.all') : cat}
          </Box>
        ))}
      </HStack>

      {/* ── Kext list ───────────────────────────────────────────────────── */}
      <Box flex={1} overflowY="auto" minH={0}>
        {isLoading ? (
          <Flex h="200px" align="center" justify="center" gap={3}
            bg={S} border={`1px solid ${B}`} borderRadius="12px" direction="column"
          >
            <Spinner size="md" color={TEAL} borderWidth="2px" />
            <Text color={TS} fontSize="13px">{t('kexts.loading')}</Text>
          </Flex>
        ) : (
          <Flex direction="column" gap={1.5}>
            {filteredKexts.map((kext) => (
              <Box
                key={kext.id}
                bg={kext.enabled ? 'rgba(45,212,191,0.04)' : S}
                border={`1px solid ${kext.enabled ? 'rgba(45,212,191,0.18)' : B}`}
                borderRadius="10px" px={4} py="10px"
                cursor={kext.required ? 'default' : 'pointer'}
                onClick={() => !kext.required && dispatch({ type: 'TOGGLE_KEXT', id: kext.id })}
                _hover={!kext.required ? {
                  borderColor: kext.enabled ? 'rgba(45,212,191,0.3)' : 'rgba(255,255,255,0.1)',
                } : {}}
                transition="all 0.15s ease"
              >
                <Flex justify="space-between" align="center">
                  <HStack gap={3}>
                    <Flex
                      w="18px" h="18px" borderRadius="5px" flexShrink={0}
                      bg={kext.enabled ? TEAL : 'transparent'}
                      border={kext.enabled ? 'none' : '1.5px solid rgba(255,255,255,0.1)'}
                      align="center" justify="center"
                      transition="all 0.15s"
                    >
                      {kext.enabled && <Check size={11} color="#0A0A0A" strokeWidth={3} />}
                    </Flex>
                    <Box minW={0}>
                      <HStack gap={2} mb="1px">
                        <Text color={kext.enabled ? T : TS} fontWeight="500" fontSize="13px">
                          {kext.name}
                        </Text>
                        {kext.required && <Lock size={10} color={TM} />}
                      </HStack>
                      <Text color={TM} fontSize="11px" truncate>{kext.description}</Text>
                    </Box>
                  </HStack>
                  <Box
                    px="7px" py="2px" borderRadius="5px" flexShrink={0}
                    bg={`${categoryColors[kext.category] ?? TS}18`}
                    fontSize="10px" fontWeight="600"
                    color={categoryColors[kext.category] ?? TS}
                  >
                    {kext.category}
                  </Box>
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
          onClick={() => navigate('/acpi')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          <ChevronLeft size={14} /> {t('nav.acpi')}
        </Box>
        <Box as="button" px={5} py="8px" borderRadius="8px"
          bg={TEAL} color="#0A0A0A" fontSize="13px" fontWeight="700"
          _hover={{ bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' }}
          onClick={() => navigate('/smbios')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          {t('kexts.configSmbios')} <ChevronRight size={14} />
        </Box>
      </Flex>
    </Flex>
  )
}
