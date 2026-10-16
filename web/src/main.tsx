import { initializeTelemetry } from "./telemetry"
import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'

initializeTelemetry()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
