/**
 * Photino / InfiniFrame message bridge.
 *
 * - C# → JS:  window.external.receiveMessage(jsonString)
 * - JS → C#:  window.chrome.webview.postMessage(jsonString)  [Windows]
 *            window.external.sendMessage(jsonString)        [macOS / Linux]
 */

export interface AppRequest<T = unknown> {
  action: string
  requestId?: string
  payload?: T
}

export interface AppResponse<T = unknown> {
  event: string
  requestId?: string
  data?: T
  error?: string
}

type MessageHandler = (response: AppResponse) => void

const handlers = new Map<string, Set<MessageHandler>>()
let isInitialized = false

function init() {
  if (isInitialized) return
  isInitialized = true

  if (typeof window === 'undefined') return

  // Windows WebView2: C# PostWebMessageAsString → fires 'message' on window.chrome.webview
  const webview = (
    window as unknown as {
      chrome?: { webview?: { addEventListener: (type: string, handler: (e: MessageEvent<string>) => void) => void } }
    }
  ).chrome?.webview
  if (webview?.addEventListener) {
    webview.addEventListener('message', e => dispatch(e.data))
  }

  // macOS/Linux Photino (WebKit/GTK): C# calls window.external.receiveMessage(jsonString)
  if (window.external) {
    const ext = window.external as unknown as Record<string, unknown>
    const prev = ext['receiveMessage'] as ((msg: string) => void) | undefined
    ext['receiveMessage'] = (msg: string) => {
      prev?.(msg)
      dispatch(msg)
    }
  }
}

function dispatch(raw: string) {
  try {
    const response = JSON.parse(raw) as AppResponse
    handlers.get(response.event)?.forEach(h => h(response))
    if (response.requestId) handlers.get(response.requestId)?.forEach(h => h(response))
  } catch (e) {
    console.error('[bridge] Failed to parse message:', raw, e)
  }
}

/** Send a message from React to the .NET backend. */
export function send<T = unknown>(action: string, payload?: T, requestId?: string): string {
  init()
  const id = requestId ?? crypto.randomUUID()
  const msg: AppRequest<T> = { action, requestId: id, payload }
  const json = JSON.stringify(msg)

  if (typeof window !== 'undefined') {
    const w = window as Window & {
      chrome?: { webview?: { postMessage: (msg: string) => void } }
      external?: { sendMessage?: (msg: string) => void }
    }
    if (w.chrome?.webview?.postMessage) {
      w.chrome.webview.postMessage(json)
    } else if (w.external?.sendMessage) {
      w.external.sendMessage(json)
    } else {
      console.warn('[bridge] No postMessage channel available — running in browser?')
    }
  }

  return id
}

/** Subscribe to backend events or one-shot request responses. */
export function on(eventOrRequestId: string, handler: MessageHandler): () => void {
  init()
  if (!handlers.has(eventOrRequestId)) handlers.set(eventOrRequestId, new Set())
  handlers.get(eventOrRequestId)!.add(handler)
  return () => handlers.get(eventOrRequestId)?.delete(handler)
}

/** One-shot: send and await the matching response by requestId. */
export function invoke<TReq = unknown, TRes = unknown>(
  action: string,
  payload?: TReq,
): Promise<AppResponse<TRes>> {
  return new Promise((resolve, reject) => {
    const id = send(action, payload)
    const unsub = on(id, response => {
      unsub()
      if (response.error) reject(new Error(response.error))
      else resolve(response as AppResponse<TRes>)
    })
    // Safety timeout
    setTimeout(() => {
      unsub()
      reject(new Error(`[bridge] Timeout waiting for response to "${action}" (id: ${id})`))
    }, 30_000)
  })
}
