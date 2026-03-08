import { Box, VStack, Text, HStack, Badge } from '@chakra-ui/react'
import { Link, useLocation } from 'react-router-dom'
import { WIZARD_STEPS } from '../context/AppContext'

export function Sidebar() {
  const location = useLocation()

  return (
    <Box
      as="nav"
      w="240px"
      minH="100vh"
      bg="bg.subtle"
      borderRightWidth="1px"
      borderColor="border.muted"
      py={4}
    >
      {/* Logo */}
      <Box px={4} mb={6}>
        <HStack gap={2}>
          <Text fontSize="xl" fontWeight="bold">
            🍏 OpCore
          </Text>
          <Badge colorPalette="brand" size="sm">
            Simplify
          </Badge>
        </HStack>
      </Box>

      {/* Navigation */}
      <VStack gap={1} align="stretch">
        {WIZARD_STEPS.map((step, index) => {
          const isActive = location.pathname === step.path
          const isHome = step.id === 'home'

          return (
            <Link key={step.id} to={step.path}>
              <HStack
                px={4}
                py={2}
                mx={2}
                borderRadius="md"
                bg={isActive ? 'bg.emphasized' : 'transparent'}
                color={isActive ? 'fg' : 'fg.muted'}
                _hover={{ bg: 'bg.muted' }}
                transition="all 0.15s"
              >
                <Text fontSize="lg" w={6} textAlign="center">
                  {step.icon}
                </Text>
                <Text flex={1} fontWeight={isActive ? 'semibold' : 'normal'}>
                  {step.label}
                </Text>
                {!isHome && (
                  <Text fontSize="xs" color="fg.subtle">
                    {index}
                  </Text>
                )}
              </HStack>
            </Link>
          )
        })}
      </VStack>

      {/* Footer */}
      <Box position="absolute" bottom={4} left={0} right={0} px={4}>
        <Text fontSize="xs" color="fg.subtle" textAlign="center">
          v0.1.0 • .NET 10
        </Text>
      </Box>
    </Box>
  )
}
