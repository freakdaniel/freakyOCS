import { createSystem, defaultConfig, defineConfig } from '@chakra-ui/react'

const config = defineConfig({
  theme: {
    tokens: {
      colors: {
        brand: {
          50:  { value: '#eef2ff' },
          100: { value: '#e0e7ff' },
          200: { value: '#c7d2fe' },
          300: { value: '#a5b4fc' },
          400: { value: '#818cf8' },
          500: { value: '#6366f1' },
          600: { value: '#4f46e5' },
          700: { value: '#4338ca' },
          800: { value: '#3730a3' },
          900: { value: '#312e81' },
          950: { value: '#1e1b4b' },
        },
      },
    },
    semanticTokens: {
      colors: {
        'chakra-body-bg': {
          _light: { value: '{colors.gray.50}' },
          _dark:  { value: '{colors.gray.950}' },
        },
      },
    },
  },
})

export const system = createSystem(defaultConfig, config)
