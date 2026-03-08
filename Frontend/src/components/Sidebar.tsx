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

const stepIcons: Record<string, LucideIcon> = {
  home: Home,
  report: Cpu,
  compatibility: ShieldCheck,
  macos: Apple,
  acpi: CircuitBoard,
  kexts: Package,
  smbios: Fingerprint,
  usb: Usb,
  build: Hammer,
  result: PartyPopper,
}

const LANGS = ["en", "ru", "zh", "es"] as const

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
      left={0}
      top={0}
      bottom={0}
      w="var(--sidebar-width)"
      bg="#06060D"
      borderRight="1px solid rgba(255,255,255,0.05)"
      display="flex"
      flexDirection="column"
      zIndex={10}
    >
      {/* Brand */}
      <Box px={5} pt={6} pb={5}>
        <Flex align="center" gap="10px">
          {/* Logo mark */}
          <Box
            w="30px" h="30px" borderRadius="8px"
            background="linear-gradient(135deg, #7B7FFF 0%, #A78BFA 100%)"
            display="flex" alignItems="center" justifyContent="center"
            flexShrink={0}
            boxShadow="0 0 12px rgba(123,127,255,0.35)"
          >
            <Box
              w="10px" h="10px" borderRadius="3px"
              bg="rgba(0,0,0,0.45)"
            />
          </Box>
          <Box>
            <Text
              fontSize="14px"
              fontWeight="700"
              letterSpacing="-0.025em"
              lineHeight={1.15}
              color="#EDF0FF"
            >
              freaky<Text as="span" color="#7B7FFF">OCS</Text>
            </Text>
            <Text
              fontSize="9px"
              color="#252840"
              fontWeight="500"
              letterSpacing="0.09em"
              textTransform="uppercase"
              mt="2px"
            >
              OpenCore Builder
            </Text>
          </Box>
        </Flex>
      </Box>

      <Box h="1px" bg="rgba(255,255,255,0.05)" mx={4} />

      {/* Navigation */}
      <Box flex={1} py={3} overflowY="auto">
        {WIZARD_STEPS.map((step, index) => {
          const isActive = location.pathname === step.path
          const isPast = index < activeIdx && activeIdx !== -1
          const Icon = stepIcons[step.id] ?? Home

          return (
            <Link
              key={step.id}
              to={step.path}
              style={{ textDecoration: "none", display: "block" }}
            >
              <Flex
                align="center"
                gap="10px"
                mx="10px"
                px="10px"
                py="7px"
                borderRadius="8px"
                borderLeft={isActive ? "2px solid #7B7FFF" : "2px solid transparent"}
                bg={isActive ? "rgba(123,127,255,0.09)" : "transparent"}
                transition="all 0.15s ease"
                cursor="pointer"
                _hover={{
                  bg: isActive
                    ? "rgba(123,127,255,0.12)"
                    : "rgba(255,255,255,0.03)",
                }}
              >
                {/* Icon box */}
                <Flex
                  w="22px" h="22px"
                  borderRadius="6px"
                  bg={
                    isActive
                      ? "rgba(123,127,255,0.18)"
                      : isPast
                        ? "rgba(123,127,255,0.06)"
                        : "rgba(255,255,255,0.03)"
                  }
                  align="center"
                  justify="center"
                  flexShrink={0}
                >
                  {isPast && !isActive ? (
                    <Check
                      size={10}
                      strokeWidth={2.5}
                      color="rgba(123,127,255,0.5)"
                    />
                  ) : (
                    <Icon
                      size={11}
                      strokeWidth={isActive ? 2.2 : 1.8}
                      color={
                        isActive
                          ? "#7B7FFF"
                          : isPast
                            ? "rgba(123,127,255,0.4)"
                            : "#252840"
                      }
                    />
                  )}
                </Flex>

                {/* Label */}
                <Text
                  fontSize="12.5px"
                  fontWeight={isActive ? "600" : "400"}
                  color={
                    isActive
                      ? "#EDF0FF"
                      : isPast
                        ? "rgba(123,127,255,0.5)"
                        : "#363B55"
                  }
                  transition="color 0.15s"
                  lineHeight="1"
                  flex={1}
                >
                  {t(`nav.${step.id}`)}
                </Text>
              </Flex>
            </Link>
          )
        })}
      </Box>

      {/* Footer */}
      <Box px={4} py={4} borderTop="1px solid rgba(255,255,255,0.04)">
        <Flex gap="4px" mb={3} wrap="wrap">
          {LANGS.map((lang) => (
            <Box
              key={lang}
              as="button"
              px="8px"
              py="4px"
              borderRadius="5px"
              fontSize="10px"
              fontWeight="600"
              textTransform="uppercase"
              letterSpacing="0.06em"
              cursor="pointer"
              transition="all 0.15s"
              bg={currentLang === lang ? "rgba(123,127,255,0.1)" : "transparent"}
              color={currentLang === lang ? "#7B7FFF" : "#252840"}
              border={`1px solid ${
                currentLang === lang
                  ? "rgba(123,127,255,0.22)"
                  : "transparent"
              }`}
              _hover={{
                color: currentLang === lang ? "#7B7FFF" : "#4A5068",
              }}
              onClick={() => handleLangChange(lang)}
            >
              {lang}
            </Box>
          ))}
        </Flex>
        <Text
          fontSize="10px"
          color="#1E2030"
          fontWeight="500"
          letterSpacing="0.04em"
        >
          v0.1.0 · .NET 10
        </Text>
      </Box>
    </Box>
  )
}

