import { Outlet } from "react-router-dom";
import { Sidebar } from "./Sidebar";

export function AppShell() {
  return (
    <div className="min-h-screen bg-[#f5f7fb]">
      <Sidebar />
      <div className="lg:pl-[260px]">
        <main className="mx-auto max-w-[1400px] px-6 py-7">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
