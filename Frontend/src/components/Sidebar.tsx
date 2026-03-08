import { Box, Text, Flex } from "@chakra-ui/react"
import { Link, useLocation } from "react-router-dom"
import { useTranslation } from "react-i18next"
import {
  Home,
  Cpu,
  ShieldCheck,
  Apple,
  CircuitBoard,
  Package,
  Fingerprint,
  Usb,
  Hammer,
  PartyPopper,
  Check,
} from "lucide-react"
import type { LucideIcon } from "lucide-react"
import { WIZARD_STEPS } from "../context/AppContext"

// ── Design tokens ──────────────────────────────────────────────────────────────
const T    = '#F5F5F5'
const TS   = '#888888'
const TM   = '#444444'
const TEAL = '#2DD4BF'

const stepIcons: Record<string, LucideIcon> = {
  home:          Home,
  report:        Cpu,
  compatibility: ShieldCheck,
  macos:         Apple,
  acpi:          CircuitBoard,
  kexts:         Package,
  smbios:        Fingerprint,
  usb:           Usb,
  build:         Hammer,
  result:        PartyPopper,
}

const LANGS = ["en", "ru", "zh", "es"] as const

// ── Logo Mark ──────────────────────────────────────────────────────────────────
function LogoMark() {
  return (
    <Box
      w="28px" h="28px" borderRadius="7px"
      background={`linear-gradient(135deg, ${TEAL} 0%, #06B6D4 100%)`}
      display="flex" alignItems="center" justifyContent="center"
      flexShrink={0}
      boxShadow={`0 0 14px rgba(45,212,191,0.4)`}
    >
      {/* Small geometric cutout */}
      <Box
        w="9px" h="9px" borderRadius="2px"
        bg="rgba(0,0,0,0.4)"
      />
    </Box>
  )
}

// ── Step Node ──────────────────────────────────────────────────────────────────
interface StepNodeProps {
  isActive: boolean
  isPast: boolean
  Icon: LucideIcon
}

function StepNode({ isActive, isPast, Icon }: StepNodeProps) {
  if (isActive) {
    return (
      <Flex
        w="24px" h="24px" borderRadius="full" flexShrink={0}
        border={`2px solid ${TEAL}`}
        align="center" justify="center"
        className="timeline-active"
        bg={`rgba(45,212,191,0.1)`}
        position="relative" zIndex={2}
      >
        <Box w="7px" h="7px" borderRadius="full" bg={TEAL} />
      </Flex>
    )
  }

  if (isPast) {
    return (
      <Flex
        w="24px" h="24px" borderRadius="full" flexShrink={0}
        bg={TEAL}
        align="center" justify="center"
        position="relative" zIndex={2}
      >
        <Check size={11} strokeWidth={2.5} color="#0A0A0A" />
      </Flex>
    )
  }

  // Future step
  return (
    <Flex
      w="24px" h="24px" borderRadius="full" flexShrink={0}
      border="1.5px solid rgba(255,255,255,0.08)"
      align="center" justify="center"
      bg="rgba(255,255,255,0.02)"
      position="relative" zIndex={2}
    >
      <Icon size={10} strokeWidth={1.6} color={TM} />
    </Flex>
  )
}

