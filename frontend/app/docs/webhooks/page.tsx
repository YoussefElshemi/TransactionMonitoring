import type { Metadata } from "next";

import { Code, CodeBlock, DocPage, DocTable, H2, List, P } from "@/components/docs/Prose";

export const metadata: Metadata = { title: "Webhooks" };

const ENVELOPE = `{
  "event": "decision.resolved",
  "version": 1,
  "timestamp": "2026-10-14T10:02:11+00:00",
  "payload": { "...": "..." }
}`;

const VERIFY = `import hmac, hashlib

def is_valid(raw_body: bytes, signature_header: str, secret_key: str) -> bool:
    expected = hmac.new(secret_key.encode("utf-8"), raw_body, hashlib.sha256).hexdigest()
    return hmac.compare_digest(expected, signature_header)`;

const EVENTS: [string, string][] = [
  ["decision.resolved", "A held transaction was decided, by an analyst or by the hold expiring"],
  ["alert.raised", "A rule matched and raised an alert"],
  ["alert.suppressed", "A rule matched but the alert was deduplicated or suppressed"],
  ["case.created", "A case was opened, by an evaluation or by hand"],
  ["case.assigned", "A case was routed to a queue or picked up by an analyst"],
  ["case.unassigned", "A case was released and is waiting to be picked up again"],
  ["case.status_changed", "A case moved between statuses without being resolved"],
  ["case.escalated", "A case matched an escalation rule"],
  ["case.breached", "A case passed its due time while still open"],
  ["case.resolved", "A case was closed with a disposition"],
  ["case.reopened", "A resolved case was reopened"],
  ["approval.requested", "Something that needs a second approver is waiting for one"],
  ["approval.approved", "A pending approval was granted"],
  ["approval.rejected", "A pending approval was refused"],
  ["approval.withdrawn", "A pending approval was withdrawn by whoever asked for it"],
  ["suspicious_activity_report.opened", "A filing was opened against a case"],
  ["suspicious_activity_report.filed", "A filing was submitted to the reporting authority"],
  ["suspicious_activity_report.acknowledged", "The authority acknowledged a filing"],
  ["suspicious_activity_report.not_filed", "A filing was closed without being submitted"],
  ["suspicious_activity_report.overdue", "A filing passed its deadline without being submitted"],
  ["test", "Sent only by the test action. Cannot be subscribed to"],
];

export default function DocsWebhooksPage() {
  return (
    <DocPage
      href="/docs/webhooks"
      title="Webhooks"
      lead="Webhooks push events to an HTTPS endpoint you register, signed with a secret only you and the platform hold. They tell you what happened after an evaluate call returned."
    >
      <H2 id="register">Register an endpoint</H2>
      <P>
        Somebody in your organization registers the endpoint in the console, under Organization,
        then Webhooks: its URL, and the events it should receive. The console shows the
        endpoint&apos;s signing secret, which your receiver needs to verify deliveries.
      </P>
      <P>
        An endpoint belongs to the mode it was registered in, so register one for test and one for
        live.
      </P>

      <H2 id="delivery">What you receive</H2>
      <P>
        Each delivery is a <Code>POST</Code> with a JSON envelope. Only the envelope&apos;s shape is
        fixed: <Code>payload</Code> depends on the event.
      </P>
      <CodeBlock label="POST to your endpoint" code={ENVELOPE} />
      <DocTable
        head={["Request header", "Meaning"]}
        rows={[
          [<Code key="h">X-Signature</Code>, "HMAC-SHA256 of the raw request body, keyed with the signing secret, in lowercase hex"],
          [<Code key="h">X-Correlation-ID</Code>, "The delivery's id. The same on every retry of one delivery, so deduplicate on it"],
          [<Code key="h">X-Webhook-Event</Code>, "The event name, so you can route before parsing the body"],
          [<Code key="h">X-Webhook-Version</Code>, "The envelope version this endpoint is pinned to"],
          [<Code key="h">X-Webhook-Replay-Of</Code>, "On a replay only: the id of the original delivery"],
        ]}
      />

      <H2 id="verify">Verify the signature</H2>
      <P>
        Compute the HMAC over the exact bytes received, before parsing any JSON, and compare in
        constant time. Reject anything that fails. Answer with a 2xx quickly and do the work
        afterwards.
      </P>
      <CodeBlock label="Python" code={VERIFY} />

      <H2 id="events">Events</H2>
      <DocTable
        head={["Event", "Sent when"]}
        rows={EVENTS.map(([event, description]) => [<Code key={event}>{event}</Code>, description])}
      />
      <P>
        The console lists the same events when an endpoint is registered.
      </P>

      <H2 id="retries">Delivery and retries</H2>
      <List>
        <li>
          A response outside 2xx, or a timeout, is retried. A delivery can therefore arrive more than
          once and out of order: deduplicate on <Code>X-Correlation-ID</Code>.
        </li>
        <li>
          An endpoint that keeps failing is switched off automatically after 20 consecutive failed
          deliveries. Fix it, then switch it back on in the console.
        </li>
        <li>
          The console keeps a log of every delivery, with the payload sent, what your endpoint
          answered and how long it took. Deliveries can be sent again from there, and a replay
          carries <Code>X-Webhook-Replay-Of</Code>.
        </li>
        <li>
          The console can also send a <Code>test</Code> event to an endpoint, and issue it a new
          signing secret.
        </li>
      </List>
    </DocPage>
  );
}
