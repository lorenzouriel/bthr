import { useEffect, useState, type ReactNode } from "react"

import DataProvider, { useData } from "./data"

import Account from "./Account"

import Dashboard from "./Dashboard"

import Reports from "./Reports"

import Records, {
  groups,
  RecordEditor,
  RecordPicker,
  type Editor,
} from "./Records"

type IconName = "home" | "wallet" | "body" | "mind" | "report" | "plus" | "settings" | "calendar" | "check" | "arrow" | "close" | "chevron" | "spark"

type Page = "Overview" | "Finance" | "Body" | "Mind" | "Reports" | "Settings"

const iconPaths: Record<IconName, ReactNode> = {
  home: (
    <>
      <path d="M3 11.5 12 4l9 7.5" />
      <path d="M5.5 10v10h13V10M9 20v-6h6v6" />
    </>
  ),

  wallet: (
    <>
      <path d="M4 6.5A2.5 2.5 0 0 1 6.5 4H19v16H6.5A2.5 2.5 0 0 1 4 17.5Z" />
      <path d="M4 7h15M15 11h6v5h-6a2.5 2.5 0 0 1 0-5Z" />
    </>
  ),

  body: (
    <>
      <path d="M6 9v6M3.5 11v2M18 9v6M20.5 11v2M6 12h12M9 8v8M15 8v8" />
    </>
  ),

  mind: (
    <>
      <path d="M12 4a7 7 0 0 0-4 12.74V20l3-1.5h1a7 7 0 0 0 0-14.5Z" />
      <path d="M9.5 9.5c1.7-1.4 3.8-1 5 .5M10 13h4" />
    </>
  ),

  report: (
    <>
      <path d="M5 4h14v16H5z" />
      <path d="M8 8h8M8 12h8M8 16h5" />
    </>
  ),

  plus: <path d="M12 5v14M5 12h14" />,

  settings: (
    <>
      <circle cx="12" cy="12" r="3" />
      <path d="M19 13.5v-3l-2-.7-.7-1.7.9-1.9-2.1-2.1-1.9.9-1.7-.7-.7-2h-3l-.7 2-1.7.7-1.9-.9-2.1 2.1.9 1.9-.7 1.7-2 .7v3l2 .7.7 1.7-.9 1.9 2.1 2.1 1.9-.9 1.7.7.7 2h3l.7-2 1.7-.7 1.9.9 2.1-2.1-.9-1.9.7-1.7Z" />
    </>
  ),

  calendar: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
    </>
  ),

  check: <path d="m5 12 4 4L19 6" />,

  arrow: (
    <>
      <path d="M12 19V5M7 10l5-5 5 5" />
    </>
  ),

  close: <path d="m6 6 12 12M18 6 6 18" />,

  chevron: <path d="m9 18 6-6-6-6" />,

  spark: (
    <>
      <path d="m12 3 1.3 4.2L17 9l-3.7 1.8L12 15l-1.3-4.2L7 9l3.7-1.8Z" />
      <path d="m18.5 15 .7 2.3 2.3.7-2.3.7-.7 2.3-.7-2.3-2.3-.7 2.3-.7Z" />
    </>
  ),
}

function Icon({ name, size = 18 }: { name: IconName; size?: number }) {
  return (
    <svg
      aria-hidden="true"
      className="icon"
      fill="none"
      height={size}
      viewBox="0 0 24 24"
      width={size}
    >
      {iconPaths[name]}
    </svg>
  )
}

const navItems: { label: string; icon: IconName; tone?: string }[] = [
  { label: "Overview", icon: "home" },

  { label: "Finance", icon: "wallet", tone: "finance" },

  { label: "Body", icon: "body", tone: "body" },

  { label: "Mind", icon: "mind", tone: "mind" },
]