// ── Main Sidebar ───────────────────────────────────────────────────────────────
export function Sidebar() {
  const location = useLocation()
  const { t, i18n } = useTranslation()
  const currentLang = i18n.language.split("-")[0]
  const activeIdx = WIZARD_STEPS.findIndex((s) => s.path === location.pathname)

  const handleLangChange = (lang: string) => {
    i18n.changeLanguage(lang)
    localStorage.setItem("freakyocs-lang", lang)
  }

  return (
    <Box
      as="nav"
      position="fixed"
      left={0} top={0} bottom={0}
      w="var(--sidebar-width)"
      bg="#0D0D0D"
      borderRight="1px solid rgba(255,255,255,0.05)"
      display="flex"
      flexDirection="column"
      zIndex={10}
    >
      {/* ── Brand ──────────────────────────────────────────────────────── */}
      <Box px={5} pt={5} pb={4}>
        <Flex align="center" gap="10px">
          <LogoMark />
          <Box>
            <Text
              fontSize="13px" fontWeight="700"
              letterSpacing="-0.02em" lineHeight={1.2}
              color={T}
            >
              freaky<Text as="span" color={TEAL}>OCS</Text>
            </Text>
            <Text
              fontSize="9px" color={TM} fontWeight="500"
              letterSpacing="0.1em" textTransform="uppercase" mt="1px"
            >
              OpenCore Builder
            </Text>
          </Box>
        </Flex>
      </Box>

      <Box h="1px" bg="rgba(255,255,255,0.04)" mx={5} />

      {/* ── Timeline Navigation ─────────────────────────────────────────── */}
      <Box flex={1} py={3} px={4} overflowY="auto" position="relative">
        {WIZARD_STEPS.map((step, index) => {
          const isActive = location.pathname === step.path
          const isPast   = index < activeIdx && activeIdx !== -1
          const Icon     = stepIcons[step.id] ?? Home

          return (
            <Box key={step.id} position="relative">

              {/* Step row */}
              <Link
                to={step.path}
                style={{ textDecoration: "none", display: "block" }}
              >
                <Flex
                  align="center"
                  gap="10px"
                  px={2} py="3px"
                  borderRadius="8px"
                  cursor="pointer"
                  position="relative"
                  bg={isActive ? "rgba(45,212,191,0.06)" : "transparent"}
                  transition="all 0.2s ease"
                  _hover={{
                    bg: isActive
                      ? "rgba(45,212,191,0.09)"
                      : "rgba(255,255,255,0.03)",
                  }}
                >
                  <StepNode
                    isActive={isActive}
                    isPast={isPast}
                    Icon={Icon}
                  />

                  <Box flex={1} minW={0}>
                    <Text
                      fontSize="12px"
                      fontWeight={isActive ? "600" : "400"}
                      color={
                        isActive ? T
                        : isPast  ? `rgba(45,212,191,0.7)`
                        : TM
                      }
                      transition="color 0.2s"
                      lineHeight="1"
                      truncate
                    >
                      {t(`nav.${step.id}`)}
                    </Text>
                    {isActive && (
                      <Text
                        fontSize="9px"
                        color={`rgba(45,212,191,0.5)`}
                        fontWeight="500"
                        mt="2px"
                        letterSpacing="0.03em"
                      >
                        {t('sidebar.currentStep')}
                      </Text>
                    )}
                  </Box>

                  {/* Active indicator dot on right */}
                  {isActive && (
                    <Box
                      w="5px" h="5px" borderRadius="full"
                      bg={TEAL}
                      flexShrink={0}
                      boxShadow={`0 0 6px ${TEAL}`}
                    />
                  )}
                </Flex>
              </Link>
            </Box>
          )
        })}
      </Box>

      {/* ── Footer ─────────────────────────────────────────────────────── */}
      <Box px={4} py={4} borderTop="1px solid rgba(255,255,255,0.04)">
        {/* Language selector */}
        <Flex gap="3px" mb={3} flexWrap="wrap">
          {LANGS.map((lang) => {
            const isSelected = currentLang === lang
            return (
              <Box
                key={lang}
                as="button"
                px="7px" py="3px"
                borderRadius="5px"
                fontSize="10px"
                fontWeight="600"
                textTransform="uppercase"
                letterSpacing="0.08em"
                cursor="pointer"
                transition="all 0.15s"
                bg={isSelected ? "rgba(45,212,191,0.1)" : "transparent"}
                color={isSelected ? TEAL : TM}
                border={`1px solid ${isSelected ? "rgba(45,212,191,0.25)" : "transparent"}`}
                _hover={{
                  color: isSelected ? TEAL : TS,
                  bg: isSelected ? "rgba(45,212,191,0.1)" : "rgba(255,255,255,0.04)",
                }}
                onClick={() => handleLangChange(lang)}
              >
                {lang}
              </Box>
            )
          })}
        </Flex>

        <Text fontSize="9px" color="rgba(255,255,255,0.15)" fontWeight="500" letterSpacing="0.04em">
          v0.1.0 · .NET 10
        </Text>
      </Box>
    </Box>
  )
}
