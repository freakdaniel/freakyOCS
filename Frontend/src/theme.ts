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
          50:  { value: '#eeeeff' },
          100: { value: '#d5d5ff' },
          200: { value: '#ababff' },
          300: { value: '#8080ff' },
          400: { value: '#7b7fff' },
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
          _light: { value: '#07070F' },
          _dark:  { value: '#07070F' },
        },
        'card-bg': {
          _light: { value: '#0D0D1C' },
          _dark:  { value: '#0D0D1C' },
        },
        'card-border': {
          _light: { value: 'rgba(255, 255, 255, 0.07)' },
          _dark:  { value: 'rgba(255, 255, 255, 0.07)' },
        },
        'text-primary': {
          _light: { value: '#EDF0FF' },
          _dark:  { value: '#EDF0FF' },
        },
        'text-secondary': {
          _light: { value: '#7A829E' },
          _dark:  { value: '#7A829E' },
        },
        'text-muted': {
          _light: { value: '#363B52' },
          _dark:  { value: '#363B52' },
        },
        'accent': {
          _light: { value: '#7B7FFF' },
          _dark:  { value: '#7B7FFF' },
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
