import { Box, Heading, Text, VStack, HStack, Card, Button, Badge, SimpleGrid } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'

export function HomePage() {
  const navigate = useNavigate()
  const { state } = useApp()

  const hasReport = !!state.report
  const hasMacOS = !!state.selectedMacOS
  const hasKexts = state.kexts.filter(k => k.enabled).length > 0

  return (
    <Box maxW="4xl" mx="auto">
      <VStack gap={8} align="stretch">
        {/* Header */}
        <VStack gap={3} textAlign="center" pt={8}>
          <Badge colorPalette="green" size="lg" px={3} py={1} borderRadius="full">
            OpCore Simplify
          </Badge>
          <Heading size="4xl" fontWeight="bold">
            OpenCore EFI Builder
          </Heading>
          <Text color="fg.muted" fontSize="lg" maxW="xl">
            Create a custom OpenCore EFI folder for your Hackintosh.
            We'll guide you through hardware detection, compatibility checks,
            and configuration — step by step.
          </Text>
        </VStack>

        {/* Quick Status */}
        {(hasReport || hasMacOS || hasKexts) && (
          <Card.Root>
            <Card.Header>
              <Heading size="md">Current Configuration</Heading>
            </Card.Header>
            <Card.Body>
              <SimpleGrid columns={{ base: 1, md: 3 }} gap={4}>
                <HStack>
                  <Text>{hasReport ? '✅' : '⏳'}</Text>
                  <Text>Hardware Report</Text>
                  {hasReport && (
                    <Badge colorPalette="blue" ml="auto">{state.report?.cpu.codename}</Badge>
                  )}
                </HStack>
                <HStack>
                  <Text>{hasMacOS ? '✅' : '⏳'}</Text>
                  <Text>macOS Version</Text>
                  {hasMacOS && (
                    <Badge colorPalette="purple" ml="auto">{state.selectedMacOS?.name}</Badge>
                  )}
                </HStack>
                <HStack>
                  <Text>{hasKexts ? '✅' : '⏳'}</Text>
                  <Text>Kexts</Text>
                  {hasKexts && (
                    <Badge colorPalette="orange" ml="auto">
                      {state.kexts.filter(k => k.enabled).length} enabled
                    </Badge>
                  )}
                </HStack>
              </SimpleGrid>
            </Card.Body>
          </Card.Root>
        )}

        {/* Action Cards */}
        <SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
          <Card.Root
            variant="outline"
            _hover={{ borderColor: 'border.emphasized', transform: 'translateY(-2px)' }}
            transition="all 0.2s"
            cursor="pointer"
            onClick={() => navigate('/report')}
          >
            <Card.Body>
              <VStack gap={3} align="start">
                <Text fontSize="3xl">🖥️</Text>
                <Heading size="md">Start New Build</Heading>
                <Text color="fg.muted" fontSize="sm">
                  Detect your hardware automatically or load an existing report.
                </Text>
                <Button colorPalette="brand" size="sm" mt={2}>
                  Get Started
                </Button>
              </VStack>
            </Card.Body>
          </Card.Root>

          <Card.Root
            variant="outline"
            _hover={{ borderColor: 'border.emphasized', transform: 'translateY(-2px)' }}
            transition="all 0.2s"
            cursor="pointer"
            onClick={() => navigate('/usb')}
          >
            <Card.Body>
              <VStack gap={3} align="start">
                <Text fontSize="3xl">🔌</Text>
                <Heading size="md">USB Mapper</Heading>
                <Text color="fg.muted" fontSize="sm">
                  Map your USB ports for macOS. Plug devices to detect active ports.
                </Text>
                <Button variant="outline" size="sm" mt={2}>
                  Open Mapper
                </Button>
              </VStack>
            </Card.Body>
          </Card.Root>
        </SimpleGrid>

        {/* Info */}
        <Card.Root variant="subtle" bg="bg.muted">
          <Card.Body>
            <HStack gap={4}>
              <Text fontSize="2xl">💡</Text>
              <VStack align="start" gap={1}>
                <Text fontWeight="semibold">Tips</Text>
                <Text color="fg.muted" fontSize="sm">
                  For best results, run this tool on the target Hackintosh machine.
                  USB mapping requires plugging devices into each port.
                </Text>
              </VStack>
            </HStack>
          </Card.Body>
        </Card.Root>
      </VStack>
    </Box>
  )
}
