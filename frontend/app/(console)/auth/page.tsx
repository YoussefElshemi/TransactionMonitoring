"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";

const Auth = () => {
  const { fetchUser } = useAuth();
  const router = useRouter();

  useEffect(() => {
    fetchUser().then((user) => {
      if (user) {
        router.push("/");
      } else {
        router.push("/auth/login");
      }
    });
  }, [router]);
};

export default Auth;
