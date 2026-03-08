import { useEffect, useState } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex, Spinner, Input } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { AcpiPatch } from '../types'
import { CircuitBoard, Search, ChevronLeft, ChevronRight, Check, Lock } from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'
const TM = '#363B52'

const categoryColors: Record<string, string> = {
  Required:          '#EF4444',
  Recommended:       '#EAB308',
  Optional:          '#60A5FA',
  'Hardware-Specific': '#A78BFA',
}

export function AcpiPatchesPage() {
  const navigate   = useNavigate()
  const { state, dispatch } = useApp()
  const invoke     = usePhotinoInvoke()
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
    const cat = patch.category
    if (!acc[cat]) acc[cat] = []
    acc[cat].push(patch)
    return acc
  }, {} as Record<string, AcpiPatch[]>)

  const enabledCount = state.acpiPatches.filter((p) => p.enabled).length

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Flex justify="space-between" align="start">
          <Box>
            <HStack gap={3} mb={1.5}>
              <Flex w="34px" h="34px" borderRadius="8px" bg="rgba(45,212,191,0.12)" align="center" justify="center">
                <CircuitBoard size={17} color="#2DD4BF" />
              </Flex>
              <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">ACPI Patches</Heading>
            </HStack>
            <Text color={TS} fontSize="sm">Select ACPI tables (SSDTs) needed for your hardware.</Text>
          </Box>
          <Box px={3} py={1.5} borderRadius="8px" bg={`${AD}0.1)`}
            fontSize="xs" fontWeight="700" color={A} letterSpacing="-0.01em">
            {enabledCount} enabled
          </Box>
        </Flex>

        {/* Search */}
        <Box position="relative">
          <Box position="absolute" left={3} top="50%" transform="translateY(-50%)" zIndex={1}>
            <Search size={14} color={TS} />
          </Box>
          <Input
            pl={9} placeholder="Search patches…"
            value={filter} onChange={(e) => setFilter(e.target.value)}
            bg={S} border={`1px solid ${B}`} borderRadius="10px"
            color={T} fontSize="sm"
            _placeholder={{ color: TM }}
            _focus={{ borderColor: `${AD}0.4)`, outline: 'none' }}
          />
        </Box>

        {isLoading ? (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
            <VStack gap={4}><Spinner size="xl" color={A} /><Text color={TS} fontSize="sm">Loading patches…</Text></VStack>
          </Box>
        ) : (
          Object.entries(groupedPatches).map(([category, patches]) => (
            <Box key={category}>
              <HStack gap={2} mb={2.5}>
                <Box w="6px" h="6px" borderRadius="full" bg={categoryColors[category] ?? TS} />
                <Text fontSize="10px" fontWeight="700" color={TS} textTransform="uppercase" letterSpacing="0.08em">
                  {category}
                </Text>
              </HStack>
              <VStack gap={1.5} align="stretch">
                {patches.map((patch) => (
                  <Box
                    key={patch.id}
                    bg={patch.enabled ? `${AD}0.07)` : S}
                    border={patch.enabled ? `1px solid ${AD}0.25)` : `1px solid ${B}`}
                    borderRadius="10px" px={4} py={3}
                    cursor={patch.required ? 'default' : 'pointer'}
                    onClick={() => !patch.required && dispatch({ type: 'TOGGLE_ACPI_PATCH', id: patch.id })}
                    _hover={!patch.required ? { borderColor: `${AD}0.3)` } : {}}
                    transition="all 0.15s ease"
                  >
                    <Flex justify="space-between" align="center">
                      <HStack gap={3}>
                        {/* Checkbox */}
                        <Box
                          w="18px" h="18px" borderRadius="5px" flexShrink={0}
                          bg={patch.enabled ? A : 'transparent'}
                          border={patch.enabled ? 'none' : `2px solid rgba(255,255,255,0.12)`}
                          display="flex" alignItems="center" justifyContent="center"
                        >
                          {patch.enabled && <Check size={11} color="white" strokeWidth={3} />}
                        </Box>
                        <Box>
                          <HStack gap={2}>
                            <Text color={T} fontWeight="500" fontSize="sm">{patch.name}</Text>
                            {patch.required && <Lock size={11} color={TM} />}
                          </HStack>
                          <Text color={TS} fontSize="xs">{patch.description}</Text>
                        </Box>
                      </HStack>
                    </Flex>
                  </Box>
                ))}
              </VStack>
            </Box>
          ))
        )}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={() => navigate('/macos')} display="flex" alignItems="center" gap={2}>
            <ChevronLeft size={14} /> macOS
          </Box>
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg={A} color="white" fontSize="sm" fontWeight="600"
            _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
            onClick={() => navigate('/kexts')} display="flex" alignItems="center" gap={2}>
            Configure Kexts <ChevronRight size={14} />
          </Box>
        </Flex>
      </VStack>
    </Box>
  )
}