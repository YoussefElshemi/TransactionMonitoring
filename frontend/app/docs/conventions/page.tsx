import type { Metadata } from "next";

import { Code, DocPage, DocTable, H2, List, P } from "@/components/docs/Prose";
import { DOCS_API_VERSION } from "@/lib/docs/site";

export const metadata: Metadata = { title: "Request conventions" };

export default function DocsConventionsPage() {
  return (
    <DocPage
      href="/docs/conventions"
      title="Request conventions"
      lead="The same few headers and the same limits apply to every endpoint. An integration needs only the Authorization header to begin with."
    >
      <H2 id="headers">Headers</H2>
      <DocTable
        head={["Request header", "Required", "Purpose"]}
        rows={[
          [<Code key="h">Authorization</Code>, "Yes, outside the public endpoints", "Bearer token"],
          [<Code key="h">X-Api-Version</Code>, "No", "Serve this one request under another contract version"],
          [<Code key="h">X-Idempotency-Key</Code>, "No, recommended on writes", "A UUID that makes a retry safe"],
        ]}
      />
      <DocTable
        head={["Response header", "Meaning"]}
        rows={[
          [<Code key="h">X-Correlation-ID</Code>, "Identifies the request in the platform's logs. Quote it when reporting a problem"],
          [<Code key="h">X-Mode</Code>, "The mode the request was served under: live or test, as fixed by the client"],
          [<Code key="h">X-Api-Version</Code>, "The version the request was actually served under"],
          [<Code key="h">X-Idempotent-Replay</Code>, "true when the response was replayed from an earlier request"],
          [
            <>
              <Code>RateLimit-Limit</Code>, <Code>RateLimit-Remaining</Code>, <Code>RateLimit-Reset</Code>
            </>,
            "Your budget, what is left of it, and the seconds until it refills",
          ],
          [<Code key="h">Retry-After</Code>, "Sent with a 429: the seconds to wait"],
          [
            <>
              <Code>Deprecation</Code>, <Code>Sunset</Code>
            </>,
            "Present only when the version served has been deprecated",
          ],
        ]}
      />

      <H2 id="modes">Live and test modes</H2>
      <P>
        <Code>live</Code> and <Code>test</Code> are separate partitions that share no data. A rule, a
        case or a transaction created in one is invisible from the other.
      </P>
      <List>
        <li>
          Test traffic is never billed, never files a report and never sends an email or a chat
          message. It spends a monthly sandbox allowance, and once that is gone calls return{" "}
          <Code>402 sandbox_credits_exhausted</Code>.
        </li>
        <li>
          The mode is a property of the API client, fixed when it is created. A test client is always
          served the sandbox and a live client always live; there is nothing to send to choose.
        </li>
        <li>
          Every response says which it was, in the <Code>X-Mode</Code> response header.
        </li>
      </List>

      <H2 id="versioning">Versioning</H2>
      <P>
        Versions are dates. The current one is <Code>{DOCS_API_VERSION}</Code>.
      </P>
      <List>
        <li>
          An API client is pinned to the version that was current when it was created, and stays
          there until you move it.
        </li>
        <li>
          Send <Code>X-Api-Version</Code> to try a newer version on a single request before moving
          the pin. A value that is not served returns <Code>400 unsupported_api_version</Code>.
        </li>
        <li>
          A deprecated version goes on being served for at least 12 months. The{" "}
          <Code>Deprecation</Code> and <Code>Sunset</Code> response headers say when.
        </li>
        <li>
          <Code>GET /api-versions</Code> lists the served versions and the full policy on what counts
          as a breaking change.
        </li>
      </List>
      <P>
        Additions are not breaking. Write your client to ignore response fields it does not
        recognise, to treat an unknown enumeration value as unknown rather than failing, and to
        branch on an error&apos;s <Code>code</Code> and never on its wording.
      </P>

      <H2 id="idempotency">Idempotency</H2>
      <P>
        Send <Code>X-Idempotency-Key</Code> with a new UUID on every evaluate call. All three
        evaluate endpoints accept it.
      </P>
      <List>
        <li>
          Repeating a request with the same key returns the original response, with{" "}
          <Code>X-Idempotent-Replay: true</Code>. Nothing is evaluated or created twice.
        </li>
        <li>Keys are scoped to your organization and kept for 24 hours.</li>
        <li>
          A key belongs to the first endpoint that used it. Using it on another returns{" "}
          <Code>409 idempotency_key_reused</Code>.
        </li>
        <li>
          If the first request is still running, the retry returns{" "}
          <Code>409 idempotency_request_in_progress</Code>. Wait, then retry.
        </li>
        <li>
          A key that is not a UUID returns <Code>400 invalid_idempotency_key</Code>.
        </li>
      </List>

      <H2 id="rate-limits">Rate limits</H2>
      <P>
        Limits are budgets that refill every 60 seconds. Evaluate calls are counted twice: against
        the API client making them, and against your organization as a whole.
      </P>
      <DocTable
        head={["Calls", "Per API client", "Per organization", "Test mode"]}
        rows={[["Evaluate endpoints", "1,200 / min", "6,000 / min", "120 / min"]]}
      />
      <P>
        Requests without a token, the token exchange included, share 60 a minute per address — one
        more reason to cache the token. These are the defaults, and your contract may set others:
        the <Code>RateLimit-*</Code> headers on every response are the authority. A request over
        budget returns <Code>429 rate_limited</Code> with <Code>Retry-After</Code>.
      </P>
    </DocPage>
  );
}
