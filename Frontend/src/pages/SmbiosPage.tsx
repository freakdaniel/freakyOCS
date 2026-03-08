import { useEffect, useCallback, useState } from 'react'
import { Box, Heading, Text, VStack, HStack, Flex, Spinner, Input } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { SmbiosModel, SmbiosConfig } from '../types'
import { Fingerprint, Search, ChevronLeft, ChevronRight, Star, RefreshCw, Copy } from 'lucide-react'

const S  = '#0D0D1C'
const B  = 'rgba(255,255,255,0.07)'
const A  = '#7B7FFF'
const AD = 'rgba(123,127,255,'
const T  = '#EDF0FF'
const TS = '#7A829E'
const TM = '#363B52'

export function SmbiosPage() {
  const navigate = useNavigate()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [models, setModels]             = useState<SmbiosModel[]>([])
  const [isLoading, setIsLoading]       = useState(true)
  const [isGenerating, setIsGenerating] = useState(false)
  const [selectedModel, setSelectedModel] = useState<string | null>(null)
  const [filter, setFilter]             = useState('')

  usePhotinoEvent<SmbiosModel[]>('smbios:models', (data) => {
    setIsLoading(false)
    if (data) setModels(data)
  })

  usePhotinoEvent<SmbiosConfig>('smbios:generated', (data) => {
    setIsGenerating(false)
    if (data) dispatch({ type: 'SET_SMBIOS', config: data })
  })

  useEffect(() => {
    invoke('smbios:list', { report: state.report, macos: state.selectedMacOS })
  }, [invoke, state.report, state.selectedMacOS])

  const handleGenerate = useCallback(() => {
    if (!selectedModel) return
    setIsGenerating(true)
    invoke('smbios:generate', { model: selectedModel })
  }, [selectedModel, invoke, dispatch])

  const filteredModels = models.filter(
    (m) =>
      m.id.toLowerCase().includes(filter.toLowerCase()) ||
      m.name.toLowerCase().includes(filter.toLowerCase()) ||
      m.cpuFamily.toLowerCase().includes(filter.toLowerCase())
  )

  const copyToClipboard = (text: string) => navigator.clipboard.writeText(text)

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={6} align="stretch">
        {/* Header */}
        <Box>
          <HStack gap={3} mb={1.5}>
            <Flex w="34px" h="34px" borderRadius="8px" bg={`${AD}0.12)`} align="center" justify="center">
              <Fingerprint size={17} color={A} />
            </Flex>
            <Heading size="lg" color={T} fontWeight="700" letterSpacing="-0.02em">SMBIOS Configuration</Heading>
          </HStack>
          <Text color={TS} fontSize="sm">Select a Mac model that matches your hardware.</Text>
        </Box>

        {/* Search */}
        <Box position="relative">
          <Box position="absolute" left={3} top="50%" transform="translateY(-50%)" zIndex={1}>
            <Search size={14} color={TS} />
          </Box>
          <Input
            pl={9} placeholder="Search models…"
            value={filter} onChange={(e) => setFilter(e.target.value)}
            bg={S} border={`1px solid ${B}`} borderRadius="10px"
            color={T} fontSize="sm"
            _placeholder={{ color: TM }}
            _focus={{ borderColor: `${AD}0.4)`, outline: 'none' }}
          />
        </Box>

        {isLoading ? (
          <Box bg={S} border={`1px solid ${B}`} borderRadius="12px" p={8}>
            <VStack gap={4}><Spinner size="xl" color={A} /><Text color={TS} fontSize="sm">Loading models…</Text></VStack>
          </Box>
        ) : (
          <VStack align="stretch" gap={1.5}>
            {filteredModels.map((model) => {
              const isSelected = selectedModel === model.id
              return (
                <Box
                  key={model.id}
                  bg={isSelected ? `${AD}0.07)` : S}
                  border={isSelected ? `1px solid ${AD}0.28)` : `1px solid ${B}`}
                  borderRadius="10px" px={4} py={3}
                  cursor="pointer"
                  onClick={() => setSelectedModel(model.id)}
                  _hover={{ borderColor: `${AD}0.22)` }}
                  transition="all 0.15s ease"
                >
                  <Flex justify="space-between" align="center">
                    <HStack gap={3}>
                      <Box
                        w="16px" h="16px" borderRadius="full" flexShrink={0}
                        border={isSelected ? 'none' : '2px solid rgba(255,255,255,0.12)'}
                        bg={isSelected ? A : 'transparent'}
                      />
                      <Box>
                        <HStack gap={2}>
                          <Text color={T} fontWeight="500" fontSize="sm">{model.id}</Text>
                          {model.recommended && (
                            <Flex px={1.5} py={0.5} borderRadius="5px" bg="rgba(34,197,94,0.1)" align="center" gap={1}>
                              <Star size={9} color="#22C55E" fill="#22C55E" />
                              <Text fontSize="10px" color="#22C55E" fontWeight="600">Best Match</Text>
                            </Flex>
                          )}
                        </HStack>
                        <Text color={TS} fontSize="xs">{model.name}</Text>
                        <HStack gap={1.5} mt={1}>
                          <Box px={1.5} py={0.5} borderRadius="4px" bg="rgba(45,212,191,0.1)" fontSize="10px" color="#2DD4BF">{model.cpuFamily}</Box>
                          <Box px={1.5} py={0.5} borderRadius="4px" bg="rgba(167,139,250,0.1)" fontSize="10px" color="#A78BFA">{model.gpuFamily}</Box>
                          <Box px={1.5} py={0.5} borderRadius="4px" bg="rgba(255,255,255,0.05)" fontSize="10px" color={TM}>{model.year}</Box>
                        </HStack>
                      </Box>
                    </HStack>
                    <Text color={TM} fontSize="xs">macOS {model.minMacOS}+</Text>
                  </Flex>
                </Box>
              )
            })}
          </VStack>
        )}

        {/* Generate Button */}
        {selectedModel && !state.smbios && (
          <Box
            as="button" w="100%" py="10px" borderRadius="10px"
            bg={A} color="white" fontWeight="600" fontSize="sm"
            _hover={{ bg: '#8F93FF', boxShadow: '0 0 20px rgba(123,127,255,0.3)' }}
            onClick={handleGenerate}
            opacity={isGenerating ? 0.7 : 1}
            display="flex" alignItems="center" justifyContent="center" gap={2}
            transition="all 0.2s"
          >
            {isGenerating ? <><Spinner size="sm" color="white" /> Generating…</> : 'Generate SMBIOS Data'}
          </Box>
        )}

        {/* Generated Data */}
        {state.smbios && (
          <Box bg={S} border={`1px solid ${AD}0.22)`} borderRadius="12px" overflow="hidden">
            <Flex justify="space-between" align="center" px={5} py={3.5} borderBottom={`1px solid ${B}`}>
              <Text color={T} fontWeight="600" fontSize="sm">Generated SMBIOS</Text>
              <Box as="button" px={3} py={1.5} borderRadius="6px"
                bg="rgba(255,255,255,0.04)" color={TS} fontSize="xs" fontWeight="500"
                _hover={{ bg: 'rgba(255,255,255,0.08)' }}
                onClick={handleGenerate} display="flex" alignItems="center" gap={1}
              >
                <RefreshCw size={11} /> Regenerate
              </Box>
            </Flex>
            <VStack align="stretch" gap={0} px={5} py={3}>
              {[
                { label: 'Model',  value: state.smbios.model },
                { label: 'Serial', value: state.smbios.serial },
                { label: 'MLB',    value: state.smbios.mlb },
                { label: 'UUID',   value: state.smbios.uuid },
                { label: 'ROM',    value: state.smbios.rom },
              ].map((item) => (
                <Flex key={item.label} justify="space-between" align="center" py={2} borderBottom={`1px solid rgba(255,255,255,0.04)`}>
                  <Text color={TS} fontSize="xs" w="60px">{item.label}</Text>
                  <HStack gap={2} flex={1} justify="flex-end">
                    <Text color={T} fontSize="xs" fontFamily="mono" fontWeight="500">{item.value}</Text>
                    <Box as="button" color={TM} _hover={{ color: A }} onClick={() => copyToClipboard(item.value)}>
                      <Copy size={12} />
                    </Box>
                  </HStack>
                </Flex>
              ))}
            </VStack>
          </Box>
        )}

        {/* Navigation */}
        <Flex justify="space-between">
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg="rgba(255,255,255,0.04)" color={TS} fontSize="sm" fontWeight="500"
            _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
            onClick={() => navigate('/kexts')} display="flex" alignItems="center" gap={2}>
            <ChevronLeft size={14} /> Kexts
          </Box>
          <Box as="button" px={4} py="8px" borderRadius="8px"
            bg={state.smbios ? A : `${AD}0.3)`}
            color="white" fontSize="sm" fontWeight="600"
            _hover={state.smbios ? { bg: '#8F93FF', boxShadow: '0 0 16px rgba(123,127,255,0.3)' } : {}}
            onClick={() => state.smbios && navigate('/usb')}
            cursor={state.smbios ? 'pointer' : 'not-allowed'}
            display="flex" alignItems="center" gap={2}
          >
            USB Mapping <ChevronRight size={14} />
          </Box>
        </Flex>
      </VStack>
    </Box>
  )
}

