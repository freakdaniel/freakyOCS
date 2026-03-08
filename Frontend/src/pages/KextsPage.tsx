import { useEffect, useState } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex, Spinner, Input } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { KextInfo } from '../types'
import { Package, Search, ChevronLeft, ChevronRight, Check, Lock } from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'
const TM = '#363B52'

const categoryColors: Record<string, string> = {
  Core:    A,
  Audio:   '#EAB308',
  Graphics:'#A78BFA',
  Network: '#2DD4BF',
  Storage: '#F97316',
  USB:     '#22C55E',
  Other:   TS,
}

export function KextsPage() {
  const navigate = useNavigate()
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
    const matchesFilter   = k.name.toLowerCase().includes(filter.toLowerCase()) || k.description.toLowerCase().includes(filter.toLowerCase())
    const matchesCategory = activeTab === 'all' || k.category === activeTab
    return matchesFilter && matchesCategory
  })
  const enabledCount = state.kexts.filter((k) => k.enabled).length

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Flex justify="space-between" align="start">
          <Box>
            <HStack gap={3} mb={1.5}>
              <Flex w="34px" h="34px" borderRadius="8px" bg={`${AD}0.12)`} align="center" justify="center">
                <Package size={17} color={A} />
              </Flex>
              <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">Kernel Extensions</Heading>
            </HStack>
            <Text color={TS} fontSize="sm">Configure kexts for your hardware.</Text>
          </Box>
          <Box px={3} py={1.5} borderRadius="8px" bg={`${AD}0.1)`}
            fontSize="xs" fontWeight="700" color={A}>
            {enabledCount} enabled
          </Box>
        </Flex>

        {/* Search & Tabs */}
        <VStack gap={3} align="stretch">
          <Box position="relative">
            <Box position="absolute" left={3} top="50%" transform="translateY(-50%)" zIndex={1}>
              <Search size={14} color={TS} />
            </Box>
            <Input
              pl={9} placeholder="Search kexts…"
              value={filter} onChange={(e) => setFilter(e.target.value)}
              bg={S} border={`1px solid ${B}`} borderRadius="10px"
              color={T} fontSize="sm"
              _placeholder={{ color: TM }}
              _focus={{ borderColor: `${AD}0.4)`, outline: 'none' }}
            />
          </Box>
          <HStack gap={1.5} flexWrap="wrap">
            {categories.map((cat) => (
              <Box
                key={cat} as="button"
                px={3} py="5px" borderRadius="7px"
                bg={activeTab === cat ? `${AD}0.14)` : 'rgba(255,255,255,0.03)'}
                border={activeTab === cat ? `1px solid ${AD}0.28)` : '1px solid transparent'}
                color={activeTab === cat ? A : TS}
                fontSize="11px" fontWeight="600"
                textTransform="capitalize"
                onClick={() => setActiveTab(cat)}
                _hover={{ color: T }} transition="all 0.15s"
              >
                {cat}
              </Box>
            ))}
          </HStack>
        </VStack>

        {isLoading ? (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
            <VStack gap={4}><Spinner size="xl" color={A} /><Text color={TS} fontSize="sm">Loading kexts…</Text></VStack>
          </Box>
        ) : (
          <VStack gap={1.5} align="stretch">
            {filteredKexts.map((kext) => (
              <Box
                key={kext.id}
                bg={kext.enabled ? `${AD}0.07)` : S}
                border={kext.enabled ? `1px solid ${AD}0.22)` : `1px solid ${B}`}
                borderRadius="10px" px={4} py={3}
                cursor={kext.required ? 'default' : 'pointer'}
                onClick={() => !kext.required && dispatch({ type: 'TOGGLE_KEXT', id: kext.id })}
                _hover={!kext.required ? { borderColor: `${AD}0.3)` } : {}}
                transition="all 0.15s ease"
              >
                <Flex justify="space-between" align="center">
                  <HStack gap={3}>
                    <Box
                      w="18px" h="18px" borderRadius="5px" flexShrink={0}
                      bg={kext.enabled ? A : 'transparent'}
                      border={kext.enabled ? 'none' : '2px solid rgba(255,255,255,0.12)'}
                      display="flex" alignItems="center" justifyContent="center"
                    >
                      {kext.enabled && <Check size={11} color="white" strokeWidth={3} />}
                    </Box>
                    <Box>
                      <HStack gap={2}>
                        <Text color={T} fontWeight="500" fontSize="sm">{kext.name}</Text>
                        <Text color={TM} fontSize="xs">{kext.version}</Text>
                        {kext.required && <Lock size={11} color={TM} />}
                      </HStack>
                      <Text color={TS} fontSize="xs">{kext.description}</Text>
                    </Box>
                  </HStack>
                  <Box
                    px={2} py={0.5} borderRadius="5px"
                    bg={`${categoryColors[kext.category] ?? TS}18`}
                    fontSize="10px" fontWeight="600"
                    color={categoryColors[kext.category] ?? TS}
                  >
                    {kext.category}
                  </Box>
                </Flex>
              </Box>
            ))}
          </VStack>
        )}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={() => navigate('/acpi')} display="flex" alignItems="center" gap={2}>
            <ChevronLeft size={14} /> ACPI
          </Box>
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg={A} color="white" fontSize="sm" fontWeight="600"
            _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
            onClick={() => navigate('/smbios')} display="flex" alignItems="center" gap={2}>
            Configure SMBIOS <ChevronRight size={14} />
          </Box>
        </Flex>
      </VStack>
    </Box>
  )
}