import { Box, Text, HStack, Flex } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  Cpu, Usb, ArrowRight,
  Zap, Shield, Layers,
} from 'lucide-react'

// ── Design tokens ──────────────────────────────────────────────────────────────
const BG   = '#0A0A0A'
const S    = '#111111'
const B    = 'rgba(255,255,255,0.06)'
const TEAL = '#2DD4BF'
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'

// ── Sub-components ─────────────────────────────────────────────────────────────

function FeaturePill({ icon: Icon, label }: { icon: typeof Zap; label: string }) {
  return (
    <HStack
      gap={2} px={3} py="6px"
      borderRadius="8px"
      bg={S} border={`1px solid ${B}`}
      fontSize="12px" fontWeight="500" color={TS}
    >
      <Icon size={12} strokeWidth={1.8} color={TEAL} />
      <Text>{label}</Text>
    </HStack>
  )
}

function ActionCard({
  icon: Icon,
  title,
  description,
  onClick,
  accent = false,
}: {
  icon: typeof Cpu
  title: string
  description: string
  onClick: () => void
  accent?: boolean
}) {
  const { t } = useTranslation()

  return (
    <Box
      bg={accent ? 'rgba(45,212,191,0.06)' : S}
      border={`1px solid ${accent ? 'rgba(45,212,191,0.25)' : B}`}
      borderRadius="14px" p={5}
      cursor="pointer" onClick={onClick}
      transition="all 0.2s ease"
      _hover={{
        bg: accent ? 'rgba(45,212,191,0.1)' : '#161616',
        borderColor: accent ? 'rgba(45,212,191,0.4)' : 'rgba(255,255,255,0.1)',
        transform: 'translateY(-1px)',
        boxShadow: accent
          ? '0 8px 32px rgba(45,212,191,0.1)'
          : '0 8px 24px rgba(0,0,0,0.4)',
      }}
    >
      <Flex direction="column" gap={4}>
        <Flex
          w="40px" h="40px" borderRadius="10px"
          bg={accent ? 'rgba(45,212,191,0.15)' : 'rgba(255,255,255,0.04)'}
          align="center" justify="center"
        >
          <Icon size={18} strokeWidth={1.7} color={accent ? TEAL : TS} />
        </Flex>
        <Box>
          <Text color={T} fontWeight="600" fontSize="13px" mb={1}>{title}</Text>
          <Text color={TS} fontSize="12px" lineHeight="1.6">{description}</Text>
        </Box>
        <HStack color={accent ? TEAL : TM} fontSize="12px" fontWeight="600" gap={1}>
          <Text>{accent ? t('common.get_started') : t('common.open')}</Text>
          <ArrowRight size={12} />
        </HStack>
      </Flex>
    </Box>
  )
}

// ── Page ───────────────────────────────────────────────────────────────────────

export function HomePage() {
  const navigate = useNavigate()
  const { t } = useTranslation()

  return (
    <Flex direction="column" h="100vh" bg={BG} px={7} py={6} gap={0} overflowY="auto">

      {/* ── Hero ─────────────────────────────────────────────────────── */}
      <Box mb={6}>
        {/* Badge */}
        <Flex
          display="inline-flex" align="center" gap={2}
          px="10px" py="4px" borderRadius="20px" mb={4}
          bg="rgba(45,212,191,0.07)" border="1px solid rgba(45,212,191,0.18)"
        >
          <Box w="6px" h="6px" borderRadius="full" bg={TEAL}
            boxShadow={`0 0 6px ${TEAL}`} />
          <Text fontSize="11px" fontWeight="600" color={TEAL} letterSpacing="0.03em">
            OpenCore EFI Builder
          </Text>
        </Flex>

        {/* Heading */}
        <Text
          fontSize="36px" fontWeight="800"
          letterSpacing="-0.04em" lineHeight={1.1}
          color={T} mb={2}
        >
          {t('home.heading')}
          <Text
            as="span" display="block"
            style={{
              background: `linear-gradient(135deg, ${TEAL} 0%, #06B6D4 60%, #818CF8 100%)`,
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
            }}
          >
            freakyOCS
          </Text>
        </Text>

        <Text color={TS} fontSize="13px" maxW="480px" lineHeight={1.7} mb={5}>
          {t('home.subheading')}
        </Text>

        {/* Feature pills */}
        <HStack gap={2} flexWrap="wrap">
          <FeaturePill icon={Zap}    label={t('home.tips_title') !== 'Tips' ? t('home.tips_title') : 'Auto hardware detection'} />
          <FeaturePill icon={Shield} label="Compatibility checker" />
          <FeaturePill icon={Layers} label="Kext management" />
        </HStack>
      </Box>

      {/* ── Divider ──────────────────────────────────────────────────── */}
      <Box h="1px" bg={B} mb={6} />

      {/* ── Quick actions ─────────────────────────────────────────────── */}
      <Box>
        <Text fontSize="10px" fontWeight="700" color={TM}
          textTransform="uppercase" letterSpacing="0.1em" mb={3}>
          Quick Start
        </Text>
        <Flex gap={4} direction={{ base: 'column', md: 'row' }}>
          <ActionCard
            icon={Cpu}
            title={t('home.action_start')}
            description={t('home.action_start_desc')}
            onClick={() => navigate('/report')}
            accent
          />
          <ActionCard
            icon={Usb}
            title={t('home.action_usb')}
            description={t('home.action_usb_desc')}
            onClick={() => navigate('/usb')}
          />
        </Flex>
      </Box>
    </Flex>
  )
}
