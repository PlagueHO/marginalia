import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// Aspire injects service URLs as process env vars via WithReference().
// For non-.NET resources the format is services__{name}__{scheme}__{index}
const apiBaseUrl =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  ''

// OpenTelemetry — Aspire injects these env vars when the dashboard is running.
const otelExporterEndpoint = process.env.OTEL_EXPORTER_OTLP_ENDPOINT ?? ''
const otelExporterHeaders = process.env.OTEL_EXPORTER_OTLP_HEADERS ?? ''
const otelResourceAttributes = process.env.OTEL_RESOURCE_ATTRIBUTES ?? ''
const otelServiceName = process.env.OTEL_SERVICE_NAME ?? ''

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  define: {
    __API_BASE_URL__: JSON.stringify(apiBaseUrl),
    __OTEL_EXPORTER_OTLP_ENDPOINT__: JSON.stringify(otelExporterEndpoint),
    __OTEL_EXPORTER_OTLP_HEADERS__: JSON.stringify(otelExporterHeaders),
    __OTEL_RESOURCE_ATTRIBUTES__: JSON.stringify(otelResourceAttributes),
    __OTEL_SERVICE_NAME__: JSON.stringify(otelServiceName),
  },
})
