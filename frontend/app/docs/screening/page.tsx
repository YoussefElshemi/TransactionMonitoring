import type { Metadata } from "next";

import { A, Code, CodeBlock, DocPage, DocTable, H2, List, P } from "@/components/docs/Prose";
import { DOCS_API_BASE } from "@/lib/docs/site";

export const metadata: Metadata = { title: "Screening a transaction" };

const REQUEST = `curl -X POST ${DOCS_API_BASE}/rule-sets/evaluate \\
  -H "Authorization: Bearer $ACCESS_TOKEN" \\
  -H "Content-Type: application/json" \\
  -H "X-Idempotency-Key: 7c0f0d0e-2f0b-4d0a-9d55-3b1f3c1f8a11" \\
  -H "X-Transaction-Reference: TXN-2026-000482" \\
  -d '{
    "amount": 12500.00,
    "currency": "GBP",
    "direction": "inbound",
    "remitter": { "name": "Acme Ltd", "account": "GB29NWBK60161331926819" },
    "beneficiary": { "name": "J Smith", "account": "GB94BARC10201530093459" }
  }'`;

const RESPONSE = `{
  "data": {
    "result": {
      "outcome": { "action": "Review", "severity": "High", "riskScore": 120 },
      "transactionId": "0b9f6c1e-6a52-4a0e-9d2e-1f6a3a1c7e10",
      "transactionReference": "TXN-2026-000482",
      "results": [
        {
          "ruleId": "5a1e0c3b-2f7d-4f55-8a53-0d6a2d0f4b21",
          "friendlyName": "Large inbound",
          "matched": true,
          "policy": { "action": "Review", "severity": "High", "riskWeight": 120 }
        }
      ],
      "decision": {
        "decisionId": "c2d4a9f0-11b7-4c3e-bb0a-7f2c7d1e9a44",
        "status": "Held",
        "heldAt": "2026-10-14T09:12:44+00:00",
        "expiresAt": "2026-10-15T09:12:44+00:00",
        "holdExpiryVerdict": "Block"
      },
      "case": { "caseId": "91aa3f5c-8d02-4e6b-a1c9-5b7e0f2d6c38", "status": "Pending", "severity": "High" }
    }
  }
}`;

export default function DocsScreeningPage() {
  return (
    <DocPage
      href="/docs/screening"
      title="Screening a transaction"
      lead="One call runs a transaction through your rules and returns an outcome to act on. The body is your transaction as a JSON object, in whatever shape your systems already use."
    >
      <CodeBlock label="Shell" code={REQUEST} />

      <H2 id="endpoints">The three evaluate endpoints</H2>
      <DocTable
        head={["Endpoint", "Scope", "Evaluates"]}
        rows={[
          [<Code key="e">POST /rule-sets/evaluate</Code>, <Code key="s">rulesets:evaluate</Code>, "Every enabled rule set"],
          [
            <Code key="e">POST /rule-sets/{"{RuleSetId}"}/evaluate</Code>,
            <Code key="s">rulesets:evaluate</Code>,
            "One rule set",
          ],
          [<Code key="e">POST /rules/{"{RuleId}"}/evaluate</Code>, <Code key="s">rules:evaluate</Code>, "One rule"],
        ]}
      />

      <H2 id="headers">Headers</H2>
      <DocTable
        head={["Header", "Notes"]}
        rows={[
          [
            <Code key="h">X-Transaction-Reference</Code>,
            <>
              Your own identifier for the transaction. Sending the same reference again with the same
              payload evaluates the same transaction again; sending it with a different payload
              returns <Code>400 transaction_payload_mismatch</Code>.
            </>,
          ],
          [
            <Code key="h">X-Create-Case</Code>,
            <>
              <Code>true</Code> asks for a case to be opened for the transaction. Defaults to{" "}
              <Code>false</Code>.
            </>,
          ],
          [
            <Code key="h">X-Idempotency-Key</Code>,
            <>
              A new UUID per transaction. Strongly recommended: see{" "}
              <A href="/docs/conventions#idempotency">idempotency</A>.
            </>,
          ],
        ]}
      />
      <P>
        If your organization has defined a transaction schema in the console, the payload is
        validated against it and a mismatch returns <Code>400 payload_schema_violation</Code>.
      </P>

      <H2 id="response">Reading the response</H2>
      <CodeBlock label="200 OK" code={RESPONSE} />
      <P>
        Act on <Code>data.result.outcome.action</Code>.
      </P>
      <DocTable
        head={["action", "Meaning", "What your system should do"]}
        rows={[
          [<Code key="a">Allow</Code>, "No rule fired, or only allowing rules did", "Proceed"],
          [<Code key="a">Flag</Code>, "A rule fired for the record", "Proceed. An alert is raised for analysts"],
          [
            <Code key="a">Review</Code>,
            "A rule asked for review, or the summed risk score reached the rule set's review threshold",
            "Hold the transaction and wait for the decision",
          ],
          [<Code key="a">Block</Code>, "A rule blocked it", "Reject the transaction"],
        ]}
      />
      <P>
        The strictest action among the rules that fired wins. <Code>severity</Code> is the highest
        among them — <Code>Info</Code>, <Code>Low</Code>, <Code>Medium</Code>, <Code>High</Code> or{" "}
        <Code>Critical</Code> — and <Code>riskScore</Code> is the sum of their weights.{" "}
        <Code>results</Code> lists every rule that ran and whether it matched.
      </P>

      <H2 id="holds">Holds and decisions</H2>
      <P>
        A held transaction comes back with a <Code>decision</Code> whose status is <Code>Held</Code>{" "}
        and an <Code>expiresAt</Code>. It ends in one of two ways:
      </P>
      <List>
        <li>An analyst releases or blocks it in the console.</li>
        <li>
          The hold window passes and <Code>holdExpiryVerdict</Code> is applied. The defaults are 24
          hours and <Code>Block</Code>, and your organization can change both in the console.
        </li>
      </List>
      <P>
        Decision statuses are <Code>Pending</Code>, <Code>Held</Code>, <Code>Released</Code>,{" "}
        <Code>Blocked</Code> and <Code>Expired</Code>. The outcome reaches your system as a{" "}
        <Code>decision.resolved</Code> <A href="/docs/webhooks">webhook</A>: an API client cannot
        read a decision back, so keep the <Code>decisionId</Code> and your own transaction reference
        from the evaluate response to match the event against.
      </P>
    </DocPage>
  );
}
