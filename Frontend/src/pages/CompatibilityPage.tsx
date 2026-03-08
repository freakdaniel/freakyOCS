import { useEffect, useCallback, useState } from 'react'
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Card,
  Button,
  Badge,
  Table,
  Spinner,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { CompatibilityResult, CompatibilityStatus } from '../types'

const statusColors: Record<CompatibilityStatus, string> = {
  supported: 'green',
  limited: 'yellow',
  unsupported: 'red',
  unknown: 'gray',
}

const statusIcons: Record<CompatibilityStatus, string> = {
  supported: '✅',
  limited: '⚠️',
  unsupported: '❌',
  unknown: '❓',
}

export function CompatibilityPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isChecking, setIsChecking] = useState(false)

  usePhotinoEvent<CompatibilityResult>('compatibility:result', (data) => {
    setIsChecking(false)
    if (data) {
      dispatch({ type: 'SET_COMPATIBILITY', result: data })
    }
  })

  const checkCompatibility = useCallback(() => {
    if (!state.report) return
    setIsChecking(true)
    invoke('compatibility:check', state.report)
  }, [invoke, state.report])

  useEffect(() => {
    if (state.report && !state.compatibility) {
      checkCompatibility()
    }
  }, [state.report, state.compatibility, checkCompatibility])

  if (!state.report) {
    return (
      <Box maxW="4xl" mx="auto">
        <Card.Root>
          <Card.Body>
            <VStack gap={4} py={8}>
              <Text fontSize="4xl">📋</Text>
              <Heading size="md">No Hardware Report</Heading>
              <Text color="fg.muted">
                Please detect or load hardware first.
              </Text>
              <Button colorPalette="brand" onClick={() => navigate('/report')}>
                Go to Hardware Detection
              </Button>
            </VStack>
          </Card.Body>
        </Card.Root>
      </Box>
    )
  }

  return (
    <Box maxW="5xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack>
            <Text fontSize="2xl">✅</Text>
            <Heading size="xl">Compatibility Check</Heading>
          </HStack>
          <Text color="fg.muted">
            Review hardware compatibility with macOS.
          </Text>
        </VStack>

        {isChecking ? (
          <Card.Root>
            <Card.Body>
              <VStack gap={4} py={8}>
                <Spinner size="xl" colorPalette="brand" />
                <Text>Checking compatibility...</Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        ) : state.compatibility ? (
          <>
            {/* Summary */}
            <Card.Root>
              <Card.Body>
                <HStack justify="space-between" wrap="wrap" gap={4}>
                  <HStack gap={3}>
                    <Text fontSize="3xl">
                      {statusIcons[state.compatibility.overallStatus]}
                    </Text>
                    <Box>
                      <Text fontWeight="semibold" fontSize="lg">
                        Overall Status
                      </Text>
                      <Badge
                        colorPalette={statusColors[state.compatibility.overallStatus]}
                        size="lg"
                      >
                        {state.compatibility.overallStatus.toUpperCase()}
                      </Badge>
                    </Box>
                  </HStack>
                  <Button variant="outline" onClick={checkCompatibility}>
                    Re-check
                  </Button>
                </HStack>
              </Card.Body>
            </Card.Root>

            {/* Warnings & Blockers */}
            {(state.compatibility.warnings.length > 0 ||
              state.compatibility.blockers.length > 0) && (
              <SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
                {state.compatibility.blockers.length > 0 && (
                  <Card.Root bg="red.subtle">
                    <Card.Header>
                      <Heading size="sm" color="red.fg">
                        ❌ Blockers
                      </Heading>
                    </Card.Header>
                    <Card.Body pt={0}>
                      <VStack align="start" gap={2}>
                        {state.compatibility.blockers.map((b, i) => (
                          <Text key={i} fontSize="sm">
                            {b}
                          </Text>
                        ))}
                      </VStack>
                    </Card.Body>
                  </Card.Root>
                )}
                {state.compatibility.warnings.length > 0 && (
                  <Card.Root bg="yellow.subtle">
                    <Card.Header>
                      <Heading size="sm" color="yellow.fg">
                        ⚠️ Warnings
                      </Heading>
                    </Card.Header>
                    <Card.Body pt={0}>
                      <VStack align="start" gap={2}>
                        {state.compatibility.warnings.map((w, i) => (
                          <Text key={i} fontSize="sm">
                            {w}
                          </Text>
                        ))}
                      </VStack>
                    </Card.Body>
                  </Card.Root>
                )}
              </SimpleGrid>
            )}

            {/* Device Table */}
            <Card.Root>
              <Card.Header>
                <Heading size="md">Device Compatibility</Heading>
              </Card.Header>
              <Card.Body p={0}>
                <Table.Root>
                  <Table.Header>
                    <Table.Row>
                      <Table.ColumnHeader>Category</Table.ColumnHeader>
                      <Table.ColumnHeader>Device</Table.ColumnHeader>
                      <Table.ColumnHeader>Status</Table.ColumnHeader>
                      <Table.ColumnHeader>Notes</Table.ColumnHeader>
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {state.compatibility.devices.map((device, i) => (
                      <Table.Row key={i}>
                        <Table.Cell>
                          <Badge variant="subtle">{device.category}</Badge>
                        </Table.Cell>
                        <Table.Cell fontWeight="medium">{device.name}</Table.Cell>
                        <Table.Cell>
                          <Badge colorPalette={statusColors[device.status]}>
                            {statusIcons[device.status]} {device.status}
                          </Badge>
                        </Table.Cell>
                        <Table.Cell color="fg.muted" fontSize="sm">
                          {device.notes}
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table.Root>
              </Card.Body>
            </Card.Root>
          </>
        ) : null}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/report')}>
            ← Hardware
          </Button>
          <Button
            colorPalette="brand"
            onClick={() => navigate('/macos')}
            disabled={
              state.compatibility?.overallStatus === 'unsupported'
            }
          >
            Select macOS →
          </Button>
        </HStack>
      </VStack>
    </Box>
  )
}

// Helper component for grid
function SimpleGrid({ columns, gap, children }: {
  columns: { base: number; md: number }
  gap: number
  children: React.ReactNode
}) {
  return (
    <Box
      display="grid"
      gridTemplateColumns={{
        base: `repeat(${columns.base}, 1fr)`,
        md: `repeat(${columns.md}, 1fr)`,
      }}
      gap={gap}
    >
      {children}
    </Box>
  )
}
