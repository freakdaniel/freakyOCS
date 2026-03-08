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
import type { KextInfo } from '../types'

const categoryIcons: Record<string, string> = {
  Core: '🔧',
  Audio: '🔊',
  Graphics: '🖼️',
  Network: '🌐',
  Storage: '💾',
  USB: '🔌',
  Other: '📦',
}

export function KextsPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [isLoading, setIsLoading] = useState(true)
  const [filter, setFilter] = useState('')
  const [activeTab, setActiveTab] = useState('all')

  usePhotinoEvent<KextInfo[]>('kexts:list', (data) => {
    setIsLoading(false)
    if (data) {
      dispatch({ type: 'SET_KEXTS', kexts: data })
    }
  })

  useEffect(() => {
    invoke('kexts:list', { report: state.report, macos: state.selectedMacOS })
  }, [invoke, state.report, state.selectedMacOS])

  // Mock data for development
  useEffect(() => {
    const mockKexts: KextInfo[] = [
      { id: 'lilu', name: 'Lilu', version: '1.6.7', description: 'Patching engine for macOS', category: 'Core', enabled: true, required: true },
      { id: 'virtualsmc', name: 'VirtualSMC', version: '1.3.2', description: 'SMC emulator', category: 'Core', enabled: true, required: true, dependencies: ['lilu'] },
      { id: 'whatevergreen', name: 'WhateverGreen', version: '1.6.6', description: 'GPU patches', category: 'Graphics', enabled: true, required: false, dependencies: ['lilu'] },
      { id: 'applealc', name: 'AppleALC', version: '1.8.8', description: 'Audio codec support', category: 'Audio', enabled: true, required: false, dependencies: ['lilu'] },
      { id: 'intelmausi', name: 'IntelMausi', version: '1.0.7', description: 'Intel ethernet', category: 'Network', enabled: false, required: false },
      { id: 'realtekrtl8111', name: 'RealtekRTL8111', version: '2.4.2', description: 'Realtek ethernet', category: 'Network', enabled: false, required: false },
      { id: 'airportitlwm', name: 'AirportItlwm', version: '2.2.0', description: 'Intel WiFi', category: 'Network', enabled: false, required: false },
      { id: 'nvmefix', name: 'NVMeFix', version: '1.1.1', description: 'NVMe power management', category: 'Storage', enabled: true, required: false, dependencies: ['lilu'] },
      { id: 'usbtoolbox', name: 'USBToolBox', version: '1.1.1', description: 'USB mapping companion', category: 'USB', enabled: true, required: false },
      { id: 'smcprocessor', name: 'SMCProcessor', version: '1.3.2', description: 'CPU sensors', category: 'Other', enabled: true, required: false, dependencies: ['virtualsmc'] },
      { id: 'smcsuperio', name: 'SMCSuperIO', version: '1.3.2', description: 'Fan sensors', category: 'Other', enabled: false, required: false, dependencies: ['virtualsmc'] },
    ]
    setTimeout(() => {
      dispatch({ type: 'SET_KEXTS', kexts: mockKexts })
      setIsLoading(false)
    }, 300)
  }, [dispatch])

  const categories = ['all', ...new Set(state.kexts.map((k) => k.category))]

  const filteredKexts = state.kexts.filter((k) => {
    const matchesFilter =
      k.name.toLowerCase().includes(filter.toLowerCase()) ||
      k.description.toLowerCase().includes(filter.toLowerCase())
    const matchesCategory = activeTab === 'all' || k.category === activeTab
    return matchesFilter && matchesCategory
  })

  const enabledCount = state.kexts.filter((k) => k.enabled).length

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <VStack gap={2} align="start">
          <HStack justify="space-between" w="full">
            <HStack>
              <Text fontSize="2xl">📦</Text>
              <Heading size="xl">Kernel Extensions</Heading>
            </HStack>
            <Badge colorPalette="brand" size="lg">
              {enabledCount} enabled
            </Badge>
          </HStack>
          <Text color="fg.muted">
            Configure kexts for your hardware.
          </Text>
        </VStack>

        {/* Search */}
        <Input
          placeholder="Search kexts..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
        />

        {/* Category Tabs */}
        <HStack gap={2} wrap="wrap">
          {categories.map((cat) => (
            <Button
              key={cat}
              size="sm"
              variant={activeTab === cat ? 'solid' : 'outline'}
              colorPalette={activeTab === cat ? 'brand' : 'gray'}
              onClick={() => setActiveTab(cat)}
            >
              {cat === 'all' ? '📋 All' : `${categoryIcons[cat] ?? '📦'} ${cat}`}
            </Button>
          ))}
        </HStack>

        {isLoading ? (
          <Card.Root>
            <Card.Body>
              <VStack gap={4} py={8}>
                <Spinner size="xl" colorPalette="brand" />
                <Text>Loading kexts...</Text>
              </VStack>
            </Card.Body>
          </Card.Root>
        ) : (
          <VStack align="stretch" gap={2}>
            {filteredKexts.map((kext) => (
              <Card.Root
                key={kext.id}
                variant="outline"
                borderColor={kext.enabled ? 'brand.muted' : 'border.muted'}
              >
                <Card.Body py={3}>
                  <HStack justify="space-between">
                    <HStack gap={3}>
                      <Checkbox.Root
                        checked={kext.enabled}
                        disabled={kext.required}
                        onCheckedChange={() =>
                          dispatch({ type: 'TOGGLE_KEXT', id: kext.id })
                        }
                      >
                        <Checkbox.HiddenInput />
                        <Checkbox.Control />
                      </Checkbox.Root>
                      <VStack align="start" gap={0}>
                        <HStack>
                          <Text fontWeight="semibold">{kext.name}</Text>
                          <Badge colorPalette="gray" size="sm">
                            v{kext.version}
                          </Badge>
                          {kext.required && (
                            <Badge colorPalette="red" size="sm">
                              Required
                            </Badge>
                          )}
                        </HStack>
                        <Text color="fg.muted" fontSize="sm">
                          {kext.description}
                        </Text>
                        {kext.dependencies && kext.dependencies.length > 0 && (
                          <Text color="fg.subtle" fontSize="xs">
                            Requires: {kext.dependencies.join(', ')}
                          </Text>
                        )}
                      </VStack>
                    </HStack>
                    <Badge colorPalette="blue" variant="subtle">
                      {categoryIcons[kext.category]} {kext.category}
                    </Badge>
                  </HStack>
                </Card.Body>
              </Card.Root>
            ))}
          </VStack>
        )}

        {/* Navigation */}
        <HStack justify="space-between">
          <Button variant="outline" onClick={() => navigate('/acpi')}>
            ← ACPI
          </Button>
          <Button colorPalette="brand" onClick={() => navigate('/smbios')}>
            Configure SMBIOS →
          </Button>
        </HStack>
      </VStack>
    </Box>
  )
}