function Sidebar({
  activePage,

  onAdd,

  onNavigate,
}: {
  activePage: Page

  onAdd: () => void

  onNavigate: (page: Page) => void
}) {
  const { user } = useData()

  return (
    <aside className="sidebar">
      <div className="brand">
        <div className="brand-mark">
          <span />
          <span />
          <span />
        </div>
        <div>Bthr</div>
      </div>
      <nav className="nav-main" aria-label="Primary navigation">
        {navItems.map((item, index) => (
          <button
            className={`nav-item ${activePage === item.label ? "active" : ""}`}
            key={item.label}
            onClick={() => onNavigate(item.label as Page)}
          >
            <span className={`nav-icon ${item.tone || ""}`}>
              <Icon name={item.icon} />
            </span>
            {item.label}
          </button>
        ))}
      </nav>
      <div className="nav-secondary">
        <button
          className={`nav-item ${activePage === "Reports" ? "active" : ""}`}
          onClick={() => onNavigate("Reports")}
        >
          <span className="nav-icon">
            <Icon name="report" />
          </span>
          Reports
        </button>
        <button className="nav-item" onClick={onAdd}>
          <span className="nav-icon">
            <Icon name="plus" />
          </span>
          Add record
        </button>
      </div>
      <div className="sidebar-spacer" />
      <button className="nav-item" onClick={() => onNavigate("Settings")}>
        <span className="nav-icon">
          <Icon name="settings" />
        </span>
        Settings
      </button>
      <div className="profile">
        <div className="avatar">{user?.username.slice(0, 2).toUpperCase()}</div>
        <div>
          <strong>{user?.username}</strong>
          <small>{user?.email}</small>
        </div>
        <Icon name="chevron" size={15} />
      </div>
    </aside>
  )
}

const domainNavigation = {
  Finance: [
    "Overview",
    "Transactions",
    "Bills",
    "Budgets",
    "Goals",
    "Investments",
  ],

  Body: [
    "Overview",
    "Training",
    "Nutrition",
    "Recovery",
    "Habits",
    "Measurements",
    "Health Log",
  ],

  Mind: ["Overview", "Meditation", "Journal"],
}

function DomainHeader({
  domain,

  description,

  action,

  section,

  onSectionChange,

  onAction,
}: {
  domain: "Finance" | "Body" | "Mind"

  description: string

  action: string

  section: string

  onSectionChange: (section: string) => void

  onAction: () => void
}) {
  return (
    <>
      <section className="domain-heading">
        <div>
          <span className={`domain-overline ${domain.toLowerCase()}`}>
            {domain} OS
          </span>
          <div className="page-title">{domain}</div>
          <p>{description}</p>
        </div>
        <button
          className={`button domain-action ${domain.toLowerCase()}`}
          onClick={onAction}
        >
          <Icon name="plus" size={16} />
          {action}
        </button>
      </section>
      <nav className="subnav" aria-label={`${domain} sections`}>
        {domainNavigation[domain].map((item) => (
          <button
            className={section === item ? "active" : ""}
            key={item}
            onClick={() => onSectionChange(item)}
          >
            {item}
          </button>
        ))}
      </nav>
    </>
  )
}

function slug(value: string) {
  return value.toLowerCase().replace(/ /g, "-")
}

function locationState() {
  let path: string[]

  try {
    path = window.location.pathname
      .split("/")
      .filter(Boolean)
      .map(decodeURIComponent)
  } catch {
    return {
      page: "Overview" as Page,
      section: "Overview",
      resource: undefined,
      valid: false,
    }
  }

  const pages: Record<string, Page> = {
    overview: "Overview",
    login: "Overview",
    register: "Overview",
    finance: "Finance",
    body: "Body",
    mind: "Mind",
    reports: "Reports",
    settings: "Settings",
    account: "Settings",
  }

  const page = pages[path[0] ?? "overview"]

  const sections = page && groups[page]

  const section = path[1]
    ? Object.keys(sections ?? {}).find((name) => slug(name) === path[1])
    : "Overview"

  return {
    page: page ?? "Overview",
    section: section ?? "Overview",
    resource: path[2],
    valid:
      !!page &&
      !!section &&
      path.length <= 3 &&
      (!path[2] || !!sections?.[section!]?.includes(path[2])) &&
      (!(page === "Reports" || page === "Settings" || page === "Overview") ||
        path.length <= 1),
  }
}

