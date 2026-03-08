import { useEffect, useCallback, useState } from 'react'
import {
  Box, Heading, Text, VStack, HStack, Flex, SimpleGrid, Spinner, Table,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { CompatibilityResult, CompatibilityStatus } from '../types'
import {
  ShieldCheck, CheckCircle2, AlertTriangle, XCircle, HelpCircle, RefreshCw, ChevronLeft, ChevronRight,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
//const BH = 'rgba(255,255,255,0.13)'
const A  = '#7B7FFF'
//const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'

const statusConfig: Record<CompatibilityStatus, { color: string; bg: string; icon: LucideIcon; label: string }> = {
  supported:   { color: '#22C55E', bg: 'rgba(34,197,94,0.1)',   icon: CheckCircle2, label: 'Supported' },
  limited:     { color: '#EAB308', bg: 'rgba(234,179,8,0.1)',   icon: AlertTriangle, label: 'Limited' },
  unsupported: { color: '#EF4444', bg: 'rgba(239,68,68,0.1)',   icon: XCircle,      label: 'Unsupported' },
  unknown:     { color: '#7A829E', bg: 'rgba(122,130,158,0.1)', icon: HelpCircle,   label: 'Unknown' },
}

export function CompatibilityPage() {
  const navigate = useNavigate()
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

  if (!state.report) {
    return (
      <Box maxW="860px" mx="auto">
        <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
          <VStack gap={4}>
            <ShieldCheck size={36} color={TS} />
            <Text color={T} fontWeight="600" fontSize="sm">No Hardware Report</Text>
            <Text color={TS} fontSize="sm">Please detect or load hardware first.</Text>
            <Box as="button" px={4} py="8px" borderRadius="8px" bg={A} color="white" fontSize="sm" fontWeight="600"
              _hover={{ bg: '#8F93FF' }} onClick={() => navigate('/report')}>
              Go to Hardware Detection
            </Box>
          </VStack>
        </Box>
      </Box>
    )
  }

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Box>
          <HStack gap={3} mb={1.5}>
            <Flex w="34px" h="34px" borderRadius="8px" bg="rgba(34,197,94,0.12)" align="center" justify="center">
              <ShieldCheck size={17} color="#22C55E" />
            </Flex>
            <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">Compatibility Check</Heading>
          </HStack>
          <Text color={TS} fontSize="sm">Review hardware compatibility with macOS.</Text>
        </Box>

        {isChecking ? (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
            <VStack gap={4}>
              <Spinner size="xl" color={A} />
              <Text color={TS} fontSize="sm">Checking compatibility…</Text>
            </VStack>
          </Box>
        ) : state.compatibility ? (
          <>
            {/* Summary */}
            <Flex bg={S} border={`1px solid ${B}`} borderRadius="12px" p={4}
              justify="space-between" align="center"
            >
              <HStack gap={3}>
                {(() => {
                  const cfg = statusConfig[state.compatibility.overallStatus]
                  const Icon = cfg.icon
                  return (
                    <>
                      <Flex w="38px" h="38px" borderRadius="9px" bg={cfg.bg} align="center" justify="center">
                        <Icon size={18} color={cfg.color} />
                      </Flex>
                      <Box>
                        <Text color={TS} fontSize="10px" fontWeight="600" textTransform="uppercase" letterSpacing="0.05em">Overall Status</Text>
                        <Text color={cfg.color} fontSize="sm" fontWeight="700">{cfg.label}</Text>
                      </Box>
                    </>
                  )
                })()}
              </HStack>
              <Box as="button" px={3} py={2} borderRadius="8px"
                bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm"
                _hover={{ bg: 'rgba(255,255,255,0.08)' }}
                onClick={checkCompatibility} display="flex" alignItems="center" gap={2}
              >
                <RefreshCw size={13} /> Re-check
              </Box>
            </Flex>

            {/* Warnings & Blockers */}
            {(state.compatibility.warnings.length > 0 || state.compatibility.blockers.length > 0) && (
              <SimpleGrid columns={{ base: 1, md: 2 }} gap={3}>
                {state.compatibility.blockers.length > 0 && (
                  <Box bg="rgba(239,68,68,0.06)" border="1px solid rgba(239,68,68,0.14)" borderRadius="12px" p={4}>
                    <HStack gap={2} mb={3}>
                      <XCircle size={15} color="#EF4444" />
                      <Text color="#EF4444" fontWeight="600" fontSize="sm">Blockers</Text>
                    </HStack>
                    <VStack align="start" gap={2}>
                      {state.compatibility.blockers.map((b, i) => (
                        <Text key={i} color="#FCA5A5" fontSize="xs">{b}</Text>
                      ))}
                    </VStack>
                  </Box>
                )}
                {state.compatibility.warnings.length > 0 && (
                  <Box bg="rgba(234,179,8,0.06)" border="1px solid rgba(234,179,8,0.15)" borderRadius="12px" p={4}>
                    <HStack gap={2} mb={3}>
                      <AlertTriangle size={15} color="#EAB308" />
                      <Text color="#EAB308" fontWeight="600" fontSize="sm">Warnings</Text>
                    </HStack>
                    <VStack align="start" gap={2}>
                      {state.compatibility.warnings.map((w, i) => (
                        <Text key={i} color="#FDE68A" fontSize="xs">{w}</Text>
                      ))}
                    </VStack>
                  </Box>
                )}
              </SimpleGrid>
            )}

            {/* Device Table */}
            <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" overflow="hidden">
              <Box px={5} py={3.5} borderBottom={`1px solid ${B}`}>
                <Text color={T} fontWeight="600" fontSize="sm">Device Compatibility</Text>
              </Box>
              <Table.Root>
                <Table.Header>
                  <Table.Row>
                    <Table.ColumnHeader color={TS} fontSize="10px" fontWeight="600" textTransform="uppercase" letterSpacing="0.06em">Category</Table.ColumnHeader>
                    <Table.ColumnHeader color={TS} fontSize="10px" fontWeight="600" textTransform="uppercase" letterSpacing="0.06em">Device</Table.ColumnHeader>
                    <Table.ColumnHeader color={TS} fontSize="10px" fontWeight="600" textTransform="uppercase" letterSpacing="0.06em">Status</Table.ColumnHeader>
                    <Table.ColumnHeader color={TS} fontSize="10px" fontWeight="600" textTransform="uppercase" letterSpacing="0.06em">Notes</Table.ColumnHeader>
                  </Table.Row>
                </Table.Header>
                <Table.Body>
                  {state.compatibility.devices.map((device, i) => {
                    const cfg = statusConfig[device.status]
                    const StatusIcon = cfg.icon
                    return (
                      <Table.Row key={i} _hover={{ bg: 'rgba(255,255,255,0.02)' }}>
                        <Table.Cell>
                          <Box display="inline-block" px={2} py={0.5} borderRadius="5px"
                            bg="rgba(255,255,255,0.04)" fontSize="10px" color={TS} fontWeight="600">
                            {device.category}
                          </Box>
                        </Table.Cell>
                        <Table.Cell color={T} fontWeight="500" fontSize="sm">{device.name}</Table.Cell>
                        <Table.Cell>
                          <HStack gap={1.5}>
                            <StatusIcon size={13} color={cfg.color} />
                            <Text color={cfg.color} fontSize="xs" fontWeight="600">{cfg.label}</Text>
                          </HStack>
                        </Table.Cell>
                        <Table.Cell color={TS} fontSize="xs">{device.notes}</Table.Cell>
                      </Table.Row>
                    )
                  })}
                </Table.Body>
              </Table.Root>
            </Box>
          </>
        ) : null}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={() => navigate('/report')} display="flex" alignItems="center" gap={2}>
            <ChevronLeft size={14} /> Hardware
          </Box>
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg={A} color="white" fontSize="sm" fontWeight="600"
            _hover={{ bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' }}
            onClick={() => navigate('/macos')} display="flex" alignItems="center" gap={2}
            opacity={state.compatibility?.overallStatus === 'unsupported' ? 0.4 : 1}
            pointerEvents={state.compatibility?.overallStatus === 'unsupported' ? 'none' : 'auto'}>
            Select macOS <ChevronRight size={14} />
          </Box>
        </Flex>
      </VStack>
    </Box>
  )
}
