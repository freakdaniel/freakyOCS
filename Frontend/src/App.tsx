import { Routes, Route } from 'react-router-dom'
import { Box } from '@chakra-ui/react'
import { Layout } from './components/Layout'
import { AppProvider } from './context/AppContext'
import {
  HomePage,
  ReportPage,
  CompatibilityPage,
  MacOSVersionPage,
  AcpiPatchesPage,
  KextsPage,
  SmbiosPage,
  UsbMapperPage,
  BuildPage,
  ResultPage,
} from './pages'

export default function App() {
  return (
    <AppProvider>
      <Box minH="100vh" bg="chakra-body-bg">
        <Layout>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/report" element={<ReportPage />} />
            <Route path="/compatibility" element={<CompatibilityPage />} />
            <Route path="/macos" element={<MacOSVersionPage />} />
            <Route path="/acpi" element={<AcpiPatchesPage />} />
            <Route path="/kexts" element={<KextsPage />} />
            <Route path="/smbios" element={<SmbiosPage />} />
            <Route path="/usb" element={<UsbMapperPage />} />
            <Route path="/build" element={<BuildPage />} />
            <Route path="/result" element={<ResultPage />} />
          </Routes>
        </Layout>
      </Box>
    </AppProvider>
  )
}
