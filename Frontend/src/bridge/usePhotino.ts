import { useEffect, useCallback } from 'react'
import { on, send, invoke, type AppResponse } from './photino'

/** Subscribe to a backend event for the lifetime of the component. */
export function usePhotinoEvent<T = unknown>(
  event: string,
  handler: (data: T | undefined, response: AppResponse<T>) => void,
) {
  useEffect(() => {
    const unsub = on(event, response => handler(response.data as T | undefined, response as AppResponse<T>))
    return unsub
  }, [event, handler])
}

/** Returns a stable send function. */
export function usePhotinoSend() {
  return useCallback(send, [])
}

/** Returns a stable invoke function. */
export function usePhotinoInvoke() {
  return useCallback(invoke, [])
}
