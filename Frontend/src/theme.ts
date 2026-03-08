import { createSystem, defaultConfig, defineConfig } from '@chakra-ui/react'

const config = defineConfig({
  theme: {
    tokens: {
      fonts: {
        heading: { value: "'Inter', -apple-system, BlinkMacSystemFont, sans-serif" },
        body:    { value: "'Inter', -apple-system, BlinkMacSystemFont, sans-serif" },
        mono:    { value: "'Fira Code', 'Cascadia Code', 'Consolas', monospace" },
      },
      colors: {
        brand: {
          50:  { value: '#e6faf8' },
          100: { value: '#b3f0ea' },
          200: { value: '#80e6db' },
          300: { value: '#4ddccc' },
          400: { value: '#2DD4BF' },
          500: { value: '#22b8a5' },
          600: { value: '#1a9c8c' },
          700: { value: '#137f72' },
          800: { value: '#0d6259' },
          900: { value: '#074540' },
          950: { value: '#032926' },
        },
      },
    },
    semanticTokens: {
      colors: {
        'chakra-body-bg': {
          _light: { value: '#0A0A0A' },
          _dark:  { value: '#0A0A0A' },
        },
        'card-bg': {
          _light: { value: '#111111' },
          _dark:  { value: '#111111' },
        },
        'card-border': {
          _light: { value: 'rgba(255, 255, 255, 0.06)' },
          _dark:  { value: 'rgba(255, 255, 255, 0.06)' },
        },
        'text-primary': {
          _light: { value: '#F5F5F5' },
          _dark:  { value: '#F5F5F5' },
        },
        'text-secondary': {
          _light: { value: '#888888' },
          _dark:  { value: '#888888' },
        },
        'text-muted': {
          _light: { value: '#444444' },
          _dark:  { value: '#444444' },
        },
        'accent': {
          _light: { value: '#2DD4BF' },
          _dark:  { value: '#2DD4BF' },
        },
        'teal': {
          _light: { value: '#2DD4BF' },
          _dark:  { value: '#2DD4BF' },
        },
        'success': {
          _light: { value: '#22C55E' },
          _dark:  { value: '#22C55E' },
        },
        'warning': {
          _light: { value: '#EAB308' },
          _dark:  { value: '#EAB308' },
        },
        'danger': {
          _light: { value: '#EF4444' },
          _dark:  { value: '#EF4444' },
        },
      },
    },
  },
})

export const system = createSystem(defaultConfig, config)
