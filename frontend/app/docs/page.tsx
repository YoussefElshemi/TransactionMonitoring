import type { Metadata } from "next";

import { A, Code, CodeBlock, DocPage, DocTable, H2, List, P } from "@/components/docs/Prose";
import { DOCS_API_BASE, DOCS_API_VERSION } from "@/lib/docs/site";

export const metadata: Metadata = { title: "Overview" };

const QUICKSTART = `# 1. Exchange your client credentials for a token
curl -X POST ${DOCS_API_BASE}/oauth/token \\
  -u "$CLIENT_ID:$CLIENT_SECRET" \\
  -d grant_type=client_credentials

# 2. Screen a transaction
curl -X POST ${DOCS_API_BASE}/rule-sets/evaluate \\
  -H "Authorization: Bearer $ACCESS_TOKEN" \\
  -H "Content-Type: application/json" \\
  -H "X-Idempotency-Key: $(uuidgen)" \\
  -H "X-Transaction-Reference: TXN-2026-000482" \\
  -d '{ "amount": 12500.00, "currency": "GBP", "direction": "inbound" }'`;

const ENVELOPE = `{
  "data": { "...": "..." }
}`;

export default function DocsOverviewPage() {
  return (
    <DocPage
      href="/docs"
      title="Transaction Monitoring API"
      lead="Screen a transaction against your rules and get a decision — allow, hold or block — in the same call. The API is JSON over HTTPS, and a first integration needs two requests."
    >
      <H2 id="quickstart">Quickstart</H2>
      <P>
        Create an API client in the console under Organization, then exchange its client id and
        secret for a token and evaluate a transaction. Create the client in test mode first: test
        traffic is never billed and never reaches a regulator.
      </P>
      <CodeBlock label="Shell" code={QUICKSTART} />
      <P>
        The response carries an <Code>outcome.action</Code> of <Code>Allow</Code>,{" "}
        <Code>Flag</Code>, <Code>Review</Code> or <Code>Block</Code>.{" "}
        <A href="/docs/screening">Screening a transaction</A> explains what to do with each.
      </P>

      <H2 id="integration">What an integration uses</H2>
      <List ordered>
        <li>
          An <A href="/docs/authentication">API client</A>, whose id and secret are exchanged for a
          bearer token.
        </li>
        <li>
          <Code>POST /rule-sets/evaluate</Code> for every{" "}
          <A href="/docs/screening">transaction you want screened</A>.
        </li>
        <li>
          A <A href="/docs/webhooks">webhook endpoint</A>, to hear about holds that resolve and cases
          that open after the call has returned.
        </li>
      </List>

      <H2 id="basics">Basics</H2>
      <DocTable
        head={["Item", "Value"]}
        rows={[
          ["Base URL", <Code key="v">{DOCS_API_BASE}</Code>],
          ["Contract version", <Code key="v">{DOCS_API_VERSION}</Code>],
          ["Format", "JSON, UTF-8. Timestamps are ISO 8601 with an offset; identifiers are UUIDs"],
          ["Enumerations", "Sent and returned as strings"],
          [
            "No token needed",
            <>
              <Code>/status</Code>, <Code>/status/heartbeat</Code>, <Code>/status/incidents</Code>,{" "}
              <Code>/api-versions</Code>, <Code>/errors</Code>
            </>,
          ],
        ]}
      />

      <H2 id="responses">Response shape</H2>
      <P>
        A successful response wraps its payload in <Code>data</Code>. Errors use a different shape,
        described under <A href="/docs/errors">Errors</A>. Every endpoint a client can call is
        listed in the <A href="/docs/reference">reference</A>.
      </P>
      <CodeBlock label="JSON" code={ENVELOPE} />
    </DocPage>
  );
}
