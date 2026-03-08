import { Box, Image } from '@chakra-ui/react'

// Import macOS logos
import highSierraLogo from '../assets/images/logos/high_sierra.png'
import mojaveLogo from '../assets/images/logos/mojave.png'
import catalinaLogo from '../assets/images/logos/catalina.png'
import bigSurLogo from '../assets/images/logos/big_sur.png'
import montereyLogo from '../assets/images/logos/monterey.png'
import venturaLogo from '../assets/images/logos/ventura.png'
import sonomaLogo from '../assets/images/logos/sonoma.png'
import sequoiaLogo from '../assets/images/logos/sequoia.png'
import tahoeLogo from '../assets/images/logos/tahoe.png'

// Map version names to their logo images
const MACOS_LOGOS: Record<string, string> = {
  'High Sierra': highSierraLogo,
  'Mojave':      mojaveLogo,
  'Catalina':    catalinaLogo,
  'Big Sur':     bigSurLogo,
  'Monterey':    montereyLogo,
  'Ventura':     venturaLogo,
  'Sonoma':      sonomaLogo,
  'Sequoia':     sequoiaLogo,
  'Tahoe':       tahoeLogo,
}

interface MacOSIconProps {
  name: string
  size?: number
  radius?: string
}

export function MacOSIcon({ name, size = 40, radius = '10px' }: MacOSIconProps) {
  const logoSrc = MACOS_LOGOS[name]

  return (
    <Box
      w={`${size}px`}
      h={`${size}px`}
      borderRadius={radius}
      flexShrink={0}
      overflow="hidden"
      boxShadow="0 2px 12px rgba(0,0,0,0.4)"
      bg="#1a1a1a"
    >
      {logoSrc ? (
        <Image
          src={logoSrc}
          alt={`macOS ${name}`}
          w="100%"
          h="100%"
          objectFit="contain"
          draggable={false}
        />
      ) : (
        <Box
          w="100%"
          h="100%"
          bg="linear-gradient(135deg, #555 0%, #333 100%)"
          display="flex"
          alignItems="center"
          justifyContent="center"
          color="rgba(255,255,255,0.6)"
          fontSize={`${size * 0.4}px`}
          fontWeight="bold"
        >
          ?
        </Box>
      )}
    </Box>
  )
}
