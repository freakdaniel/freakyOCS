import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ChakraProvider } from '@chakra-ui/react'
import { HashRouter } from 'react-router-dom'
import { system } from './theme'
import App from './App'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ChakraProvider value={system}>
      <HashRouter>
        <App />
      </HashRouter>
    </ChakraProvider>
  </StrictMode>,
)
