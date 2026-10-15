import { readFileSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const source = process.argv[2];

if (!source) {
  console.error("Usage: node scripts/generate-api-reference.mjs <url-or-path-to-swagger.json>");
  process.exit(1);
}

const load = async () => {
  if (!/^https?:/.test(source)) return JSON.parse(readFileSync(source, "utf8"));

  // The development certificate is self-signed.
  if (new URL(source).hostname === "localhost") process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";

  const response = await fetch(source);
  if (!response.ok) throw new Error(`${source} answered ${response.status}`);
  return response.json();
};

const spec = await load();
const schemas = spec.components?.schemas ?? {};

const METHODS = ["get", "post", "put", "patch", "delete"];

const operations = Object.entries(spec.paths).flatMap(([path, item]) =>
  METHODS.filter((method) => item[method]).map((method) => ({
    method,
    path,
    tag: item[method].tags?.[0] ?? "",
    operation: item[method],
  }))
);

const at = (...keys) => (o) => keys.includes(`${o.method.toUpperCase()} ${o.path}`);

const GROUPS = [
  ["authentication", "Authentication", at("POST /oauth/token")],
  ["evaluation", "Evaluation", (o) => /\/evaluate$/.test(o.path)],
  [
    "platform",
    "Status and metadata",
    at(
      "GET /status",
      "GET /status/heartbeat",
      "GET /status/incidents",
      "POST /status/subscriptions",
      "GET /api-versions",
      "GET /errors"
    ),
  ],
];

const PURPOSES = {
  "POST /oauth/token": "Exchange client credentials or a refresh token for an access token",
  "POST /rules/{RuleId}/evaluate": "Evaluate a transaction against one rule",
  "POST /rule-sets/evaluate": "Evaluate a transaction against every enabled rule set",
  "POST /rule-sets/{RuleSetId}/evaluate": "Evaluate a transaction against one rule set",
  "GET /api-versions": "Served versions and the change policy",
  "GET /errors": "The error code catalogue",
  "GET /status": "Current status, components, incidents and maintenance",
  "GET /status/heartbeat": "Whether status probes are fresh",
  "GET /status/incidents": "Incident history",
  "POST /status/subscriptions": "Subscribe an email address to status updates",
};

const COMMON_HEADERS = ["X-Mode", "X-Api-Version", "X-Idempotency-Key"];

const typeOf = (schema) => {
  if (!schema) return "any";
  if (schema.$ref) {
    const name = schema.$ref.split("/").pop();
    const target = schemas[name];
    if (target?.enum) return target.enum.join(" | ");
    return "object";
  }
  if (schema.enum) return schema.enum.join(" | ");
  if (schema.type === "array") return `${typeOf(schema.items)}[]`;
  if (schema.format === "uuid") return "uuid";
  if (schema.format === "date-time") return "date-time";
  if (schema.format === "date") return "date";
  if (schema.format === "binary") return "file";
  return schema.type ?? "object";
};

const resolve = (schema) => (schema?.$ref ? schemas[schema.$ref.split("/").pop()] : schema);

const parametersOf = (operation) =>
  (operation.parameters ?? [])
    .filter((parameter) => !COMMON_HEADERS.includes(parameter.name))
    .map((parameter) => ({
      name: parameter.name,
      in: parameter.in,
      type: typeOf(parameter.schema),
      required: parameter.required === true,
    }));

const bodyOf = (operation) => {
  const content = operation.requestBody?.content;
  if (!content) return null;

  const contentType =
    ["application/json", "multipart/form-data", "application/x-www-form-urlencoded"].find((type) => content[type]) ??
    Object.keys(content)[0];
  const schema = resolve(content[contentType].schema);
  const required = schema?.required ?? [];

  return {
    contentType,
    freeForm: !schema?.properties,
    fields: Object.entries(schema?.properties ?? {})
      .filter(([, property]) => !property.readOnly)
      .map(([name, property]) => ({
        name,
        type: typeOf(property),
        required: required.includes(name),
      })),
  };
};

const successOf = (operation) =>
  Object.keys(operation.responses ?? {})
    .filter((status) => status.startsWith("2"))
    .map(Number)[0] ?? 200;

const claimed = new Set();

const groups = GROUPS.map(([id, title, matches]) => {
  const members = operations.filter((o) => !claimed.has(o) && matches(o));
  members.forEach((o) => claimed.add(o));

  return {
    id,
    title,
    endpoints: members.map((o) => ({
      method: o.method.toUpperCase(),
      path: o.path,
      purpose: PURPOSES[`${o.method.toUpperCase()} ${o.path}`],
      paged: (o.operation.parameters ?? []).some((parameter) => parameter.name === "PageNumber"),
      idempotent: (o.operation.parameters ?? []).some((parameter) => parameter.name === "X-Idempotency-Key"),
      success: successOf(o.operation),
      parameters: parametersOf(o.operation),
      body: bodyOf(o.operation),
    })),
  };
});

const stale = Object.keys(PURPOSES).filter(
  (key) => !operations.some((o) => `${o.method.toUpperCase()} ${o.path}` === key)
);

if (stale.length > 0) {
  console.error(`PURPOSES names ${stale.length} operation(s) the spec no longer has:\n  ${stale.join("\n  ")}`);
  process.exit(1);
}

const output = `// Generated by scripts/generate-api-reference.mjs from the API's OpenAPI document.
// Do not edit by hand: change the script, or the API, and run it again.

import type { ReferenceGroup } from "@/lib/docs/reference";

export const REFERENCE_SPEC_VERSION = ${JSON.stringify(spec.info?.version ?? "")};

export const REFERENCE_GROUPS: ReferenceGroup[] = ${JSON.stringify(groups, null, 2)};
`;

const target = fileURLToPath(new URL("../app/lib/docs/reference.generated.ts", import.meta.url));

writeFileSync(target, output);

console.log(
  `Documented ${claimed.size} of ${operations.length} operations in ${groups.length} groups. ` +
    `${operations.length - claimed.size} left out.`
);
