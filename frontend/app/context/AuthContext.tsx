"use client";

import {
  ApplicationUser,
  AuthContextType,
  ProblemDetails,
  UserResource,
  ValidationProblemDetails
} from "@/types";
import { useRouter } from "next/navigation";
import {
  createContext,
  useContext,
  useState,
  ReactNode,
  useEffect
} from "react";
import { toast } from "sonner";
import { API_BASE, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
// The module-level translator rather than `useTranslation`: this provider is
// mounted *outside* `LocalizationProvider`, because the reader's language is
// resolved from the session and this is what establishes one. A sign-in
// failure is therefore read in English, which is the honest answer — nothing
// yet knows who is typing. @see lib/i18n
import { translateForReader as t } from "@/lib/i18n";

const reportSignInRateLimit = (
  response: Response,
  titleKey: string
): void => {
  const retryAfter = Number(response.headers.get("Retry-After"));
  const seconds = Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null;

  toast.error(t(titleKey), {
    id: "auth-rate-limited",
    description: seconds
      ? t(
          seconds === 1 ? "app.waitAboutOneSecond" : "app.waitAboutSeconds",
          seconds
        )
      : t("app.waitMomentTryAgainYour")
  });
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUserState] = useState<ApplicationUser | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [authResolved, setAuthResolved] = useState<boolean>(false);
  const router = useRouter();

  const setUser = (state: ApplicationUser | null) => {
    if (state === null) {
      setUserState(null);
      localStorage.removeItem("user");
    } else {
      setUserState(state);
      localStorage.setItem("user", JSON.stringify(state));
    }
  };

  const fetchAndSetUser = async (showLoading = true) => {
    if (showLoading) setLoading(true);
    try {
      const user = await fetchUser();
      setUser(user);
      return user;
    } catch {
      setUser(null);
      return null;
    } finally {
      setAuthResolved(true);
      if (showLoading) setLoading(false);
    }
  };

  const refreshUser = () => fetchAndSetUser(false);

  const fetchUser = async (): Promise<ApplicationUser | null> => {
    try {
      const response = await apiFetch(`${API_BASE}/auth/manage/info`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include"
      });

      if (response.ok) {
        const { user, organization } = await unwrap<UserResource>(response);
        return { ...user, organization };
      } else {
        return null;
      }
    } catch {
      return null;
    }
  };

  const logIn = async (username: string, password: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/auth/login?useCookies=true`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ username, password }),
          credentials: "include"
        }
      );

      if (response.ok) {
        await fetchAndSetUser();
        toast.success(t("app.successfullyLogged"));
      } else if (response.status === 429) {
        reportSignInRateLimit(response, "app.tooManySignInAttempts");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        if (errorData.detail === "Failed") {
          toast.error(t("app.invalidCredentials"));
        } else if (errorData.detail === "LockedOut") {
          toast.error(
            t("app.youHaveSubmittedIncorrectCredentials")
          );
        } else {
          toast.error(t("app.errorOccurredDuringLogin"));
        }
      }
    } catch {
      toast.error(t("app.errorOccurredDuringLogin"));
    }
  };

  const register = async (
    username: string,
    email: string,
    password: string
  ) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/auth/register?useCookies=true`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ username, email, password }),
          credentials: "include"
        }
      );

      if (response.ok) {
        await fetchAndSetUser();
        toast.success(t("app.registrationSuccessful"));
      } else if (response.status === 429) {
        reportSignInRateLimit(response, "app.tooManyRegistrationAttempts");
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("app.errorOccurredRegistering"));
      }
    } catch {
      toast.error(t("app.errorOccurredRegistering"));
    }
  };

  const logOut = async () => {
    try {
      const response = await apiFetch(`${API_BASE}/auth/logout`, {
        method: "POST",
        credentials: "include"
      });

      if (response.ok) {
        toast.success(t("app.successfullyLoggedOut"));
        setUser(null);
        router.push("/auth/login");
      } else if (response.status === 429) {
        reportSignInRateLimit(response, "app.tooManySignOutAttempts");
      } else {
        toast.error(t("app.errorOccurredDuringLogout"));
      }
    } catch {
      toast.error(t("app.errorOccurredDuringLogout"));
    }
  };

  useEffect(() => {
    const savedUserState = localStorage.getItem("user");

    if (savedUserState) {
      setUserState(JSON.parse(savedUserState));
      fetchAndSetUser();
    } else {
      setLoading(false);
      refreshUser();
    }
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="h-12 w-12 animate-spin rounded-full border-2 border-brand-600 border-t-transparent" />
      </div>
    );
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        authResolved,
        fetchUser,
        refreshUser,
        register,
        logIn,
        logOut
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};
