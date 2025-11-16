// client/src/main.tsx
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './components/App'

createRoot(document.getElementById('root')!).render(
    // <StrictMode>
    <App />
    // </StrictMode>,
)
