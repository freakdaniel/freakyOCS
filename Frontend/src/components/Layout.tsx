import { Box, HStack } from '@chakra-ui/react'
import type { ReactNode } from 'react'
import { Sidebar } from './Sidebar'

interface LayoutProps {
  children: ReactNode
}

export function Layout({ children }: LayoutProps) {
  return (
    <HStack align="stretch" gap={0} minH="100vh">
      <Sidebar />
      <Box as="main" flex="1" p={6} overflowY="auto">
        {children}
      </Box>
    </HStack>
  )
}
