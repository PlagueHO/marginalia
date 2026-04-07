import { useState, useEffect, useCallback } from "react";
import { getAccessStatus } from "@/services/configService";
import { setAccessCode, getApiBaseUrl } from "@/services/api";

const ACCESS_CODE_KEY = "accessCode";

export function useAccessCode() {
  const [accessCodeRequired, setAccessCodeRequired] = useState(false);
  const [isVerified, setIsVerified] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function checkStatus() {
      try {
        const status = await getAccessStatus();

        if (cancelled) return;

        if (!status.accessCodeRequired) {
          setAccessCodeRequired(false);
          setIsVerified(true);
          setIsLoading(false);
          return;
        }

        setAccessCodeRequired(true);

        // Check for a cached access code in sessionStorage
        const cached = sessionStorage.getItem(ACCESS_CODE_KEY);
        if (cached) {
          setAccessCode(cached);

          // Validate the cached code with a probe request
          try {
            const probe = await fetch(
              `${getApiBaseUrl()}/api/config/llm`,
              {
                method: "GET",
                headers: {
                  "Content-Type": "application/json",
                  "X-Access-Code": cached,
                  "X-User-Id": "_anonymous",
                },
              }
            );

            if (!cancelled) {
              if (probe.ok) {
                setIsVerified(true);
              } else {
                // Cached code is invalid — clear it
                sessionStorage.removeItem(ACCESS_CODE_KEY);
                setAccessCode(null);
              }
            }
          } catch {
            if (!cancelled) {
              sessionStorage.removeItem(ACCESS_CODE_KEY);
              setAccessCode(null);
            }
          }
        }
      } catch {
        if (!cancelled) {
          // If we can't reach the server, allow through (will fail on actual API calls)
          setIsVerified(true);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    checkStatus();

    return () => {
      cancelled = true;
    };
  }, []);

  const submitCode = useCallback(async (code: string) => {
    setError(null);
    setAccessCode(code);

    try {
      const probe = await fetch(
        `${getApiBaseUrl()}/api/config/llm`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
            "X-Access-Code": code,
            "X-User-Id": "_anonymous",
          },
        }
      );

      if (probe.ok) {
        sessionStorage.setItem(ACCESS_CODE_KEY, code);
        setIsVerified(true);
      } else {
        setAccessCode(null);
        setError("Invalid access code");
      }
    } catch {
      setAccessCode(null);
      setError("Unable to verify access code. Please try again.");
    }
  }, []);

  return { accessCodeRequired, isVerified, isLoading, error, submitCode };
}
