import { Box, Heading, Text, VStack, HStack, Flex, SimpleGrid } from '@chakra-ui/react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useApp } from '../context/AppContext'
import {
  Cpu,
  Usb,
  ChevronRight,
  ArrowRight,
  Monitor,
  Apple,
  Package,
  Zap,
  Shield,
  Layers,
} from 'lucide-react'

// ── Shared design tokens ────────────────────────────────────────────
const C = {
  surface:   '#0D0D1C',
  border:    'rgba(255,255,255,0.07)',
  borderHov: 'rgba(255,255,255,0.13)',
  accent:    '#7B7FFF',
  accentDim: 'rgba(123,127,255,0.09)',
  accentBd:  'rgba(123,127,255,0.28)',
  teal:      '#2DD4BF',
  tealDim:   'rgba(45,212,191,0.08)',
  text:      '#EDF0FF',
  sub:       '#7A829E',
  muted:     '#363B52',
}

function StatCard({
  icon: Icon,
  label,
  value,
  color,
}: {
  icon: React.FC<{ size?: number; strokeWidth?: number }>
  label: string
  value: string
  color: string
}) {
  return (
    <Box
      bg={C.surface}
      border={`1px solid ${C.border}`}
      borderRadius="12px"
      p={4}
      transition="border-color 0.2s"
      _hover={{ borderColor: C.borderHov }}
    >
      <HStack gap={3.5}>
        <Flex
          w="38px" h="38px"
          borderRadius="9px"
          bg={`${color}18`}
          align="center"
          justify="center"
          flexShrink={0}
        >
          <Icon size={18} strokeWidth={1.8} />
        </Flex>
        <Box>
          <Text fontSize="11px" color={C.muted} fontWeight="500" letterSpacing="0.04em" textTransform="uppercase">
            {label}
          </Text>
          <Text fontSize="sm" color={C.text} fontWeight="600" mt="2px">
            {value}
          </Text>
        </Box>
      </HStack>
    </Box>
  )
}

function ActionCard({
  icon: Icon,
  title,
  description,
  onClick,
  accent = false,
}: {
  icon: React.FC<{ size?: number; strokeWidth?: number }>
  title: string
  description: string
  onClick: () => void
  accent?: boolean
}) {
  const { t } = useTranslation()

  return (
    <Box
      bg={accent
        ? 'linear-gradient(135deg, rgba(123,127,255,0.11) 0%, rgba(123,127,255,0.04) 100%)'
        : C.surface}
      border={accent ? `1px solid ${C.accentBd}` : `1px solid ${C.border}`}
      borderRadius="14px"
      p={6}
      cursor="pointer"
      onClick={onClick}
      transition="all 0.2s ease"
      _hover={{
        bg: accent
          ? 'linear-gradient(135deg, rgba(123,127,255,0.17) 0%, rgba(123,127,255,0.08) 100%)'
          : '#111120',
        borderColor: accent ? 'rgba(123,127,255,0.5)' : C.borderHov,
        transform: 'translateY(-2px)',
        boxShadow: accent
          ? '0 8px 32px rgba(123,127,255,0.12)'
          : '0 8px 24px rgba(0,0,0,0.4)',
      }}
    >
      <VStack align="start" gap={4}>
        <Flex
          w="44px" h="44px"
          borderRadius="11px"
          bg={accent ? 'rgba(123,127,255,0.15)' : 'rgba(255,255,255,0.04)'}
          align="center"
          justify="center"
        >
          <Icon size={20} strokeWidth={1.7}
            //color={accent ? C.accent : C.sub}
          />
        </Flex>
        <Box>
          <Text color={C.text} fontWeight="600" fontSize="sm" mb={1.5}>
            {title}
          </Text>
          <Text color={C.sub} fontSize="xs" lineHeight="1.6">
            {description}
          </Text>
        </Box>
        <HStack
          color={accent ? C.accent : C.muted}
          fontSize="xs"
          fontWeight="600"
          gap={1.5}
        >
          <Text>{accent ? t('common.get_started') : t('common.open')}</Text>
          <ChevronRight size={13} />
        </HStack>
      </VStack>
    </Box>
  )
}

function FeaturePill({ icon: Icon, label }: { icon: typeof Zap; label: string }) {
  return (
    <HStack
      gap={2}
      px={3} py="7px"
      borderRadius="8px"
      bg={C.surface}
      border={`1px solid ${C.border}`}
      fontSize="xs"
      fontWeight="500"
      color={C.sub}
    >
      <Icon size={13} strokeWidth={1.8} color={C.accent} />
      <Text>{label}</Text>
    </HStack>
  )
}

