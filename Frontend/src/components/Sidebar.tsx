import { Box, Text, Flex } from "@chakra-ui/react"
import { Link, useLocation } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { useRef, useState, useEffect, type RefObject } from "react"
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

// ── Step Node ──────────────────────────────────────────────────────────────────
interface StepNodeProps {
  isActive:  boolean
  isPast:    boolean
  Icon:      LucideIcon
  isHovered: boolean
}

function StepNode({ isActive, isPast, Icon, isHovered }: StepNodeProps) {
  if (isActive) {
    return (
      <Flex
        w="24px" h="24px" borderRadius="full" flexShrink={0}
        border={`2px solid ${TEAL}`}
        align="center" justify="center"
        className="timeline-active"
        bg="rgba(45,212,191,0.12)"
        position="relative" zIndex={2}
        style={{ transform: 'scale(1.12)', transition: 'transform 0.4s cubic-bezier(0.34,1.56,0.64,1)' }}
      >
        <Box
          w="7px" h="7px" borderRadius="full" bg={TEAL}
          style={{ boxShadow: `0 0 8px ${TEAL}, 0 0 16px rgba(45,212,191,0.4)` }}
        />
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
        style={{
          boxShadow:  isHovered ? '0 0 16px rgba(45,212,191,0.55)' : '0 0 0px transparent',
          transform:  isHovered ? 'scale(1.1)'  : 'scale(1)',
          transition: 'box-shadow 0.3s ease, transform 0.3s cubic-bezier(0.34,1.56,0.64,1)',
        }}
      >
        <Check size={11} strokeWidth={2.5} color="#0A0A0A" />
      </Flex>
    )
  }

  return (
    <Flex
      w="24px" h="24px" borderRadius="full" flexShrink={0}
      align="center" justify="center"
      position="relative" zIndex={2}
      style={{
        border:     isHovered ? '1.5px solid rgba(45,212,191,0.4)' : '1.5px solid rgba(255,255,255,0.08)',
        background: isHovered ? 'rgba(45,212,191,0.07)'           : 'rgba(255,255,255,0.02)',
        transform:  isHovered ? 'scale(1.08)'                     : 'scale(1)',
        transition: 'all 0.25s cubic-bezier(0.34,1.56,0.64,1)',
      }}
    >
      <Icon
        size={10} strokeWidth={1.6}
        color={isHovered ? TEAL : TM}
        style={{ transition: 'color 0.25s ease' }}
      />
    </Flex>
  )
}

