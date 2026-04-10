/**
 * OpenTelemetry browser SDK initialization.
 *
 * Sends traces and metrics to the Aspire dashboard via HTTP OTLP.
 * Gracefully no-ops when the OTLP endpoint is not configured (e.g. standalone dev).
 *
 * When APPLICATIONINSIGHTS_CONNECTION_STRING is set, also initializes the
 * Application Insights browser SDK to send telemetry to Azure Monitor.
 *
 * @see https://aspire.dev/dashboard/enable-browser-telemetry/
 */

import { trace, metrics, type Span } from '@opentelemetry/api';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import {
  MeterProvider,
  PeriodicExportingMetricReader,
} from '@opentelemetry/sdk-metrics';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';

declare const __OTEL_EXPORTER_OTLP_ENDPOINT__: string;
declare const __OTEL_EXPORTER_OTLP_HEADERS__: string;
declare const __OTEL_RESOURCE_ATTRIBUTES__: string;
declare const __OTEL_SERVICE_NAME__: string;
declare const __APPLICATIONINSIGHTS_CONNECTION_STRING__: string;

const SERVICE_NAME = 'marginalia-app';

function parseDelimitedValues(s: string): Record<string, string> {
  if (!s) return {};
  const result: Record<string, string> = {};
  for (const pair of s.split(',')) {
    const idx = pair.indexOf('=');
    if (idx > 0) {
      result[pair.slice(0, idx).trim()] = pair.slice(idx + 1).trim();
    }
  }
  return result;
}

function getEndpoint(): string {
  return typeof __OTEL_EXPORTER_OTLP_ENDPOINT__ !== 'undefined'
    ? __OTEL_EXPORTER_OTLP_ENDPOINT__
    : '';
}

function getHeaders(): Record<string, string> {
  const raw =
    typeof __OTEL_EXPORTER_OTLP_HEADERS__ !== 'undefined'
      ? __OTEL_EXPORTER_OTLP_HEADERS__
      : '';
  return parseDelimitedValues(raw);
}

function getResourceAttributes(): Record<string, string> {
  const raw =
    typeof __OTEL_RESOURCE_ATTRIBUTES__ !== 'undefined'
      ? __OTEL_RESOURCE_ATTRIBUTES__
      : '';
  return parseDelimitedValues(raw);
}

function getServiceName(): string {
  const name =
    typeof __OTEL_SERVICE_NAME__ !== 'undefined' ? __OTEL_SERVICE_NAME__ : '';
  return name || SERVICE_NAME;
}

function getAppInsightsConnectionString(): string {
  return typeof __APPLICATIONINSIGHTS_CONNECTION_STRING__ !== 'undefined'
    ? __APPLICATIONINSIGHTS_CONNECTION_STRING__
    : '';
}

let initialized = false;

/**
 * Initialize telemetry. Call once before React renders.
 *
 * Starts the OpenTelemetry SDK (Aspire OTLP) when the OTLP endpoint is set,
 * and the Application Insights browser SDK when the connection string is set.
 * Either or both may be active simultaneously.
 */
export function initTelemetry(): void {
  if (initialized) return;
  initialized = true;

  initOpenTelemetry();
  initApplicationInsights();
}

/**
 * Initialize the OpenTelemetry SDK for the Aspire dashboard.
 * No-ops if the OTLP endpoint is not configured.
 */
function initOpenTelemetry(): void {
  const endpoint = getEndpoint();
  if (!endpoint) {
    console.debug('[Telemetry] OTEL_EXPORTER_OTLP_ENDPOINT not set — telemetry disabled');
    return;
  }

  const headers = getHeaders();
  const attributes = getResourceAttributes();
  attributes[ATTR_SERVICE_NAME] = getServiceName();

  const resource = resourceFromAttributes(attributes);

  // --- Traces ---
  const traceExporter = new OTLPTraceExporter({
    url: `${endpoint}/v1/traces`,
    headers,
  });

  const tracerProvider = new WebTracerProvider({
    resource,
    spanProcessors: [new SimpleSpanProcessor(traceExporter)],
  });
  tracerProvider.register({
    contextManager: new ZoneContextManager(),
  });

  // --- Metrics ---
  const metricExporter = new OTLPMetricExporter({
    url: `${endpoint}/v1/metrics`,
    headers,
  });

  const meterProvider = new MeterProvider({
    resource,
    readers: [
      new PeriodicExportingMetricReader({
        exporter: metricExporter,
        exportIntervalMillis: 10_000,
      }),
    ],
  });
  metrics.setGlobalMeterProvider(meterProvider);

  // --- Auto-instrumentations ---
  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: [/.*/],
      }),
      new UserInteractionInstrumentation(),
    ],
  });

  console.debug('[Telemetry] OpenTelemetry initialized — exporting to', endpoint);
}

/**
 * Initialize the Application Insights browser SDK.
 * No-ops if the connection string is not configured.
 */
function initApplicationInsights(): void {
  const connectionString = getAppInsightsConnectionString();
  if (!connectionString) {
    console.debug('[Telemetry] APPLICATIONINSIGHTS_CONNECTION_STRING not set — App Insights disabled');
    return;
  }

  const appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true,
      disableFetchTracking: false,
      enableCorsCorrelation: true,
      enableRequestHeaderTracking: true,
      enableResponseHeaderTracking: true,
    },
  });
  appInsights.loadAppInsights();

  console.debug('[Telemetry] Application Insights initialized');
}

// --- Shared tracer & meter for custom instrumentation ---

export const tracer = trace.getTracer(SERVICE_NAME);
export const meter = metrics.getMeter(SERVICE_NAME);

// --- Convenience helpers ---

/** Record a duration (ms) on a span and end it. */
export function endSpanWithDuration(span: Span, startMs: number): void {
  span.setAttribute('duration_ms', performance.now() - startMs);
  span.end();
}
