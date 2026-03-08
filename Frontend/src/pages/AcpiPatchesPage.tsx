import { useEffect, useState } from 'react'
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
  Checkbox,
  Input,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { AcpiPatch } from '../types'

const categoryColors: Record<string, string> = {
  Required: 'red',
  Recommended: 'yellow',
  Optional: 'blue',
  'Hardware-Specific': 'purple',
}

export function AcpiPatchesPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isLoading, setIsLoading] = useState(true)
  const [filter, setFilter] = useState('')

  usePhotinoEvent<AcpiPatch[]>('acpi:patches', (data) => {
    setIsLoading(false)
    if (data) {
      dispatch({ type: 'SET_ACPI_PATCHES', patches: data })
    }
  })

  useEffect(() => {
    invoke('acpi:list', { report: state.report, macos: state.selectedMacOS })
  }, [invoke, state.report, state.selectedMacOS])

  // Mock data for development
  useEffect(() => {
    const mockPatches: AcpiPatch[] = [
      { id: 'ssdt-plug', name: 'SSDT-PLUG', description: 'CPU power management fix', category: 'Required', enabled: true, required: true },
      { id: 'ssdt-ec', name: 'SSDT-EC', description: 'Embedded Controller fix', category: 'Required', enabled: true, required: true },
      { id: 'ssdt-awac', name: 'SSDT-AWAC', description: 'AWAC clock fix (300-series+)', category: 'Hardware-Specific', enabled: false, required: false },
      { id: 'ssdt-rhub', name: 'SSDT-RHUB', description: 'USB RHUB reset', category: 'Optional', enabled: false, required: false },
      { id: 'ssdt-pnlf', name: 'SSDT-PNLF', description: 'Backlight fix for laptops', category: 'Hardware-Specific', enabled: false, required: false },
      { id: 'ssdt-sbus-mchc', name: 'SSDT-SBUS-MCHC', description: 'SMBus support', category: 'Recommended', enabled: true, required: false },
      { id: 'ssdt-usbx', name: 'SSDT-USBX', description: 'USB power properties', category: 'Required', enabled: true, required: true },
      { id: 'ssdt-xosi', name: 'SSDT-XOSI', description: 'OS detection spoof', category: 'Optional', enabled: false, required: false },
    ]
    setTimeout(() => {
      dispatch({ type: 'SET_ACPI_PATCHES', patches: mockPatches })
      setIsLoading(false)
    }, 300)
  }, [dispatch])

  const filteredPatches = state.acpiPatches.filter(
    (p) =>
      p.name.toLowerCase().includes(filter.toLowerCase()) ||
      p.description.toLowerCase().includes(filter.toLowerCase())
  )

  const groupedPatches = filteredPatches.reduce(
    (acc, patch) => {
      const cat = patch.category
      if (!acc[cat]) acc[cat] = []
      acc[cat].push(patch)
      return acc
    },
    {} as Record<string, AcpiPatch[]>
  )

  const enabledCount = state.acpiPatches.filter((p) => p.enabled).length

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack justify="space-between" w="full">
            <HStack>
              <Text fontSize="2xl">⚙️</Text>
              <Heading size="xl">ACPI Patches</Heading>
            </HStack>
            <Badge colorPalette="brand" size="lg">
              {enabledCount} enabled
            </Badge>
          </HStack>
          <Text color="fg.muted">
            Select ACPI tables (SSDTs) needed for your hardware.
          </Text>
        </VStack>

        {/* Search */}
        <Input
          placeholder="Search patches..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
        />

        {isLoading ? (
          <Card.Root>
            <Card.Body>
              <VStack gap={4} py={8}>
                <Spinner size="xl" colorPalette="brand" />
                <Text>Loading patches...</Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        ) : (
          Object.entries(groupedPatches).map(([category, patches]) => (
            <Card.Root key={category}>
              <Card.Header pb={2}>
                <HStack>
                  <Badge colorPalette={categoryColors[category] ?? 'gray'}>
                    {category}
                  </Badge>
                  <Text color="fg.muted" fontSize="sm">
                    {patches.length} patches
                  </Text>
                </HStack>
              </Card.Header>
              <Card.Body pt={0}>
                <VStack align="stretch" gap={2}>
                  {patches.map((patch) => (
                    <HStack
                      key={patch.id}
                      p={3}
                      bg="bg.subtle"
                      borderRadius="md"
                      justify="space-between"
                    >
                      <HStack gap={3}>
                        <Checkbox.Root
                          checked={patch.enabled}
                          disabled={patch.required}
                          onCheckedChange={() =>
                            dispatch({ type: 'TOGGLE_ACPI_PATCH', id: patch.id })
                          }
                        >
                          <Checkbox.HiddenInput />
                          <Checkbox.Control />
                        </Checkbox.Root>
                        <VStack align="start" gap={0}>
                          <HStack>
                            <Text fontWeight="semibold">{patch.name}</Text>
                            {patch.required && (
                              <Badge colorPalette="red" size="sm">
                                Required
                              </Badge>
                            )}
                          </HStack>
                          <Text color="fg.muted" fontSize="sm">
                            {patch.description}
                          </Text>
                        </VStack>
                      </HStack>
                    </HStack>
                  ))}
                </VStack>
              </Card.Body>
            </Card.Root>
          ))
        )}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/macos')}>
            ← macOS
          </Button>
          <Button colorPalette="brand" onClick={() => navigate('/kexts')}>
            Configure Kexts →
          </Button>
        </HStack>
      </VStack>
    </Box>
  )
}
