import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { Layout } from './components/Layout'
import { AddPlantPage } from './pages/AddPlantPage'
import { AuthPage } from './pages/AuthPage'
import { PlantDetailPage } from './pages/PlantDetailPage'
import { PlantsPage } from './pages/PlantsPage'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/register" element={<AuthPage mode="register" />} />
          <Route element={<Layout />}>
            <Route index element={<PlantsPage />} />
            <Route path="/plants/new" element={<AddPlantPage />} />
            <Route path="/plants/:id" element={<PlantDetailPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
