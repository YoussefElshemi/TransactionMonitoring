import type { Metadata } from "next";

import { A, Code, CodeBlock, DocPage, DocTable, H2, List, Note, P } from "@/components/docs/Prose";
import { DOCS_API_BASE } from "@/lib/docs/site";

export const metadata: Metadata = { title: "Authentication" };

const TOKEN_REQUEST = `curl -X POST ${DOCS_API_BASE}/oauth/token \\
  -u "$CLIENT_ID:$CLIENT_SECRET" \\
  -d grant_type=client_credentials \\
  -d scope=rulesets:evaluate`;

const TOKEN_RESPONSE = `{
  "access_token": "CfDJ8...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "CfDJ8...",
  "scope": "rulesets:evaluate"
}`;

const REFRESH_REQUEST = `curl -X POST ${DOCS_API_BASE}/oauth/token \\
  -d grant_type=refresh_token \\
  -d refresh_token="$REFRESH_TOKEN"`;

export default function DocsAuthenticationPage() {
  return (
    <DocPage
      href="/docs/authentication"
      title="Authentication"
      lead="The API authenticates with an API client: a client id and a secret, exchanged for a short-lived bearer token using the OAuth 2.0 client credentials grant."
    >
      <H2 id="create-a-client">1. Create an API client</H2>
      <P>
        Somebody in your organization with access to the console creates the client under
        Organization, then API clients, and chooses its scopes. The console shows the client secret
        once, when the client is created, so store it then: it cannot be shown again.
      </P>
      <P>
        A client is bound to the mode the console was in when it was created, test or live, and
        pinned to the API version that was current at that moment. Use separate clients for test and
        live.
      </P>

      <H2 id="get-a-token">2. Exchange the credentials for a token</H2>
      <P>Send them as a form or as JSON, in the body or as HTTP Basic.</P>
      <CodeBlock label="Shell" code={TOKEN_REQUEST} />
      <CodeBlock label="200 OK" code={TOKEN_RESPONSE} />
      <DocTable
        head={["Field", "Notes"]}
        rows={[
          [
            <Code key="f">grant_type</Code>,
            <>
              <Code>client_credentials</Code> or <Code>refresh_token</Code>
            </>,
          ],
          [
            <>
              <Code>client_id</Code>, <Code>client_secret</Code>
            </>,
            "Required for client_credentials unless sent as Basic",
          ],
          [
            <Code key="f">scope</Code>,
            "Optional and space-separated. Leave it out to receive every scope granted to the client",
          ],
          [<Code key="f">refresh_token</Code>, "Required for the refresh_token grant"],
        ]}
      />
      <P>
        The access token lasts one hour. Tokens are opaque, so do not parse them. Cache the token and
        ask for a new one shortly before <Code>expires_in</Code> elapses rather than on every call:
        the token endpoint is rate limited like any other.
      </P>

      <H2 id="call-the-api">3. Call the API</H2>
      <CodeBlock label="HTTP" code={`POST /rule-sets/evaluate\nAuthorization: Bearer CfDJ8...`} />

      <H2 id="refreshing">Refreshing a token</H2>
      <P>
        Either exchange the credentials again, or send the refresh token. A refreshed token carries
        the scopes the original was granted that the client still holds.
      </P>
      <CodeBlock label="Shell" code={REFRESH_REQUEST} />

      <H2 id="scopes">Scopes</H2>
      <P>A client can do only what its scopes allow, and there are two.</P>
      <DocTable
        head={["Scope", "Grants"]}
        rows={[
          [
            <Code key="s">rulesets:evaluate</Code>,
            <>
              <Code>POST /rule-sets/evaluate</Code>, <Code>POST /rule-sets/{"{RuleSetId}"}/evaluate</Code>
            </>,
          ],
          [<Code key="s">rules:evaluate</Code>, <Code key="e">POST /rules/{"{RuleId}"}/evaluate</Code>],
        ]}
      />
      <P>
        Scopes are chosen when the client is created and can be changed in the console. A change
        takes effect on the client&apos;s next token.
      </P>

      <H2 id="rotating-secrets">Rotating secrets</H2>
      <P>
        A client can hold several secrets at once, which is what makes rotation possible without
        downtime. All three steps happen in the console, on the client&apos;s page.
      </P>
      <List ordered>
        <li>Issue a new secret, optionally with an expiry. It is shown once.</li>
        <li>Deploy the new secret to your systems.</li>
        <li>Revoke the old one. Tokens and refresh tokens issued from it stop working.</li>
      </List>

      <H2 id="token-errors">Token endpoint errors</H2>
      <P>
        This one endpoint answers in the OAuth shape, <Code>error</Code> and{" "}
        <Code>error_description</Code>, rather than the{" "}
        <A href="/docs/errors">problem format</A> used everywhere else.
      </P>
      <DocTable
        head={["Status", "error", "Meaning"]}
        rows={[
          ["400", <Code key="c">invalid_request</Code>, "Credentials or refresh token missing, or the JSON could not be read"],
          ["400", <Code key="c">unsupported_grant_type</Code>, "The grant type is not one of the two above"],
          ["400", <Code key="c">invalid_scope</Code>, "The scope is unknown, or not granted to this client"],
          ["400", <Code key="c">invalid_grant</Code>, "The refresh token is invalid or has expired"],
          ["401", <Code key="c">invalid_client</Code>, "Wrong id or secret, or the client or secret is disabled, revoked or expired"],
        ]}
      />

      <H2 id="failures">When a request is refused</H2>
      <DocTable
        head={["Status", "Code", "Meaning"]}
        rows={[
          [
            "401",
            <Code key="c">unauthorized</Code>,
            "No token, or it has expired, or its client or secret has been revoked. The body is empty",
          ],
          [
            "403",
            <Code key="c">forbidden</Code>,
            "The token is valid but its scopes do not cover this endpoint",
          ],
        ]}
      />
      <Note>
        A rule or rule set that belongs to another organization, or to the other mode, answers{" "}
        <Code>404 not_found</Code> rather than 403. See{" "}
        <A href="/docs/conventions#modes">live and test modes</A>.
      </Note>
    </DocPage>
  );
}
