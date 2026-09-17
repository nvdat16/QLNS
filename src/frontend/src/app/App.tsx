import { Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "../features/auth/context/AuthContext";
import { LoginPage } from "../features/auth/pages/LoginPage";
import { ProtectedRoute } from "../features/auth/pages/ProtectedRoute";
import { ContractsPage } from "../features/contracts/pages/ContractsPage";
import { EmployeesPage } from "../features/employees/pages/EmployeesPage";
import { InterviewsPage } from "../features/recruitment/pages/InterviewsPage";
import { OffersPage } from "../features/recruitment/pages/OffersPage";
import { PipelinePage } from "../features/recruitment/pages/PipelinePage";
import { RecruitmentLayout } from "../features/recruitment/pages/RecruitmentLayout";
import { RequisitionsPage } from "../features/recruitment/pages/RequisitionsPage";
import { AppShell } from "./layout/AppShell";

export function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<AppShell />}>
            <Route index element={<Navigate to="/employees" replace />} />
            <Route path="employees" element={<EmployeesPage />} />
            <Route path="contracts" element={<ContractsPage />} />
            <Route path="recruitment" element={<RecruitmentLayout />}>
              <Route index element={<Navigate to="/recruitment/pipeline" replace />} />
              <Route path="pipeline" element={<PipelinePage />} />
              <Route path="requisitions" element={<RequisitionsPage />} />
              <Route path="interviews" element={<InterviewsPage />} />
              <Route path="offers" element={<OffersPage />} />
            </Route>
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  );
}
