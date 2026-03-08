import { useCallback } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke } from '../bridge/usePhotino'
import {
  PartyPopper, XCircle, FolderOpen, Settings, ListChecks,
  BarChart3, ChevronLeft, RotateCcw, AlertTriangle,
} from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'

export function ResultPage() {
  const navigate = useNavigate()
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
      <Box maxW="860px" mx="auto">
        <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
          <VStack gap={4}>
            <FolderOpen size={32} color={TS} />
            <Heading size="md" color={T}>No Build Result</Heading>
            <Text color={TS} fontSize="sm">Complete a build first to see results.</Text>
            <Box
              as="button" px={5} py={2} borderRadius="8px"
              bg={A} color="white" fontSize="sm" fontWeight="600"
              _hover={{ bg: '#8F93FF' }}
              onClick={() => navigate('/build')}
            >
              Go to Build
            </Box>
          </VStack>
        </Box>
      </Box>
    )
  }

  const result = state.buildResult

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Success / Fail Banner */}
        <Box
          bg={result.success ? 'rgba(34,197,94,0.05)' : 'rgba(239,68,68,0.05)'}
          border={result.success ? '1px solid rgba(34,197,94,0.15)' : '1px solid rgba(239,68,68,0.15)'}
          borderRadius="12px" py={8}
        >
          <VStack gap={3}>
            {result.success ? <PartyPopper size={36} color="#22C55E" /> : <XCircle size={36} color="#EF4444" />}
            <Heading size="lg" color={result.success ? '#22C55E' : '#EF4444'} letterSpacing="-0.02em">
              {result.success ? 'Build Successful!' : 'Build Failed'}
            </Heading>
            {result.success && <Text color={TS} fontSize="sm">Your OpenCore EFI folder has been created.</Text>}
          </VStack>
        </Box>

        {/* Output Path */}
        {result.success && (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
              <HStack gap={2}>
                <FolderOpen size={15} color={A} />
                <Text color={T} fontWeight="600" fontSize="sm">Output Location</Text>
              </HStack>
              <Box
                as="button" px={3} py={1.5} borderRadius="6px"
                bg={`${AD}0.1)`} color={A}
                fontSize="xs" fontWeight="500"
                _hover={{ bg: `${AD}0.18)` }}
                onClick={openFolder}
              >
                Open Folder
              </Box>
            </Flex>
            <Box px={5} py={3}>
              <Text color={T} fontFamily="mono" fontSize="sm" bg="rgba(255,255,255,0.03)" px={3} py={2} borderRadius="6px">
                {result.outputPath}
              </Text>
            </Box>
          </Box>
        )}

        {/* BIOS Settings */}
        {result.biosSettings && result.biosSettings.length > 0 && (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Flex justify="space-between" align="center" px={5} py={3} borderBottom={`1px solid ${B}`}>
              <HStack gap={2}>
                <Settings size={15} color={A} />
                <Text color={T} fontWeight="600" fontSize="sm">BIOS Settings</Text>
              </HStack>
              <Flex px={2} py={0.5} borderRadius="5px" bg="rgba(234,179,8,0.1)" align="center" gap={1}>
                <AlertTriangle size={10} color="#EAB308" />
                <Text fontSize="xs" color="#EAB308" fontWeight="500">Required for macOS</Text>
              </Flex>
            </Flex>
            <VStack align="stretch" gap={0}>
              <Flex px={5} py={2} borderBottom={`1px solid rgba(255,255,255,0.04)`} bg="rgba(255,255,255,0.02)">
                <Text flex={2} fontSize="10px" color={TS} fontWeight="600" textTransform="uppercase">SETTING</Text>
                <Text flex={1} fontSize="10px" color={TS} fontWeight="600" textTransform="uppercase">CATEGORY</Text>
                <Text flex={1} fontSize="10px" color={TS} fontWeight="600" textTransform="uppercase">VALUE</Text>
                <Text w="80px" fontSize="10px" color={TS} fontWeight="600" textTransform="uppercase" textAlign="right">STATUS</Text>
              </Flex>
              {result.biosSettings.map((setting, i) => (
                <Flex key={i} px={5} py={2.5} align="center" borderBottom={`1px solid rgba(255,255,255,0.03)`} _hover={{ bg: 'rgba(255,255,255,0.01)' }}>
                  <Text flex={2} color={T} fontSize="sm" fontWeight="500">{setting.name}</Text>
                  <Box flex={1}>
                    <Box display="inline-block" px={1.5} py={0.5} borderRadius="4px" bg="rgba(255,255,255,0.05)" fontSize="xs" color={TS}>
                      {setting.category}
                    </Box>
                  </Box>
                  <Box flex={1}>
                    <Box
                      display="inline-block" px={1.5} py={0.5} borderRadius="4px"
                      bg={setting.recommended === 'Enabled' ? 'rgba(34,197,94,0.1)' : 'rgba(239,68,68,0.1)'}
                      fontSize="xs" fontWeight="500"
                      color={setting.recommended === 'Enabled' ? '#22C55E' : '#EF4444'}
                    >
                      {setting.recommended}
                    </Box>
                  </Box>
                  <Text w="80px" textAlign="right" fontSize="xs" fontWeight="500" color={setting.required ? '#EF4444' : TS}>
                    {setting.required ? 'Required' : 'Optional'}
                  </Text>
                </Flex>
              ))}
            </VStack>
          </Box>
        )}

        {/* Next Steps */}
        {result.nextSteps && result.nextSteps.length > 0 && (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
            <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
              <HStack gap={2}>
                <ListChecks size={15} color={A} />
                <Text color={T} fontWeight="600" fontSize="sm">Next Steps</Text>
              </HStack>
            </Box>
            <VStack align="stretch" gap={0} px={5} py={3}>
              {result.nextSteps.map((step, i) => (
                <HStack key={i} gap={3} py={2.5} borderBottom={`1px solid rgba(255,255,255,0.04)`}>
                  <Flex
                    w="22px" h="22px" borderRadius="full" flexShrink={0}
                    bg={`${AD}0.12)`} align="center" justify="center"
                  >
                    <Text fontSize="xs" color={A} fontWeight="700">{i + 1}</Text>
                  </Flex>
                  <Text color={T} fontSize="sm">{step}</Text>
                </HStack>
              ))}
            </VStack>
          </Box>
        )}

        {/* Config Summary */}
        <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
          <Box px={5} py={3} borderBottom={`1px solid ${B}`}>
            <HStack gap={2}>
              <BarChart3 size={15} color={A} />
              <Text color={T} fontWeight="600" fontSize="sm">Configuration Summary</Text>
            </HStack>
          </Box>
          <Box display="grid" gridTemplateColumns={{ base: '1fr', md: 'repeat(2, 1fr)' }} gap={2} p={4}>
            {[
              { label: 'macOS',       value: state.selectedMacOS?.name,     color: '#A78BFA' },
              { label: 'SMBIOS',      value: state.smbios?.model,           color: '#38BDF8' },
              { label: 'ACPI Patches',value: `${state.acpiPatches.filter((p) => p.enabled).length}`, color: '#22C55E' },
              { label: 'Kexts',       value: `${state.kexts.filter((k) => k.enabled).length}`,       color: '#f97316' },
              { label: 'USB Ports',   value: `${state.usbControllers.reduce((a, c) => a + c.ports.filter((p) => p.selected).length, 0)}`, color: '#EAB308' },
              { label: 'CPU',         value: state.report?.cpu.codename,    color: TS },
            ].map((item) => (
              <Flex key={item.label} justify="space-between" align="center" px={3} py={2} bg="rgba(255,255,255,0.02)" borderRadius="8px">
                <Text color={TS} fontSize="sm">{item.label}</Text>
                <Box px={2} py={0.5} borderRadius="4px" bg={`${item.color}15`} fontSize="xs" fontWeight="600" color={item.color}>
                  {item.value ?? 'N/A'}
                </Box>
              </Flex>
            ))}
          </Box>
        </Box>

        {/* Actions */}
        <Flex justify="space-between" align="center">
          <Box
            as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.07)', color: T }}
            onClick={() => navigate('/build')} display="flex" alignItems="center" gap={2}
          >
            <ChevronLeft size={14} /> Back to Build
          </Box>
          <HStack gap={2}>
            <Box
              as="button" px={4} py="8px" borderRadius="8px"
              bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
              _hover={{ bg: 'rgba(255,255,255,0.07)', color: T }}
              onClick={startNew} display="flex" alignItems="center" gap={2}
            >
              <RotateCcw size={13} /> New Build
            </Box>
            {result.success && (
              <Box
                as="button" px={4} py="8px" borderRadius="8px"
                bg={A} color="white" fontSize="sm" fontWeight="600"
                _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
                onClick={openFolder} display="flex" alignItems="center" gap={2}
              >
                <FolderOpen size={13} /> Open EFI
              </Box>
            )}
          </HStack>
        </Flex>
      </VStack>
    </Box>
  )
}