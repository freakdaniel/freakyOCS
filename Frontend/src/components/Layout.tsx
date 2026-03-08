import { Box, Flex } from '@chakra-ui/react'
import type { ReactNode } from 'react'
import { Sidebar } from './Sidebar'

interface LayoutProps {
  children: ReactNode
}

export function Layout({ children }: LayoutProps) {
  return (
    <Flex h="100vh" bg="#0A0A0A" pos="relative" overflow="hidden">
      {/* Ambient teal blob — top right */}
      <Box
        className="blob"
        top="-120px"
        right="-80px"
        w="500px" h="500px"
        bg="radial-gradient(circle, rgba(45,212,191,0.06) 0%, transparent 70%)"
      />
      {/* Subtle warm blob — bottom left of content */}
      <Box
        className="blob"
        bottom="-150px"
        left="calc(var(--sidebar-width) + 5%)"
        w="400px" h="400px"
        bg="radial-gradient(circle, rgba(45,212,191,0.04) 0%, transparent 70%)"
      />

      <Sidebar />

      <Box
        as="main"
        flex="1"
        ml="var(--sidebar-width)"
        h="100vh"
        overflow="hidden"
        pos="relative"
        zIndex={1}
      >
        {children}
      </Box>
    </Flex>
  )
}
