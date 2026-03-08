import { Box, Flex } from '@chakra-ui/react'
import type { ReactNode } from 'react'
import { Sidebar } from './Sidebar'

interface LayoutProps {
  children: ReactNode
}

export function Layout({ children }: LayoutProps) {
  return (
    <Flex minH="100vh" bg="#07070F" pos="relative" overflow="hidden">
      {/* Ambient glow blobs — stolen straight from chakra-ui.com */}
      <Box
        className="blob"
        top="-180px"
        left="calc(var(--sidebar-width) + 10%)"
        w="700px" h="700px"
        bg="radial-gradient(circle, rgba(123,127,255,0.07) 0%, transparent 65%)"
      />
      <Box
        className="blob"
        bottom="-200px"
        right="-100px"
        w="500px" h="500px"
        bg="radial-gradient(circle, rgba(45,212,191,0.05) 0%, transparent 65%)"
      />

      <Sidebar />

      <Box
        as="main"
        flex="1"
        ml="var(--sidebar-width)"
        p={7}
        overflowY="auto"
        maxH="100vh"
        pos="relative"
        zIndex={1}
      >
        {children}
      </Box>
    </Flex>
  )
}
