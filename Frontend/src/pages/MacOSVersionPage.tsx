import { useEffect, useCallback, useState } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex, Spinner } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { MacOSVersion } from '../types'
import { Apple, CheckCircle2, AlertTriangle, XCircle, ChevronLeft, ChevronRight } from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'
const TM = '#363B52'

export function MacOSVersionPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [versions, setVersions] = useState<MacOSVersion[]>([])
  const [isLoading, setIsLoading] = useState(true)

  usePhotinoEvent<MacOSVersion[]>('macos:versions', (data) => {
    setIsLoading(false)
    if (data) setVersions(data)
  })

  useEffect(() => { invoke('macos:list', { report: state.report }) }, [invoke, state.report])

  const handleSelect = useCallback((version: MacOSVersion) => {
    dispatch({ type: 'SET_MACOS', version })
  }, [dispatch])

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Box>
          <HStack gap={3} mb={1.5}>
            <Flex w="34px" h="34px" borderRadius="8px" bg="rgba(167,139,250,0.12)" align="center" justify="center">
              <Apple size={17} color="#A78BFA" />
            </Flex>
            <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">macOS Version</Heading>
          </HStack>
          <Text color={TS} fontSize="sm">Select the macOS version you want to install.</Text>
        </Box>

        {isLoading ? (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
            <VStack gap={4}>
              <Spinner size="xl" color={A} />
              <Text color={TS} fontSize="sm">Loading versions…</Text>
            </VStack>
          </Box>
        ) : (
          <VStack gap={2} align="stretch">
            {versions.map((version) => {
              const isSelected = state.selectedMacOS?.version === version.version
              const isDisabled = !version.supported
              return (
                <Box
                  key={version.version}
                  bg={isSelected ? `${AD}0.08)` : S}
                  border={isSelected ? `1px solid ${AD}0.3)` : `1px solid ${B}`}
                  borderRadius="10px" px={5} py={3.5}
                  opacity={isDisabled ? 0.4 : 1}
                  cursor={isDisabled ? 'not-allowed' : 'pointer'}
                  onClick={() => !isDisabled && handleSelect(version)}
                  _hover={!isDisabled ? { borderColor: isSelected ? `${AD}0.5)` : 'rgba(255,255,255,0.13)' } : {}}
                  transition="all 0.15s ease"
                >
                  <Flex justify="space-between" align="center">
                    <HStack gap={4}>
                      {/* Radio dot */}
                      <Box
                        w="16px" h="16px" borderRadius="full" flexShrink={0}
                        borderWidth="2px"
                        borderColor={isSelected ? A : 'rgba(255,255,255,0.14)'}
                        bg={isSelected ? A : 'transparent'}
                        display="flex" alignItems="center" justifyContent="center"
                      >
                        {isSelected && <Box w="6px" h="6px" borderRadius="full" bg="white" />}
                      </Box>
                      <Box>
                        <HStack gap={2}>
                          <Text color={T} fontWeight="600" fontSize="sm">macOS {version.name}</Text>
                          <Text color={TM} fontSize="xs" fontWeight="500">{version.version}</Text>
                        </HStack>
                        <Text color={TS} fontSize="xs">Darwin {version.darwin}</Text>
                      </Box>
                    </HStack>
                    <HStack gap={2}>
                      {version.warnings?.map((w, i) => (
                        <HStack key={i} gap={1} px={2} py={0.5} borderRadius="5px" bg="rgba(234,179,8,0.1)">
                          <AlertTriangle size={11} color="#EAB308" />
                          <Text color="#EAB308" fontSize="10px" fontWeight="600">{w}</Text>
                        </HStack>
                      ))}
                      {!version.supported ? (
                        <HStack gap={1} px={2} py={0.5} borderRadius="5px" bg="rgba(239,68,68,0.1)">
                          <XCircle size={11} color="#EF4444" />
                          <Text color="#EF4444" fontSize="10px" fontWeight="600">Unsupported</Text>
                        </HStack>
                      ) : (
                        <HStack gap={1} px={2} py={0.5} borderRadius="5px" bg="rgba(34,197,94,0.1)">
                          <CheckCircle2 size={11} color="#22C55E" />
                          <Text color="#22C55E" fontSize="10px" fontWeight="600">Compatible</Text>
                        </HStack>
                      )}
                    </HStack>
                  </Flex>
                </Box>
              )
            })}
          </VStack>
        )}

        {/* Selected confirmation */}
        {state.selectedMacOS && (
          <HStack gap={2} px={4} py={3} borderRadius="10px"
            bg={`${AD}0.06)`} border={`1px solid ${AD}0.2)`}>
            <CheckCircle2 size={15} color={A} />
            <Text color="#C7D2FE" fontSize="sm">
              Selected: <strong>macOS {state.selectedMacOS.name}</strong> ({state.selectedMacOS.version})
            </Text>
          </HStack>
        )}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={() => navigate('/compatibility')} display="flex" alignItems="center" gap={2}>
            <ChevronLeft size={14} /> Compatibility
          </Box>
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg={A} color="white" fontSize="sm" fontWeight="600"
            _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
            onClick={() => navigate('/acpi')} display="flex" alignItems="center" gap={2}
            opacity={state.selectedMacOS ? 1 : 0.4}
            pointerEvents={state.selectedMacOS ? 'auto' : 'none'}>
            Configure ACPI <ChevronRight size={14} />
          </Box>
        </Flex>
      </VStack>
    </Box>
  )
}