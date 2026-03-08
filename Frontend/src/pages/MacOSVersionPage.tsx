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
  Spinner,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { MacOSVersion } from '../types'

export function MacOSVersionPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [versions, setVersions] = useState<MacOSVersion[]>([])
  const [isLoading, setIsLoading] = useState(true)

  usePhotinoEvent<MacOSVersion[]>('macos:versions', (data) => {
    setIsLoading(false)
    if (data) {
      setVersions(data)
    }
  })

  useEffect(() => {
    invoke('macos:list', { report: state.report })
  }, [invoke, state.report])

  const handleSelect = useCallback(
    (version: MacOSVersion) => {
      dispatch({ type: 'SET_MACOS', version })
    },
    [dispatch]
  )

  // Mock data for development
  useEffect(() => {
    const mockVersions: MacOSVersion[] = [
      { name: 'Sequoia', version: '15.0', darwin: '24.0.0', supported: true },
      { name: 'Sonoma', version: '14.0', darwin: '23.0.0', supported: true },
      { name: 'Ventura', version: '13.0', darwin: '22.0.0', supported: true },
      { name: 'Monterey', version: '12.0', darwin: '21.0.0', supported: true },
      { name: 'Big Sur', version: '11.0', darwin: '20.0.0', supported: true, warnings: ['Legacy support'] },
      { name: 'Catalina', version: '10.15', darwin: '19.0.0', supported: false, warnings: ['End of life'] },
    ]
    setTimeout(() => {
      setVersions(mockVersions)
      setIsLoading(false)
    }, 500)
  }, [])

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack>
            <Text fontSize="2xl">🍎</Text>
            <Heading size="xl">macOS Version</Heading>
          </HStack>
          <Text color="fg.muted">
            Select the macOS version you want to install.
          </Text>
        </VStack>

        {isLoading ? (
          <Card.Root>
            <Card.Body>
              <VStack gap={4} py={8}>
                <Spinner size="xl" colorPalette="brand" />
                <Text>Loading versions...</Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        ) : (
          <VStack gap={3} align="stretch">
            {versions.map((version) => {
              const isSelected = state.selectedMacOS?.version === version.version
              const isDisabled = !version.supported

              return (
                <Card.Root
                  key={version.version}
                  variant="outline"
                  borderWidth={2}
                  borderColor={isSelected ? 'brand.500' : 'border.muted'}
                  opacity={isDisabled ? 0.5 : 1}
                  cursor={isDisabled ? 'not-allowed' : 'pointer'}
                  onClick={() => !isDisabled && handleSelect(version)}
                  _hover={!isDisabled ? { borderColor: 'border.emphasized' } : {}}
                  transition="all 0.15s"
                >
                  <Card.Body>
                    <HStack justify="space-between">
                      <HStack gap={4}>
                        <Box
                          w={5}
                          h={5}
                          borderRadius="full"
                          borderWidth={2}
                          borderColor={isSelected ? 'brand.500' : 'border.muted'}
                          bg={isSelected ? 'brand.500' : 'transparent'}
                        />
                        <VStack align="start" gap={0}>
                          <HStack>
                            <Text fontWeight="semibold" fontSize="lg">
                              macOS {version.name}
                            </Text>
                            <Badge colorPalette="gray">{version.version}</Badge>
                          </HStack>
                          <Text color="fg.muted" fontSize="sm">
                            Darwin {version.darwin}
                          </Text>
                        </VStack>
                      </HStack>
                      <HStack>
                        {version.warnings?.map((warning, i) => (
                          <Badge key={i} colorPalette="yellow">
                            {warning}
                          </Badge>
                        ))}
                        {!version.supported && (
                          <Badge colorPalette="red">Unsupported</Badge>
                        )}
                        {version.supported && (
                          <Badge colorPalette="green">Compatible</Badge>
                        )}
                      </HStack>
                    </HStack>
                  </Card.Body>
                </Card.Root>
              )
            })}
          </VStack>
        )}

        {/* Selected Info */}
        {state.selectedMacOS && (
          <Card.Root bg="brand.subtle">
            <Card.Body>
              <HStack>
                <Text fontSize="xl">✓</Text>
                <Text>
                  Selected: <strong>macOS {state.selectedMacOS.name}</strong> ({state.selectedMacOS.version})
                </Text>
              </HStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/compatibility')}>
            ← Compatibility
          </Button>
          <Button
            colorPalette="brand"
            onClick={() => navigate('/acpi')}
            disabled={!state.selectedMacOS}
          >
            Configure ACPI →
          </Button>
        </HStack>
      </VStack>
    </Box>
  )
}
