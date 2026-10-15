export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface ReferenceParameter {
  name: string;
  in: "path" | "query" | "header" | "cookie";
  type: string;
  required: boolean;
}

export interface ReferenceField {
  name: string;
  type: string;
  required: boolean;
}

export interface ReferenceBody {
  contentType: string;
  freeForm: boolean;
  fields: ReferenceField[];
}

export interface ReferenceEndpoint {
  method: HttpMethod;
  path: string;
  purpose: string;
  paged: boolean;
  idempotent: boolean;
  success: number;
  parameters: ReferenceParameter[];
  body: ReferenceBody | null;
}

export interface ReferenceGroup {
  id: string;
  title: string;
  endpoints: ReferenceEndpoint[];
}

export const endpointAnchor = (endpoint: Pick<ReferenceEndpoint, "method" | "path">) =>
  `${endpoint.method}-${endpoint.path}`
    .toLowerCase()
    .replace(/[{}]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
