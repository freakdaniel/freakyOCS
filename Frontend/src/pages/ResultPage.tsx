import { useCallback } from 'react'
import {
  Box,
  Heading,
  Text,
  VStack,
  HStack,
  Card,
  Button,
  Badge,
  Code,
  Table,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke } from '../bridge/usePhotino'

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
      <Box maxW="4xl" mx="auto">
        <Card.Root>
          <Card.Body>
            <VStack gap={4} py={8}>
              <Text fontSize="4xl">📦</Text>
              <Heading size="md">No Build Result</Heading>
              <Text color="fg.muted">Complete a build first to see results.</Text>
              <Button colorPalette="brand" onClick={() => navigate('/build')}>
                Go to Build
              </Button>
            </VStack>
          </Card.Body>
        </Card.Root>
      </Box>
    )
  }

  const result = state.buildResult

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Success Header */}
        <Card.Root bg={result.success ? 'green.subtle' : 'red.subtle'}>
          <Card.Body>
            <VStack gap={4} py={4}>
              <Text fontSize="5xl">{result.success ? '🎉' : '❌'}</Text>
              <Heading size="xl">
                {result.success ? 'Build Successful!' : 'Build Failed'}
              </Heading>
              {result.success && (
                <Text color="fg.muted">
                  Your OpenCore EFI folder has been created.
                </Text>
              )}
            </VStack>
          </Card.Body>
        </Card.Root>

        {/* Output Path */}
        {result.success && (
          <Card.Root>
            <Card.Header>
              <Heading size="md">📁 Output Location</Heading>
            </Card.Header>
            <Card.Body>
              <HStack justify="space-between" wrap="wrap" gap={4}>
                <Code fontSize="md" p={3} borderRadius="md">
                  {result.outputPath}
                </Code>
                <Button colorPalette="brand" onClick={openFolder}>
                  Open Folder
                </Button>
              </HStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* BIOS Settings */}
        {result.biosSettings && result.biosSettings.length > 0 && (
          <Card.Root>
            <Card.Header>
              <HStack justify="space-between">
                <Heading size="md">⚙️ BIOS Settings</Heading>
                <Badge colorPalette="yellow">Required for macOS</Badge>
              </HStack>
            </Card.Header>
            <Card.Body p={0}>
              <Table.Root>
                <Table.Header>
                  <Table.Row>
                    <Table.ColumnHeader>Setting</Table.ColumnHeader>
                    <Table.ColumnHeader>Category</Table.ColumnHeader>
                    <Table.ColumnHeader>Recommended</Table.ColumnHeader>
                    <Table.ColumnHeader>Required</Table.ColumnHeader>
                  </Table.Row>
                </Table.Header>
                <Table.Body>
                  {result.biosSettings.map((setting, i) => (
                    <Table.Row key={i}>
                      <Table.Cell fontWeight="medium">{setting.name}</Table.Cell>
                      <Table.Cell>
                        <Badge variant="subtle">{setting.category}</Badge>
                      </Table.Cell>
                      <Table.Cell>
                        <Badge
                          colorPalette={
                            setting.recommended === 'Enabled' ? 'green' : 'red'
                          }
                        >
                          {setting.recommended}
                        </Badge>
                      </Table.Cell>
                      <Table.Cell>
                        {setting.required ? (
                          <Badge colorPalette="red">Required</Badge>
                        ) : (
                          <Text color="fg.subtle">Optional</Text>
                        )}
                      </Table.Cell>
                    </Table.Row>
                  ))}
                </Table.Body>
              </Table.Root>
            </Card.Body>
          </Card.Root>
        )}

        {/* Next Steps */}
        {result.nextSteps && result.nextSteps.length > 0 && (
          <Card.Root>
            <Card.Header>
              <Heading size="md">📋 Next Steps</Heading>
            </Card.Header>
            <Card.Body>
              <VStack align="stretch" gap={3}>
                {result.nextSteps.map((step, i) => (
                  <HStack key={i} gap={3}>
                    <Box
                      w={8}
                      h={8}
                      borderRadius="full"
                      bg="brand.500"
                      color="white"
                      display="flex"
                      alignItems="center"
                      justifyContent="center"
                      fontWeight="bold"
                      flexShrink={0}
                    >
                      {i + 1}
                    </Box>
                    <Text>{step}</Text>
                  </HStack>
                ))}
              </VStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* Configuration Summary */}
        <Card.Root variant="subtle">
          <Card.Header>
            <Heading size="md">📊 Configuration Summary</Heading>
          </Card.Header>
          <Card.Body>
            <Box
              display="grid"
              gridTemplateColumns={{ base: '1fr', md: 'repeat(2, 1fr)' }}
              gap={4}
            >
              <HStack justify="space-between" p={2} bg="bg.subtle" borderRadius="md">
                <Text color="fg.muted">macOS Version</Text>
                <Badge colorPalette="purple">{state.selectedMacOS?.name}</Badge>
              </HStack>
              <HStack justify="space-between" p={2} bg="bg.subtle" borderRadius="md">
                <Text color="fg.muted">SMBIOS</Text>
                <Badge colorPalette="blue">{state.smbios?.model}</Badge>
              </HStack>
              <HStack justify="space-between" p={2} bg="bg.subtle" borderRadius="md">
                <Text color="fg.muted">ACPI Patches</Text>
                <Badge colorPalette="green">
                  {state.acpiPatches.filter((p) => p.enabled).length}
                </Badge>
              </HStack>
              <HStack justify="space-between" p={2} bg="bg.subtle" borderRadius="md">
                <Text color="fg.muted">Kexts</Text>
                <Badge colorPalette="orange">
                  {state.kexts.filter((k) => k.enabled).length}
                </Badge>
              </HStack>
              <HStack justify="space-between" p={2} bg="bg.subtle" borderRadius="md">
                <Text color="fg.muted">USB Ports</Text>
                <Badge colorPalette="yellow">
                  {state.usbControllers.reduce(
                    (acc, c) => acc + c.ports.filter((p) => p.selected).length,
                    0
                  )}
                </Badge>
              </HStack>
              <HStack justify="space-between" p={2} bg="bg.subtle" borderRadius="md">
                <Text color="fg.muted">CPU</Text>
                <Badge colorPalette="gray">{state.report?.cpu.codename}</Badge>
              </HStack>
            </Box>
          </Card.Body>
        </Card.Root>

        {/* Actions */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/build')}>
            ← Back to Build
          </Button>
          <HStack>
            <Button variant="outline" onClick={startNew}>
              Start New Build
            </Button>
            {result.success && (
              <Button colorPalette="brand" onClick={openFolder}>
                📁 Open EFI Folder
              </Button>
            )}
          </HStack>
        </HStack>

        {/* Footer */}
        <Card.Root variant="subtle" bg="brand.subtle">
          <Card.Body>
            <VStack gap={2}>
              <Text fontWeight="semibold">🍏 OpCore Simplify</Text>
              <Text color="fg.muted" fontSize="sm" textAlign="center">
                Built with .NET 10 + React + Photino
                <br />
                Based on OpenCore and Dortania guides
              </Text>
            </VStack>
          </Card.Body>
        </Card.Root>
      </VStack>
    </Box>
  )
}
