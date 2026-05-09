import { useEffect } from "react";

const APP_TITLE = "Marginalia";

export function usePageTitle(pageTitle?: string) {
  useEffect(() => {
    const normalizedPageTitle = pageTitle?.trim();
    document.title = normalizedPageTitle
      ? `${normalizedPageTitle} | ${APP_TITLE}`
      : APP_TITLE;
  }, [pageTitle]);
}