function Workspace() {
  const { user, checking } = useData()

  const [route, setRoute] = useState(locationState)

  const [picker, setPicker] = useState(false),
    [editor, setEditor] = useState<Editor | null>(null)

  useEffect(() => {
    const changed = () => {
      setRoute(locationState())
      setEditor(null)
      setPicker(false)
    }
    window.addEventListener("popstate", changed)
    return () => window.removeEventListener("popstate", changed)
  }, [])

  useEffect(() => {
    if (!user) {
      setEditor(null)
      setPicker(false)
    }
  }, [user])

  function navigate(page: Page, section = "Overview", resource?: string) {
    const url = `/${slug(page)}${
      section === "Overview" ? "" : "/" + slug(section)
    }${resource ? "/" + resource : ""}`

    window.history.pushState(null, "", url)
    setRoute(locationState())
    setEditor(null)
    setPicker(false)
    window.scrollTo(0, 0)
  }

  if (checking)
    return (
      <div className="auth-shell" role="status">
        Restoring your session...
      </div>
    )

  if (!user) return <Account authentication />

  const { page, section } = route

  const resourceKeys = groups[page]?.[section] ?? []

  const key = route.resource ?? resourceKeys[0]

  const isDomain = ["Finance", "Body", "Mind"].includes(page)

  const add = () => (key ? setEditor({ key }) : setPicker(true))

  return (
    <div className="app-shell">
      <Sidebar
        activePage={page}
        onAdd={() => setPicker(true)}
        onNavigate={(page) => navigate(page)}
      />
      <main className="main">
        <header className="topbar">
          <div className="mobile-brand">
            <div className="brand-mark">
              <span />
              <span />
              <span />
            </div>
            Bthr
          </div>
          <button
            className="mobile-report"
            onClick={() => navigate("Reports")}
            aria-label="Reports"
          >
            <Icon name="report" />
          </button>
          <div className="date-button">
            <Icon name="calendar" />
            {new Date().toLocaleDateString(undefined, {
              month: "long",
              day: "numeric",
              year: "numeric",
            })}
          </div>
          <button
            className="icon-button"
            onClick={() => navigate("Settings")}
            aria-label="Settings"
          >
            <Icon name="settings" />
          </button>
          <button className="button add-button" onClick={() => setPicker(true)}>
            <Icon name="plus" />
            Add record
          </button>
        </header>
        <div
          className={`content ${page !== "Overview" ? "domain-content" : ""}`}
        >
          {!route.valid ? (
            <section className="panel">
              <h1>Page not found</h1>
              <button
                className="button primary"
                onClick={() => navigate("Overview")}
              >
                Go to overview
              </button>
            </section>
          ) : isDomain ? (
            <div className="domain-page">
              <DomainHeader
                domain={page as "Finance" | "Body" | "Mind"}
                description={
                  page === "Finance"
                    ? "Your money, with context, not noise."
                    : page === "Body"
                      ? "Training, fuel, and recovery in one rhythm."
                      : "A quieter view of attention, mood, and reflection."
                }
                action="Add record"
                section={section}
                onSectionChange={(section) => navigate(page, section)}
                onAction={add}
              />
              {section === "Overview" ? (
                <Dashboard
                  key={page}
                  domain={page}
                  onSection={(section) => navigate(page, section)}
                  onAdd={setEditor}
                  onNavigate={(page) => navigate(page as Page)}
                />
              ) : (
                <>
                  <nav className="live-resource-tabs" aria-label="Record types">
                    {resourceKeys.map((resource) => (
                      <button
                        key={resource}
                        className={key === resource ? "active" : ""}
                        onClick={() => navigate(page, section, resource)}
                      >
                        {resource.replace(/-/g, " ")}
                      </button>
                    ))}
                  </nav>
                  <Records key={key} resourceKey={key} onEdit={setEditor} />
                </>
              )}
            </div>
          ) : page === "Reports" ? (
            <Reports />
          ) : page === "Settings" ? (
            <Account />
          ) : (
            <Dashboard
              domain="Overview"
              onSection={() => {}}
              onAdd={setEditor}
              onNavigate={(page) => navigate(page as Page)}
            />
          )}
        </div>
      </main>
      <nav className="mobile-nav" aria-label="Mobile navigation">
        {(["Overview", "Finance", "Body", "Mind"] as Page[]).map(
          (item, index) => (
            <button
              key={item}
              className={item === page ? "active" : ""}
              onClick={() => navigate(item)}
            >
              <Icon
                name={(["home", "wallet", "body", "mind"] as IconName[])[index]}
              />
              <span>{item}</span>
            </button>
          ),
        )}
        <button onClick={() => setPicker(true)} aria-label="Add record">
          <Icon name="plus" />
          <span>Add</span>
        </button>
      </nav>
      {picker && (
        <RecordPicker
          onClose={() => setPicker(false)}
          onSelect={(selection) => {
            setPicker(false)
            setEditor(selection)
          }}
        />
      )}
      {editor && (
        <RecordEditor
          key={`${editor.key}-${editor.row?.id ?? "new"}`}
          editor={editor}
          onClose={() => setEditor(null)}
        />
      )}
    </div>
  )
}

export default function App() {
  return (
    <DataProvider>
      <Workspace />
    </DataProvider>
  )
}
