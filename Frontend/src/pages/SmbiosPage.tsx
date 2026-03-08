import { useEffect, useCallback, useState } from 'react'
import { Box, Text, HStack, Flex, Spinner, Input } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import { usePhotinoInvoke, usePhotinoEvent } from '../bridge/usePhotino'
import type { SmbiosModel, SmbiosConfig } from '../types'
import { Search, ChevronLeft, ChevronRight, Star, RefreshCw, Copy } from 'lucide-react'

const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'

export function SmbiosPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { state, dispatch } = useApp()
  const invoke = usePhotinoInvoke()
  const [models, setModels]               = useState<SmbiosModel[]>([])
  const [isLoading, setIsLoading]         = useState(true)
  const [isGenerating, setIsGenerating]   = useState(false)
  const [selectedModel, setSelectedModel] = useState<string | null>(null)
  const [filter, setFilter]               = useState('')
  const [showAll, setShowAll]             = useState(false)

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
  }, [selectedModel, invoke])

  const filtered = models.filter(
    (m) =>
      m.id.toLowerCase().includes(filter.toLowerCase()) ||
      m.name.toLowerCase().includes(filter.toLowerCase()) ||
      m.cpuFamily.toLowerCase().includes(filter.toLowerCase())
  )

  // Recommended always at top; when showAll=false, only show recommended
  const sortedModels = [...filtered].sort((a, b) => {
    if (a.recommended && !b.recommended) return -1
    if (!a.recommended && b.recommended) return 1
    return 0
  })
  const displayModels = showAll ? sortedModels : sortedModels.filter((m) => m.recommended)
  const hasRecommended = models.some((m) => m.recommended)

  const copyToClipboard = (text: string) => navigator.clipboard.writeText(text)

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0}>

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <Box mb={5}>
        <Text fontSize="22px" fontWeight="700" letterSpacing="-0.03em" color={T} mb={1}>
          {t('smbios.title')}
        </Text>
        <Text color={TS} fontSize="13px">{t('smbios.subtitle')}</Text>
      </Box>

      {/* ── Search + Show All ───────────────────────────────────────────── */}
      <HStack gap={2} mb={3}>
        <Box position="relative" flex={1}>
          <Box position="absolute" left={3} top="50%" transform="translateY(-50%)" zIndex={1}>
            <Search size={13} color={TS} />
          </Box>
          <Input
            pl={9} placeholder={t('smbios.searchPlaceholder')}
            value={filter} onChange={(e) => setFilter(e.target.value)}
            bg={S} border={`1px solid ${B}`} borderRadius="9px"
            color={T} fontSize="13px" h="36px"
            _placeholder={{ color: TM }}
            _focus={{ borderColor: 'rgba(45,212,191,0.35)', outline: 'none', boxShadow: 'none' }}
          />
        </Box>
        {hasRecommended && (
          <Box
            as="button" px={3} h="36px" borderRadius="8px" flexShrink={0}
            bg={showAll ? 'rgba(255,255,255,0.04)' : 'rgba(45,212,191,0.08)'}
            border={`1px solid ${showAll ? B : 'rgba(45,212,191,0.2)'}`}
            color={showAll ? TS : TEAL}
            fontSize="12px" fontWeight="600"
            onClick={() => setShowAll(!showAll)}
            transition="all 0.15s"
            display="flex" alignItems="center" justifyContent="center"
          >
            {showAll ? t('smbios.recommended') : t('smbios.showAll')}
          </Box>
        )}
      </HStack>

      {/* ── Model list ─────────────────────────────────────────────────── */}
      <Box flex={1} overflowY="auto" minH={0} mb={3}>
        {isLoading ? (
          <Flex h="200px" align="center" justify="center" gap={3}
            bg={S} border={`1px solid ${B}`} borderRadius="12px" direction="column"
          >
            <Spinner size="md" color={TEAL} borderWidth="2px" />
            <Text color={TS} fontSize="13px">{t('smbios.loading')}</Text>
          </Flex>
        ) : (
          <Flex direction="column" gap={1.5}>
            {displayModels.map((model) => {
              const isSelected = selectedModel === model.id
              return (
                <Box
                  key={model.id}
                  bg={isSelected ? 'rgba(45,212,191,0.05)' : S}
                  border={`1px solid ${isSelected ? 'rgba(45,212,191,0.25)' : B}`}
                  borderRadius="10px" px={4} py="10px"
                  cursor="pointer"
                  onClick={() => setSelectedModel(model.id)}
                  _hover={{ borderColor: isSelected ? 'rgba(45,212,191,0.4)' : 'rgba(255,255,255,0.1)' }}
                  transition="all 0.15s ease"
                >
                  <Flex justify="space-between" align="center">
                    <HStack gap={3}>
                      <Box
                        w="16px" h="16px" borderRadius="full" flexShrink={0}
                        border={isSelected ? 'none' : '1.5px solid rgba(255,255,255,0.1)'}
                        bg={isSelected ? TEAL : 'transparent'}
                        display="flex" alignItems="center" justifyContent="center"
                        transition="all 0.15s"
                      >
                        {isSelected && <Box w="5px" h="5px" borderRadius="full" bg="#0A0A0A" />}
                      </Box>
                      <Box>
                        <HStack gap={2} mb="2px">
                          <Text color={isSelected ? TEAL : T} fontWeight="600" fontSize="13px">
                            {model.id}
                          </Text>
                          {model.recommended && (
                            <HStack gap={1} px="6px" py="1px" borderRadius="4px"
                              bg="rgba(45,212,191,0.08)" border="1px solid rgba(45,212,191,0.2)">
                              <Star size={9} color={TEAL} fill={TEAL} />
                              <Text fontSize="10px" color={TEAL} fontWeight="600">{t('smbios.bestMatch')}</Text>
                            </HStack>
                          )}
                        </HStack>
                        <Text color={TS} fontSize="11px" mb="4px">{model.name}</Text>
                        <HStack gap={1.5}>
                          <Box px="6px" py="1px" borderRadius="4px"
                            bg="rgba(45,212,191,0.08)" fontSize="10px" color={TEAL}>
                            {model.cpuFamily}
                          </Box>
                          <Box px="6px" py="1px" borderRadius="4px"
                            bg="rgba(167,139,250,0.08)" fontSize="10px" color="#A78BFA">
                            {model.gpuFamily}
                          </Box>
                          <Box px="6px" py="1px" borderRadius="4px"
                            bg="rgba(255,255,255,0.04)" fontSize="10px" color={TM}>
                            {model.year}
                          </Box>
                        </HStack>
                      </Box>
                    </HStack>
                    <Text color={TM} fontSize="11px">macOS {model.minMacOS}+</Text>
                  </Flex>
                </Box>
              )
            })}
          </Flex>
        )}
      </Box>

      {/* ── Generate button ─────────────────────────────────────────────── */}
      {selectedModel && !state.smbios && (
        <Box
          as="button" w="100%" py="9px" borderRadius="9px" mb={3}
          bg={TEAL} color="#0A0A0A" fontWeight="700" fontSize="13px"
          _hover={{ bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' }}
          onClick={handleGenerate}
          opacity={isGenerating ? 0.7 : 1}
          display="flex" alignItems="center" justifyContent="center" gap={2}
          transition="all 0.2s"
        >
          {isGenerating
            ? <><Spinner size="xs" color="#0A0A0A" borderWidth="2px" /> {t('smbios.generating')}</>
            : t('smbios.generate')
          }
        </Box>
      )}

      {/* ── Generated data ──────────────────────────────────────────────── */}
      {state.smbios && (
        <Box bg={S} border="1px solid rgba(45,212,191,0.15)" borderRadius="12px" overflow="hidden" mb={3}>
          <Flex justify="space-between" align="center" px={4} py={3} borderBottom={`1px solid ${B}`}>
            <Text color={T} fontWeight="600" fontSize="13px">{t('smbios.generated')}</Text>
            <Box as="button" px={3} py="5px" borderRadius="6px"
              bg="rgba(255,255,255,0.04)" color={TS} fontSize="11px" fontWeight="500"
              _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
              onClick={handleGenerate} display="flex" alignItems="center" gap={1}
              transition="all 0.15s"
            >
              <RefreshCw size={11} /> {t('smbios.regenerate')}
            </Box>
          </Flex>
          <Flex direction="column" px={4} py={2}>
            {[
              { label: 'Model',  value: state.smbios.model },
              { label: 'Serial', value: state.smbios.serial },
              { label: 'MLB',    value: state.smbios.mlb },
              { label: 'UUID',   value: state.smbios.uuid },
              { label: 'ROM',    value: state.smbios.rom },
            ].map((item, i, arr) => (
              <Flex
                key={item.label} justify="space-between" align="center" py={2}
                borderBottom={i < arr.length - 1 ? `1px solid rgba(255,255,255,0.04)` : 'none'}
              >
                <Text color={TS} fontSize="11px" w="52px" flexShrink={0}>{item.label}</Text>
                <HStack gap={2} flex={1} justify="flex-end">
                  <Text color={T} fontSize="11px" fontFamily="mono" fontWeight="500" truncate>
                    {item.value}
                  </Text>
                  <Box
                    as="button" color={TM} flexShrink={0}
                    _hover={{ color: TEAL }} onClick={() => copyToClipboard(item.value)}
                    transition="color 0.15s"
                  >
                    <Copy size={12} />
                  </Box>
                </HStack>
              </Flex>
            ))}
          </Flex>
        </Box>
      )}

      {/* ── Navigation ─────────────────────────────────────────────────── */}
      <Flex justify="space-between" align="center" pt={4} borderTop={`1px solid ${B}`}>
        <Box as="button" px={4} py="8px" borderRadius="8px"
          bg="rgba(255,255,255,0.04)" color={TS} fontSize="13px" fontWeight="500"
          _hover={{ bg: 'rgba(255,255,255,0.08)', color: T }}
          onClick={() => navigate('/kexts')} display="flex" alignItems="center" gap={2}
          transition="all 0.15s">
          <ChevronLeft size={14} /> {t('nav.kexts')}
        </Box>
        <Box as="button" px={5} py="8px" borderRadius="8px"
          bg={state.smbios ? TEAL : 'rgba(255,255,255,0.06)'}
          color={state.smbios ? '#0A0A0A' : TS}
          fontSize="13px" fontWeight="700"
          _hover={state.smbios ? { bg: '#38E5CE', boxShadow: '0 0 20px rgba(45,212,191,0.3)' } : {}}
          onClick={() => state.smbios && navigate('/usb')}
          cursor={state.smbios ? 'pointer' : 'not-allowed'}
          display="flex" alignItems="center" gap={2}
          opacity={state.smbios ? 1 : 0.4}
          transition="all 0.15s"
        >
          {t('smbios.usbMapping')} <ChevronRight size={14} />
        </Box>
      </Flex>
    </Flex>
  )
}
