import { useEffect, useState } from "react"
import QRCode from "qrcode"
import { api } from "./api"

export default function TelegramLink() {
  const [linked, setLinked] = useState(false)
  const [challenge, setChallenge] = useState<{ token: string; url: string }>()
  const [phoneCode, setPhoneCode] = useState("")
  const [qr, setQr] = useState("")
  const [sender, setSender] = useState<string | null>(null)
  const [error, setError] = useState("")
  const [busy, setBusy] = useState(false)
  const post = <T,>(path: string, body = {}) => api<T>(`/api/telegram/${path}`, {
    method: "POST", headers: { "X-Bthr-Link": "1" }, body: JSON.stringify(body),
  })
  useEffect(() => { api<{ linked: boolean }>("/api/telegram/status").then(x => setLinked(x.linked)).catch(() => setError("Telegram linking is unavailable.")) }, [])
  useEffect(() => {
    if (!challenge) return
    const timer = window.setInterval(() => {
      post<{ sender: string | null }>("pending", { token: challenge.token })
        .then(x => setSender(x.sender)).catch(() => { setChallenge(undefined); setSender(null); setError("Link expired. Create a new link.") })
    }, 3000)
    return () => window.clearInterval(timer)
  }, [challenge])
  async function action(fn: () => Promise<void>) {
    setBusy(true); setError("")
    try { await fn() } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  return <section className="panel" aria-label="Telegram account linking">
    <h2>Telegram agent</h2>
    <p>{linked ? "Your Telegram account is connected." : "Connect Telegram to manage your records and send meal photos. Photos and requested records may be processed by the configured AI provider."}</p>
    {error && <p role="alert">{error}</p>}
    <button type="button" className="button ghost" disabled={busy} onClick={() => action(async () => {
      if (linked) { await post("unlink"); setLinked(false); setChallenge(undefined); setSender(null) }
      else { const next = await post<{token: string; url: string}>("challenge"); setChallenge(next); setSender(null); setQr(await QRCode.toDataURL(next.url)) }
    })}>{linked ? "Disconnect Telegram" : "Create Telegram link"}</button>
    {!linked && <div>
      <label>Phone-link code from Telegram<input value={phoneCode} maxLength={64} onChange={e => setPhoneCode(e.target.value)} /></label>
      <button type="button" className="button ghost" disabled={busy || !phoneCode} onClick={() => action(async () => {
        await post("phone-attach", { token: phoneCode.trim() }); setQr(""); setSender(null); setChallenge({ token: phoneCode.trim(), url: "" })
      })}>Check phone link</button>
    </div>}
    {challenge && <div>
      <p>Scan this code or open the link. Then approve the Telegram account shown here. Expires in five minutes.</p>
      {qr && <img src={qr} alt="Telegram account linking QR code" width={220} height={220} />}
      {challenge.url && <a href={challenge.url} target="_blank" rel="noreferrer">Open Telegram</a>}
      {sender && <div><p>Approve Telegram user ID: <strong>{sender}</strong>. Check that this matches the ID shown by the bot.</p>
        <button type="button" className="button primary" disabled={busy} onClick={() => action(async () => {
          await post("approve", { token: challenge.token }); setLinked(true); setChallenge(undefined); setSender(null)
        })}>Approve connection</button></div>}
    </div>}
  </section>
}
