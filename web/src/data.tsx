import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react"

import { api, ApiError } from "./api"

import { RESOURCES } from "./resources"

export type Row = Record<string, any>

export type User = {
  id: number
  username: string
  email: string
  plan: number
  isAdmin?: boolean
}

type Data = {
  user: User | null
  checking: boolean
  sessionError: string
  authenticate: (register: boolean, values: Row) => Promise<void>
  logout: () => Promise<void>
  retrySession: () => void
  records: Record<string, Row[]>
  errors: Record<string, string>
  loading: boolean
  refresh: () => void
  version: number
}

const Context = createContext<Data | null>(null)

export function useData() {
  const value = useContext(Context)
  if (!value) throw new Error("Missing data provider")
  return value
}

export function canAccess(key: string, user: User) {
  return !["goals", "budgets", "investments"].includes(key) || user.plan >= 1
}

export default function DataProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  const [checking, setChecking] = useState(true)

  const [sessionError, setSessionError] = useState("")

  const [records, setRecords] = useState<Record<string, Row[]>>({})

  const [errors, setErrors] = useState<Record<string, string>>({})

  const [loading, setLoading] = useState(false)

  const [version, setVersion] = useState(0)

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const retrySession = useCallback(() => {
    setChecking(true)
    setSessionError("")

    api<User>("/api/auth/me")
      .then(setUser)
      .catch((error) => {
        setUser(null)
        if (!(error instanceof ApiError && error.status === 401))
          setSessionError(error.message)
      })
      .finally(() => setChecking(false))
  }, [])

  useEffect(() => {
    retrySession()

    const expire = () => {
      setUser(null)
      setRecords({})
      setErrors({})
    }

    window.addEventListener("session-expired", expire)

    return () => window.removeEventListener("session-expired", expire)
  }, [retrySession])

  useEffect(() => {
    if (!user) return

    const controller = new AbortController()

    setLoading(true)

    Promise.all(
      RESOURCES.filter((config) => canAccess(config.key, user)).map(
        async (config) => {
          try {
            return {
              key: config.key,
              rows: await api<Row[]>(
                config.basePath.replace("{userId}", String(user.id)),
                { signal: controller.signal },
              ),
              error: "",
            }
          } catch (error) {
            return {
              key: config.key,
              rows: [] as Row[],
              error: (error as Error).message,
            }
          }
        },
      ),
    ).then((results) => {
      if (controller.signal.aborted) return

      setRecords(
        Object.fromEntries(results.map((result) => [result.key, result.rows])),
      )

      setErrors(
        Object.fromEntries(
          results
            .filter((result) => result.error)
            .map((result) => [result.key, result.error]),
        ),
      )

      setLoading(false)
    })

    return () => controller.abort()
  }, [user, version])

  const authenticate = async (register: boolean, values: Row) => {
    await api(`/api/auth/${register ? "register" : "login"}`, {
      method: "POST",
      body: JSON.stringify(values),
    })

    const me = await api<User>("/api/auth/me")
    setRecords({})
    setErrors({})
    setSessionError("")
    setUser(me)
  }

  const logout = async () => {
    await api("/api/auth/logout", { method: "POST" })
    setUser(null)
    setRecords({})
    setErrors({})
  }

  return (
    <Context.Provider
      value={{
        user,
        checking,
        sessionError,
        authenticate,
        logout,
        retrySession,
        records,
        errors,
        loading,
        refresh,
        version,
      }}
    >
      {children}
    </Context.Provider>
  )
}
