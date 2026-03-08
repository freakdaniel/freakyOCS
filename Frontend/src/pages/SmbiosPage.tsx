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
  Input,
  Code,
} from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { SmbiosModel, SmbiosConfig } from '../types'

export function SmbiosPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [models, setModels] = useState<SmbiosModel[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isGenerating, setIsGenerating] = useState(false)
  const [selectedModel, setSelectedModel] = useState<string | null>(null)
  const [filter, setFilter] = useState('')

  usePhotinoEvent<SmbiosModel[]>('smbios:models', (data) => {
    setIsLoading(false)
    if (data) {
      setModels(data)
    }
  })

  usePhotinoEvent<SmbiosConfig>('smbios:generated', (data) => {
    setIsGenerating(false)
    if (data) {
      dispatch({ type: 'SET_SMBIOS', config: data })
    }
  })

  useEffect(() => {
    invoke('smbios:list', { report: state.report, macos: state.selectedMacOS })
  }, [invoke, state.report, state.selectedMacOS])

  // Mock data for development
  useEffect(() => {
    const mockModels: SmbiosModel[] = [
      { id: 'iMac20,1', name: 'iMac (Retina 5K, 27-inch, 2020)', year: 2020, cpuFamily: 'Comet Lake', gpuFamily: 'AMD Radeon Pro', recommended: true, minMacOS: '10.15.6' },
      { id: 'iMac20,2', name: 'iMac (Retina 5K, 27-inch, 2020)', year: 2020, cpuFamily: 'Comet Lake', gpuFamily: 'AMD Radeon Pro', recommended: true, minMacOS: '10.15.6' },
      { id: 'MacBookPro16,1', name: 'MacBook Pro (16-inch, 2019)', year: 2019, cpuFamily: 'Coffee Lake', gpuFamily: 'AMD Radeon Pro', recommended: false, minMacOS: '10.15.1' },
      { id: 'Macmini8,1', name: 'Mac mini (2018)', year: 2018, cpuFamily: 'Coffee Lake', gpuFamily: 'Intel UHD 630', recommended: false, minMacOS: '10.14' },
      { id: 'iMacPro1,1', name: 'iMac Pro (2017)', year: 2017, cpuFamily: 'Skylake-X', gpuFamily: 'AMD Vega', recommended: false, minMacOS: '10.13.2' },
      { id: 'MacPro7,1', name: 'Mac Pro (2019)', year: 2019, cpuFamily: 'Cascade Lake-W', gpuFamily: 'AMD Pro', recommended: false, notes: 'For HEDT systems', minMacOS: '10.15' },
    ]
    setTimeout(() => {
      setModels(mockModels)
      setIsLoading(false)
    }, 300)
  }, [])

  const handleGenerate = useCallback(() => {
    if (!selectedModel) return
    setIsGenerating(true)
    invoke('smbios:generate', { model: selectedModel })

    // Mock generation
    setTimeout(() => {
      dispatch({
        type: 'SET_SMBIOS',
        config: {
          model: selectedModel,
          serial: 'C02XXXXXXXXX',
          mlb: 'C02XXXXXXXXXXXXXXXXX',
          uuid: crypto.randomUUID().toUpperCase(),
          rom: 'A1B2C3D4E5F6',
        },
      })
      setIsGenerating(false)
    }, 500)
  }, [selectedModel, invoke, dispatch])

  const filteredModels = models.filter(
    (m) =>
      m.id.toLowerCase().includes(filter.toLowerCase()) ||
      m.name.toLowerCase().includes(filter.toLowerCase()) ||
      m.cpuFamily.toLowerCase().includes(filter.toLowerCase())
  )

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack>
            <Text fontSize="2xl">🏷️</Text>
            <Heading size="xl">SMBIOS</Heading>
          </HStack>
          <Text color="fg.muted">
            Select a Mac model that matches your hardware.
          </Text>
        </VStack>

        {/* Search */}
        <Input
          placeholder="Search models..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
        />

        {isLoading ? (
          <Card.Root>
            <Card.Body>
              <VStack gap={4} py={8}>
                <Spinner size="xl" colorPalette="brand" />
                <Text>Loading models...</Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        ) : (
          <VStack align="stretch" gap={2}>
            {filteredModels.map((model) => {
              const isSelected = selectedModel === model.id

              return (
                <Card.Root
                  key={model.id}
                  variant="outline"
                  borderWidth={2}
                  borderColor={isSelected ? 'brand.500' : 'border.muted'}
                  cursor="pointer"
                  onClick={() => setSelectedModel(model.id)}
                  _hover={{ borderColor: 'border.emphasized' }}
                  transition="all 0.15s"
                >
                  <Card.Body py={3}>
                    <HStack justify="space-between">
                      <HStack gap={3}>
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
                            <Text fontWeight="semibold">{model.id}</Text>
                            {model.recommended && (
                              <Badge colorPalette="green">Recommended</Badge>
                            )}
                          </HStack>
                          <Text color="fg.muted" fontSize="sm">
                            {model.name}
                          </Text>
                          <HStack gap={2} mt={1}>
                            <Badge colorPalette="blue" size="sm" variant="subtle">
                              {model.cpuFamily}
                            </Badge>
                            <Badge colorPalette="purple" size="sm" variant="subtle">
                              {model.gpuFamily}
                            </Badge>
                            <Badge colorPalette="gray" size="sm" variant="subtle">
                              {model.year}
                            </Badge>
                          </HStack>
                        </VStack>
                      </HStack>
                      <Text color="fg.subtle" fontSize="sm">
                        macOS {model.minMacOS}+
                      </Text>
                    </HStack>
                  </Card.Body>
                </Card.Root>
              )
            })}
          </VStack>
        )}

        {/* Generate Button */}
        {selectedModel && !state.smbios && (
          <Button
            colorPalette="brand"
            size="lg"
            onClick={handleGenerate}
            loading={isGenerating}
            loadingText="Generating..."
          >
            Generate SMBIOS Data
          </Button>
        )}

        {/* Generated Data */}
        {state.smbios && (
          <Card.Root bg="brand.subtle">
            <Card.Header>
              <HStack justify="space-between">
                <Heading size="md">Generated SMBIOS</Heading>
                <Button size="sm" variant="outline" onClick={handleGenerate}>
                  Regenerate
                </Button>
              </HStack>
            </Card.Header>
            <Card.Body>
              <VStack align="stretch" gap={2}>
                <HStack justify="space-between">
                  <Text color="fg.muted">Model:</Text>
                  <Code>{state.smbios.model}</Code>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">Serial:</Text>
                  <Code>{state.smbios.serial}</Code>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">MLB:</Text>
                  <Code fontSize="xs">{state.smbios.mlb}</Code>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">UUID:</Text>
                  <Code fontSize="xs">{state.smbios.uuid}</Code>
                </HStack>
                <HStack justify="space-between">
                  <Text color="fg.muted">ROM:</Text>
                  <Code>{state.smbios.rom}</Code>
                </HStack>
              </VStack>
            </Card.Body>
          </Card.Root>
        )}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/kexts')}>
            ← Kexts
          </Button>
          <Button
            colorPalette="brand"
            onClick={() => navigate('/usb')}
            disabled={!state.smbios}
          >
            USB Mapping →
          </Button>
        </HStack>
      </VStack>
    </Box>
  )
}
