import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

import en from './locales/en.json'
import ru from './locales/ru.json'
import zh from './locales/zh.json'
import es from './locales/es.json'

const savedLang = localStorage.getItem('freakyocs-lang') || 'en'

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      ru: { translation: ru },
      zh: { translation: zh },
      es: { translation: es },
    },
    lng: savedLang,
    fallbackLng: 'en',
    interpolation: { escapeValue: false },
  })

export default i18n
