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

// Application Insights — injected in production via container env vars.
const appInsightsConnectionString =
  process.env.APPLICATIONINSIGHTS_CONNECTION_STRING ?? ''

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
    __APPLICATIONINSIGHTS_CONNECTION_STRING__: JSON.stringify(
      appInsightsConnectionString,
    ),
  },
  build: {
    rolldownOptions: {
      // Suppress known eval warning from @protobufjs/inquire (transitive OTel dependency).
      // The library uses eval to dynamically require modules — this is intentional and safe.
      onLog(level, log, defaultHandler) {
        if (log.code === 'EVAL' && log.id?.includes('@protobufjs/inquire')) {
          return
        }
        defaultHandler(level, log)
      },
      output: {
        // Split large vendor bundles into separate chunks for better caching and load performance.
        manualChunks: (id) => {
          if (id.includes('node_modules/@opentelemetry') || id.includes('node_modules/@protobufjs')) {
            return 'vendor-otel'
          }
          if (id.includes('node_modules/@microsoft/applicationinsights')) {
            return 'vendor-appinsights'
          }
          if (
            id.includes('node_modules/react/') ||
            id.includes('node_modules/react-dom/') ||
            id.includes('node_modules/react-router')
          ) {
            return 'vendor-react'
          }
          if (id.includes('node_modules/radix-ui') || id.includes('node_modules/@radix-ui')) {
            return 'vendor-radix'
          }
          if (id.includes('node_modules/lucide-react')) {
            return 'vendor-icons'
          }
          if (id.includes('node_modules')) {
            return 'vendor'
          }
        },
      },
    },
  },
})
