import type { Metadata } from "next";

import { A, Code, CodeBlock, DocPage, DocTable, H2, P } from "@/components/docs/Prose";

export const metadata: Metadata = { title: "Errors" };

const PROBLEM = `{
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more fields failed validation.",
  "instance": "/rule-sets/evaluate",
  "code": "validation_failed",
  "correlationId": "e8399595-5b2b-4a69-b4f7-27c122fcce48",
  "errors": { "amount": ["'amount' is required."] }
}`;

const code = (...codes: string[]) => (
  <>
    {codes.map((value, index) => (
      <span key={value}>
        {index > 0 && ", "}
        <Code>{value}</Code>
      </span>
    ))}
  </>
);

export default function DocsErrorsPage() {
  return (
    <DocPage
      href="/docs/errors"
      title="Errors"
      lead="An error is an RFC 7807 problem document with two additions: a stable code to branch on, and the correlation id to quote when reporting it."
    >
      <CodeBlock label="400 Bad Request" code={PROBLEM} />
      <DocTable
        head={["Member", "Notes"]}
        rows={[
          [<Code key="m">code</Code>, "A stable identifier. Branch on this"],
          [<Code key="m">status</Code>, "The same as the HTTP status"],
          [code("title", "detail"), "For a person to read. The wording can change without notice"],
          [<Code key="m">correlationId</Code>, "The same value as the X-Correlation-ID response header"],
          [<Code key="m">errors</Code>, "On validation failures only: each field, and what was wrong with it"],
        ]}
      />
      <P>
        Two responses do not follow this shape. A 401 has an empty body, and{" "}
        <Code>POST /oauth/token</Code> answers in the{" "}
        <A href="/docs/authentication#token-errors">OAuth error shape</A>.
      </P>

      <H2 id="codes">Codes every integration should handle</H2>
      <DocTable
        head={["Status", "Code", "What to do"]}
        rows={[
          ["400", code("validation_failed"), "Fix the fields named in errors. Do not retry unchanged"],
          ["400", code("bad_request"), "The JSON is malformed, a value has the wrong type, or the body is too large"],
          ["400", code("payload_schema_violation"), "The transaction payload does not match your transaction schema"],
          ["400", code("transaction_payload_mismatch"), "The transaction reference was already used with a different payload"],
          ["400", code("invalid_idempotency_key", "unsupported_api_version"), "A header carries a value that is not accepted"],
          ["401", code("unauthorized"), "Get a new token, then retry once"],
          ["402", code("sandbox_credits_exhausted"), "The month's sandbox allowance has been spent"],
          ["403", code("forbidden"), "The client's scopes do not cover this endpoint. Do not retry"],
          ["404", code("not_found"), "The rule or rule set is missing, or in another mode or another organization"],
          ["409", code("idempotency_request_in_progress"), "Retry after a short wait"],
          ["409", code("idempotency_key_reused"), "Generate a new key"],
          ["429", code("rate_limited"), "Wait the number of seconds in Retry-After"],
          ["500", code("internal_error"), "Retry with the same idempotency key. Report the correlation id if it persists"],
          ["503", code("service_unavailable"), "Retry with backoff"],
        ]}
      />
      <P>
        <Code>GET /errors</Code> returns the complete, current catalogue with each code&apos;s status
        and description, and needs no token. For a code your client does not recognise, fall back on
        the HTTP status.
      </P>

      <H2 id="retrying">Retrying safely</H2>
      <P>
        Retry a 429, a 500, a 503 or a network timeout, with exponential backoff and the same{" "}
        <A href="/docs/conventions#idempotency">idempotency key</A>. Do not retry any other 4xx
        without changing the request: it will fail the same way.
      </P>
    </DocPage>
  );
}
