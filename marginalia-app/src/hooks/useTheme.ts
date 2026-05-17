import { useCallback, useEffect, useState } from "react";

export type ThemePreference = "system" | "light" | "dark";
type ResolvedTheme = "light" | "dark";

function resolveTheme(theme: ThemePreference): ResolvedTheme {
  if (theme === "system") {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }
  return theme;
}

function getInitialTheme(): ThemePreference {
  const stored = localStorage.getItem("theme") as ThemePreference | null;
  if (stored === "system" || stored === "light" || stored === "dark") return stored;
  return "system";
}

// Apply the class synchronously before React's first render to prevent a flash.
(function applyThemeClass() {
  const resolvedTheme = resolveTheme(getInitialTheme());
  if (resolvedTheme === "dark") {
    document.documentElement.classList.add("dark");
  } else {
    document.documentElement.classList.remove("dark");
  }
})();

export function useTheme() {
  const [theme, setTheme] = useState<ThemePreference>(getInitialTheme);
  const [systemTheme, setSystemTheme] = useState<ResolvedTheme>(() =>
    window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light"
  );
  const resolvedTheme = theme === "system" ? systemTheme : theme;

  useEffect(() => {
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const handleChange = (event: MediaQueryListEvent) => {
      setSystemTheme(event.matches ? "dark" : "light");
    };

    media.addEventListener("change", handleChange);
    return () => {
      media.removeEventListener("change", handleChange);
    };
  }, []);

  useEffect(() => {
    const root = document.documentElement;
    if (resolvedTheme === "dark") {
      root.classList.add("dark");
    } else {
      root.classList.remove("dark");
    }

    localStorage.setItem("theme", theme);
  }, [theme, resolvedTheme]);

  const toggleTheme = useCallback(() => {
    setTheme((t) => (resolveTheme(t) === "dark" ? "light" : "dark"));
  }, []);

  return { theme, resolvedTheme, setTheme, toggleTheme };
}