export function HomePage() {
  const navigate = useNavigate()
  const { state } = useApp()
  const { t } = useTranslation()

  const hasReport   = !!state.report
  const hasMacOS    = !!state.selectedMacOS
  const enabledKexts = state.kexts.filter(k => k.enabled).length
  const hasProgress = hasReport || hasMacOS || enabledKexts > 0

  return (
    <Box maxW="860px" mx="auto">
      <VStack gap={10} align="stretch">

        {/* ── Hero ─────────────────────────────────────────────── */}
        <Box pt={4} pb={2} pos="relative">
          {/* Badge */}
          <Flex align="center" gap={2} mb={5}>
            <Box
              px="10px" py="4px"
              borderRadius="20px"
              bg="rgba(45,212,191,0.08)"
              border="1px solid rgba(45,212,191,0.2)"
              fontSize="11px"
              fontWeight="600"
              color={C.teal}
              letterSpacing="0.02em"
            >
              OpenCore EFI Builder
            </Box>
          </Flex>

          {/* Heading */}
          <Heading
            fontSize={{ base: '32px', md: '42px' }}
            fontWeight="800"
            letterSpacing="-0.035em"
            lineHeight={1.12}
            mb={4}
            color={C.text}
          >
            {t('home.heading')}
            <Text
              as="span"
              display="block"
              bgGradient="to-r"
              gradientFrom="#7B7FFF"
              gradientTo="#2DD4BF"
              bgClip="text"
              color="transparent"
            >
              freakyOCS
            </Text>
          </Heading>

          <Text
            color={C.sub}
            fontSize="sm"
            maxW="500px"
            lineHeight={1.7}
            mb={6}
          >
            {t('home.subheading')}
          </Text>

          {/* CTA row */}
          <Flex gap={3} flexWrap="wrap">
            <Box
              as="button"
              onClick={() => navigate('/report')}
              display="inline-flex" alignItems="center" gap={2}
              px={5} py="10px"
              borderRadius="10px"
              bg={C.accent}
              color="white"
              fontSize="sm"
              fontWeight="600"
              letterSpacing="-0.01em"
              transition="all 0.2s"
              _hover={{
                bg: '#8F93FF',
                boxShadow: '0 0 20px rgba(123,127,255,0.4)',
                transform: 'translateY(-1px)',
              }}
            >
              {t('home.action_start')} <ArrowRight size={15} />
            </Box>
            <Box
              as="button"
              onClick={() => navigate('/usb')}
              display="inline-flex" alignItems="center" gap={2}
              px={5} py="10px"
              borderRadius="10px"
              bg="rgba(255,255,255,0.04)"
              border={`1px solid ${C.border}`}
              color={C.sub}
              fontSize="sm"
              fontWeight="500"
              transition="all 0.2s"
              _hover={{ bg: 'rgba(255,255,255,0.07)', borderColor: C.borderHov, color: C.text }}
            >
              USB Mapper
            </Box>
          </Flex>

          {/* Feature pills */}
          <Flex gap={2} mt={6} flexWrap="wrap">
            <FeaturePill icon={Zap}    label="Auto hardware detection" />
            <FeaturePill icon={Shield} label="Compatibility checker" />
            <FeaturePill icon={Layers} label="Kext management" />
          </Flex>
        </Box>

        {/* Divider */}
        <Box h="1px" bg={C.border} />

        {/* ── Status Cards (only if wizard has started) ──────── */}
        {hasProgress && (
          <>
            <Box>
              <Text
                fontSize="11px"
                fontWeight="600"
                color={C.muted}
                textTransform="uppercase"
                letterSpacing="0.08em"
                mb={3}
              >
                Current Progress
              </Text>
              <SimpleGrid columns={{ base: 1, md: 3 }} gap={3}>
                <StatCard
                  icon={Monitor}
                  label={t('home.stat_hardware')}
                  value={hasReport ? state.report!.cpu.codename : t('common.not_detected')}
                  color={hasReport ? '#22C55E' : C.muted}
                />
                <StatCard
                  icon={Apple}
                  label={t('home.stat_macos')}
                  value={hasMacOS ? state.selectedMacOS!.name : t('common.not_selected')}
                  color={hasMacOS ? C.accent : C.muted}
                />
                <StatCard
                  icon={Package}
                  label={t('home.stat_kexts')}
                  value={enabledKexts > 0 ? `${enabledKexts} kexts` : t('common.none')}
                  color={enabledKexts > 0 ? '#EAB308' : C.muted}
                />
              </SimpleGrid>
            </Box>
            <Box h="1px" bg={C.border} />
          </>
        )}

        {/* ── Quick Actions ─────────────────────────────────── */}
        <Box>
          <Text
            fontSize="11px"
            fontWeight="600"
            color={C.muted}
            textTransform="uppercase"
            letterSpacing="0.08em"
            mb={3}
          >
            Quick Start
          </Text>
          <SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
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
          </SimpleGrid>
        </Box>

      </VStack>
    </Box>
  )
}