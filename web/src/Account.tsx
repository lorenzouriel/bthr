import { useState } from "react"

import { api } from "./api"

import { useData } from "./data"

export default function Account({
  authentication = false,
}: {
  authentication?: boolean
}) {
  const { user, authenticate, logout, sessionError, retrySession } = useData()

  const [register, setRegister] = useState(
    window.location.pathname === "/register",
  )

  const [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [success, setSuccess] = useState("")

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget

    const values = Object.fromEntries(new FormData(form))
    setBusy(true)
    setError("")
    setSuccess("")

    try {
      if (authentication) await authenticate(register, values)
      else {
        await api("/api/auth/change-password", {
          method: "POST",
          body: JSON.stringify(values),
        })
        form.reset()
        setSuccess("Password changed.")
      }
    } catch (error) {
      setError((error as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className={authentication ? "auth-shell" : "panel account-panel"}>
      <div className={authentication ? "panel" : ""}>
        <span className="eyebrow">Bthr | Your personal space</span>
        <h1 className="page-title">
          {authentication
            ? register
              ? "Create an account"
              : "Welcome back"
            : "Settings"}
        </h1>
        {authentication ? (
          <p>Sign in to your finance, body, and mind records.</p>
        ) : (
          <p>
            {user?.username} | {user?.email} | Plan {user?.plan}
          </p>
        )}
        {sessionError && (
          <div className="api-error" role="alert">
            {sessionError}
            <button className="filter-button" onClick={retrySession}>
              Retry connection
            </button>
          </div>
        )}
        <form className="entry-form" onSubmit={submit}>
          <fieldset disabled={busy}>
            {authentication ? (
              <>
                {register && (
                  <>
                    <label>
                      Username
                      <input
                        name="username"
                        autoComplete="username"
                        required
                        maxLength={100}
                      />
                    </label>
                    <label>
                      Phone number
                      <input
                        name="phoneNumber"
                        type="tel"
                        autoComplete="tel"
                        required
                        maxLength={15}
                      />
                    </label>
                  </>
                )}
                <label>
                  Email
                  <input
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    maxLength={100}
                  />
                </label>
                <label>
                  Password
                  <input
                    name="password"
                    type="password"
                    autoComplete={
                      register ? "new-password" : "current-password"
                    }
                    minLength={register ? 8 : undefined}
                    maxLength={100}
                    required
                  />
                </label>
                {register && (
                  <p className="data-note">
                    Use 8 or more characters, including a number and a special
                    character.
                  </p>
                )}
              </>
            ) : (
              <>
                <h2>Change password</h2>
                <label>
                  Current password
                  <input
                    name="currentPassword"
                    type="password"
                    autoComplete="current-password"
                    required
                  />
                </label>
                <label>
                  New password
                  <input
                    name="newPassword"
                    type="password"
                    autoComplete="new-password"
                    minLength={8}
                    required
                  />
                </label>
                <p className="data-note">
                  Use 8 or more characters, including a number and a special
                  character.
                </p>
              </>
            )}
          </fieldset>
          {error && (
            <p role="alert" className="api-error">
              {error}
            </p>
          )}
          {success && <p role="status">{success}</p>}
          <button className="button primary" disabled={busy}>
            {busy
              ? "Please wait..."
              : authentication
                ? register
                  ? "Create account"
                  : "Log in"
                : "Change password"}
          </button>
        </form>
        {authentication ? (
          <button
            className="text-button"
            disabled={busy}
            onClick={() => {
              setRegister(!register)
              setError("")
            }}
          >
            {register
              ? "Already registered? Log in"
              : "New here? Create an account"}
          </button>
        ) : (
          <button
            className="button ghost"
            disabled={busy}
            onClick={async () => {
              setBusy(true)
              setError("")
              try {
                await logout()
              } catch (error) {
                setError((error as Error).message)
              } finally {
                setBusy(false)
              }
            }}
          >
            Log out
          </button>
        )}
      </div>
    </section>
  )
}
