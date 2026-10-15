import type { Metadata } from "next";
import { ChevronRight } from "lucide-react";

import { A, Code, DocPage, H2, MethodBadge, P } from "@/components/docs/Prose";
import {
  endpointAnchor,
  type ReferenceEndpoint,
  type ReferenceField,
  type ReferenceParameter,
} from "@/lib/docs/reference";
import { REFERENCE_GROUPS } from "@/lib/docs/reference.generated";

export const metadata: Metadata = { title: "Endpoint reference" };

const ENDPOINT_COUNT = REFERENCE_GROUPS.reduce((total, group) => total + group.endpoints.length, 0);

export default function DocsReferencePage() {
  return (
    <DocPage
      href="/docs/reference"
      title="Endpoint reference"
      lead={`The ${ENDPOINT_COUNT} endpoints an API client can call: the token exchange, the evaluate endpoints its scopes grant, and the ones that need no token at all.`}
    >
      <P>
        Every endpoint also takes the <A href="/docs/conventions#headers">common headers</A> and
        returns the <A href="/docs/errors">common errors</A>, which are not repeated on each row.
        Everything else your organization does — rules, cases, webhooks, the clients themselves —
        is managed in the console and is not part of this API.
      </P>

      {REFERENCE_GROUPS.map((group) => (
        <section key={group.id} className="space-y-3">
          <H2 id={group.id}>{group.title}</H2>
          <div className="divide-y divide-slate-100 overflow-hidden rounded-lg border border-slate-200 bg-card">
            {group.endpoints.map((endpoint) => (
              <EndpointRow key={endpointAnchor(endpoint)} endpoint={endpoint} />
            ))}
          </div>
        </section>
      ))}
    </DocPage>
  );
}

function EndpointRow({ endpoint }: { endpoint: ReferenceEndpoint }) {
  const own = endpoint.parameters;
  const path = own.filter((parameter) => parameter.in === "path");
  const query = own.filter((parameter) => parameter.in === "query");
  const header = own.filter((parameter) => parameter.in === "header");

  return (
    <details open id={endpointAnchor(endpoint)} className="group scroll-mt-24">
      <summary className="flex cursor-pointer list-none flex-wrap items-center gap-x-3 gap-y-1 px-4 py-2.5 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-600 [&::-webkit-details-marker]:hidden">
        <ChevronRight className="size-3.5 flex-shrink-0 text-slate-400 transition-transform group-open:rotate-90" />
        <MethodBadge method={endpoint.method} />
        <span className="break-all font-mono text-xs text-slate-800">{endpoint.path}</span>
        <span className="text-sm text-slate-500 sm:ml-auto sm:text-right">{endpoint.purpose}</span>
      </summary>

      <div className="space-y-4 border-t border-slate-100 bg-slate-50 px-4 py-4 sm:pl-11">
        <p className="flex flex-wrap gap-x-5 gap-y-1 text-xs text-slate-500">
          <span>
            Success <Code>{endpoint.success}</Code>
          </span>
          {endpoint.paged && <span>Paged listing</span>}
          {endpoint.idempotent && (
            <span>
              Accepts <Code>X-Idempotency-Key</Code>
            </span>
          )}
        </p>

        <Fields title="Path parameters" fields={path} />
        <Fields title="Query parameters" fields={query} />
        <Fields title="Headers" fields={header} />

        {endpoint.body &&
          (endpoint.body.freeForm ? (
            <div>
              <FieldsTitle>Request body</FieldsTitle>
              <p className="mt-1 text-xs leading-relaxed text-slate-600">
                Any JSON object: your own transaction, in your own shape.
              </p>
            </div>
          ) : (
            <Fields title={`Request body · ${endpoint.body.contentType}`} fields={endpoint.body.fields} />
          ))}
      </div>
    </details>
  );
}

function FieldsTitle({ children }: { children: React.ReactNode }) {
  return (
    <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">{children}</p>
  );
}

function Fields({ title, fields }: { title: string; fields: (ReferenceParameter | ReferenceField)[] }) {
  if (fields.length === 0) return null;

  return (
    <div>
      <FieldsTitle>{title}</FieldsTitle>
      <ul className="mt-1.5 space-y-1">
        {fields.map((field) => (
          <li key={field.name} className="flex flex-wrap items-baseline gap-x-2 text-xs">
            <span className="font-mono text-slate-800">{field.name}</span>
            <span className="break-all font-mono text-slate-500">{field.type}</span>
            {field.required && (
              <span className="font-mono text-[10px] uppercase tracking-widest text-critical-700">
                required
              </span>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
