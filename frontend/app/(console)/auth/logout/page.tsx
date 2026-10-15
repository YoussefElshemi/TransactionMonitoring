"use client";

import { useEffect } from "react";
import { useAuth } from "@/context/AuthContext";
import { useTranslation } from "@/context/LocalizationContext";

const Logout = () => {
  const t = useTranslation();
  const { logOut } = useAuth();

  useEffect(() => {
    logOut();
  }, []);

  return (
    <div className="flex h-screen items-center justify-center bg-slate-50">
      <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {t("auth.signingOut")}
      </p>
    </div>
  );
};

export default Logout;