// ── Main Sidebar ───────────────────────────────────────────────────────────────
export function Sidebar() {
  const location    = useLocation()
  const { t, i18n } = useTranslation()
  const currentLang = i18n.language.split("-")[0]
  const activeIdx   = WIZARD_STEPS.findIndex((s) => s.path === location.pathname)

  const stepRefs     = useRef<(HTMLDivElement | null)[]>([])
  const containerRef = useRef<HTMLDivElement>(null)
  const [pillStyle, setPillStyle]   = useState({ top: 0, height: 32, opacity: 0 })
  const [pillReady, setPillReady]   = useState(false)   // enables spring after first paint
  const [hoveredIdx, setHoveredIdx] = useState<number | null>(null)

  // ── Calculate sliding-pill position ────────────────────────────────────────
  useEffect(() => {
    if (activeIdx < 0 || !stepRefs.current[activeIdx] || !containerRef.current) {
      setPillStyle(prev => ({ ...prev, opacity: 0 }))
      return
    }
    const el = stepRefs.current[activeIdx]!
    setPillStyle({ top: el.offsetTop, height: el.offsetHeight, opacity: 1 })
  }, [activeIdx, location.pathname])

  // ── Enable spring transition after first paint ──────────────────────────────
  useEffect(() => {
    const raf = requestAnimationFrame(() =>
      requestAnimationFrame(() => setPillReady(true))
    )
    return () => cancelAnimationFrame(raf)
  }, [])

  const handleLangChange = (lang: string) => {
    i18n.changeLanguage(lang)
    localStorage.setItem("freakyocs-lang", lang)
  }

  const pillTransition = pillReady
    ? 'top 0.5s cubic-bezier(0.34,1.56,0.64,1), height 0.35s cubic-bezier(0.34,1.56,0.64,1), opacity 0.35s ease'
    : 'opacity 0.35s ease'

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
      {/* ── Timeline Navigation ─────────────────────────────────────────── */}
      <Box
        flex={1} py={3} px={4}
        overflowY="auto"
        position="relative"
        ref={containerRef as RefObject<HTMLDivElement>}
      >
        {/* ── Magnetic sliding pill ──────────────────────────────────── */}
        <Box
          position="absolute"
          left="16px" right="16px"
          borderRadius="8px"
          pointerEvents="none"
          zIndex={1}
          overflow="hidden"
          style={{
            top:        `${pillStyle.top}px`,
            height:     `${pillStyle.height}px`,
            opacity:    pillStyle.opacity,
            background: 'linear-gradient(90deg, rgba(45,212,191,0.1) 0%, rgba(45,212,191,0.04) 100%)',
            border:     '1px solid rgba(45,212,191,0.22)',
            boxShadow:  '0 0 24px rgba(45,212,191,0.07), inset 0 1px 0 rgba(45,212,191,0.1)',
            transition: pillTransition,
          }}
        >
          {/* Left accent bar */}
          <Box
            position="absolute"
            left={0} top="15%" bottom="15%"
            w="2px" borderRadius="full"
            style={{
              background: `linear-gradient(180deg, transparent, ${TEAL}, transparent)`,
              boxShadow:  `0 0 6px ${TEAL}`,
            }}
          />
          {/* Shimmer sweep */}
          <div className="pill-shimmer" />
        </Box>

        {WIZARD_STEPS.map((step, index) => {
          const isActive  = location.pathname === step.path
          const isPast    = index < activeIdx && activeIdx !== -1
          const Icon      = stepIcons[step.id] ?? Home
          const isHovered = hoveredIdx === index

          return (
            <Box
              key={step.id}
              position="relative"
              ref={(el: HTMLDivElement | null) => { stepRefs.current[index] = el }}
              style={{
                animation:      'sidebar-item-in 0.5s cubic-bezier(0.16,1,0.3,1) both',
                animationDelay: `${index * 38}ms`,
              }}
            >
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
                  zIndex={2}
                  onMouseEnter={() => setHoveredIdx(index)}
                  onMouseLeave={() => setHoveredIdx(null)}
                  style={{
                    transform:  isHovered && !isActive ? 'translateX(3px)' : 'none',
                    transition: 'transform 0.25s cubic-bezier(0.34,1.56,0.64,1)',
                  }}
                >
                  <StepNode
                    isActive={isActive}
                    isPast={isPast}
                    Icon={Icon}
                    isHovered={isHovered}
                  />

                  <Box flex={1} minW={0}>
                    <Text
                      fontSize="12px"
                      fontWeight={isActive ? "600" : "400"}
                      color={
                        isActive   ? T
                        : isPast   ? 'rgba(45,212,191,0.7)'
                        : isHovered ? TS
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
                        color="rgba(45,212,191,0.5)"
                        fontWeight="500"
                        mt="2px"
                        letterSpacing="0.03em"
                        style={{ animation: 'fade-in 0.35s ease both' }}
                      >
                        {t('sidebar.currentStep')}
                      </Text>
                    )}
                  </Box>

                  {/* Active glow dot */}
                  <Box
                    w="5px" h="5px" borderRadius="full"
                    flexShrink={0}
                    style={{
                      background: TEAL,
                      opacity:    isActive ? 1 : 0,
                      transform:  isActive ? 'scale(1)' : 'scale(0)',
                      transition: 'opacity 0.3s ease, transform 0.4s cubic-bezier(0.34,1.56,0.64,1)',
                      boxShadow:  isActive
                        ? `0 0 8px ${TEAL}, 0 0 18px rgba(45,212,191,0.4)`
                        : 'none',
                    }}
                  />
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
