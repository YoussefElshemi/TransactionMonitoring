"use client";

import { useRef, useState, type DragEvent } from "react";
import { toast } from "sonner";
import { Paperclip, Upload } from "lucide-react";

import { AttachmentResource } from "@/types";
import { API_BASE, reportApiError } from "@/lib/api";
import {
  ALLOWED_EXTENSIONS,
  FILE_ACCEPT,
  MAX_ATTACHMENT_BYTES,
  UPLOAD_FIELD_LABELS,
  formatBytes,
  rejectionReason,
} from "@/lib/attachments";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

type Phase = "idle" | "uploading" | "scanning";

/**
 * Send one file, with progress.
 *
 * `XMLHttpRequest` rather than `fetch` for one reason: `fetch` cannot report
 * upload progress, and a 25 MiB statement over a desk connection is exactly the
 * upload somebody needs to see moving. The response is re-wrapped as a `Response`
 * so the shared error path — validation envelopes, the human-readable `detail`,
 * the correlation id in the toast — works unchanged. @see reportApiError
 */
const postAttachment = (
  caseId: string,
  file: File,
  onProgress: (fraction: number) => void
): Promise<Response> =>
  new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    const body = new FormData();
    // The server binds a single form field called `File`; anything else is an
    // empty request as far as the validator is concerned.
    body.append("File", file);

    request.open("POST", `${API_BASE}/cases/${caseId}/attachments`);
    request.withCredentials = true;
    request.responseType = "text";

    request.upload.addEventListener("progress", (event) => {
      if (event.lengthComputable) onProgress(event.loaded / event.total);
    });

    request.addEventListener("load", () => {
      const headers = new Headers();
      for (const line of request.getAllResponseHeaders().trim().split(/\r?\n/)) {
        const separator = line.indexOf(":");
        if (separator > 0) {
          headers.append(
            line.slice(0, separator).trim(),
            line.slice(separator + 1).trim()
          );
        }
      }

      resolve(
        new Response(request.response || null, {
          status: request.status,
          statusText: request.statusText,
          headers,
        })
      );
    });

    request.addEventListener("error", () => reject(new Error("network")));
    request.addEventListener("abort", () => reject(new Error("aborted")));

    request.send(body);
  });

export function AttachmentUploadZone({
  caseId,
  onUploaded,
  onUnauthorized,
  disabled = false,
  disabledReason,
}: {
  caseId: string;
  onUploaded: (resource: AttachmentResource) => void;
  onUnauthorized: () => void;
  disabled?: boolean;
  disabledReason?: string;
}) {
  const t = useTranslation();
  const inputRef = useRef<HTMLInputElement>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const [progress, setProgress] = useState(0);
  const [dragging, setDragging] = useState(false);
  const [current, setCurrent] = useState<string | null>(null);

  const busy = phase !== "idle";

  const upload = async (files: File[]) => {
    if (files.length === 0 || busy || disabled) return;

    const accepted: File[] = [];
    for (const file of files) {
      const reason = rejectionReason(file);
      if (reason) toast.error(reason, { id: `rejected-${file.name}` });
      else accepted.push(file);
    }

    for (const file of accepted) {
      setCurrent(file.name);
      setPhase("uploading");
      setProgress(0);

      try {
        const response = await postAttachment(caseId, file, (fraction) => {
          setProgress(fraction);
          if (fraction >= 1) setPhase("scanning");
        });

        if (response.ok) {
          const resource = (
            (await response.json()) as { data: AttachmentResource }
          ).data;
          onUploaded(resource);
        } else {
          await reportApiError(response, {
            fallback: t("attachments.namedCouldNotUploaded", file.name),
            onUnauthorized,
            toastId: `upload-${file.name}`,
            fieldLabels: UPLOAD_FIELD_LABELS,
          });
        }
      } catch {
        toast.error(t("attachments.namedCouldNotUploaded", file.name), {
          id: `upload-${file.name}`,
          description: t("attachments.requestDidNotReachServer"),
        });
      } finally {
        setPhase("idle");
        setCurrent(null);
        setProgress(0);
      }
    }

    // Cleared so that re-picking the same file fires `change` again — without
    // this, a failed upload cannot be retried from the picker.
    if (inputRef.current) inputRef.current.value = "";
  };

  const handleDrop = (event: DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    setDragging(false);
    if (disabled || busy) return;
    upload(Array.from(event.dataTransfer.files));
  };

  const leftTheZone = (event: DragEvent<HTMLButtonElement>): boolean => {
    const next = event.relatedTarget;
    return !(next instanceof Node) || !event.currentTarget.contains(next);
  };

  return (
    // `relative` is load-bearing, not decoration. The live region at the foot
    // of this component is `sr-only`, which is `position: absolute` — with no
    // positioned ancestor its containing block is the initial one, so it
    // escapes the app's scroll container, lands at its static position in the
    // *document*, and stretches <body> to reach it. On a long case screen that
    // is a thousand pixels of blank the window can scroll into.
    <div className="relative space-y-2">
      <input
        ref={inputRef}
        type="file"
        multiple
        accept={FILE_ACCEPT}
        className="hidden"
        tabIndex={-1}
        onChange={(event) => upload(Array.from(event.target.files ?? []))}
      />

      <button
        type="button"
        disabled={disabled || busy}
        onClick={() => inputRef.current?.click()}
        onDragOver={(event) => {
          // Prevented on every pass, which is what tells the browser this is a
          // drop target at all. React bails out of the re-render when the flag
          // is already set, so the repeat calls cost nothing.
          event.preventDefault();
          if (!disabled && !busy) setDragging(true);
        }}
        onDragLeave={(event) => {
          if (leftTheZone(event)) setDragging(false);
        }}
        onDrop={handleDrop}
        className={cn(
          "flex w-full flex-col items-center justify-center gap-2 rounded-xl border border-dashed px-4 py-8 text-center transition-colors",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800 focus-visible:ring-offset-2",
          dragging
            ? "border-brand-600 bg-brand-50"
            : "border-slate-300 bg-slate-50/50 hover:border-slate-400 hover:bg-slate-50",
          (disabled || busy) && "cursor-not-allowed opacity-70 hover:border-slate-300"
        )}
      >
        {busy ? (
          <>
            <Paperclip className="size-5 text-slate-400" />
            <span className="font-mono text-sm text-slate-700">
              {phase === "uploading" ? "Uploading" : "Scanning"} {current}
            </span>
            <span className="text-xs text-slate-500">
              {phase === "uploading"
                ? `${Math.round(progress * 100)}%`
                :
                  "The file is being checked for malware. The result comes back with the upload."}
            </span>
          </>
        ) : disabled ? (
          <>
            <Paperclip className="size-5 text-slate-300" />
            <span className="text-sm text-slate-500">
              {disabledReason ?? "Uploading is unavailable"}
            </span>
          </>
        ) : (
          <>
            <Upload className="size-5 text-slate-400" />
            <span className="font-mono text-sm font-medium text-slate-700">
              {t("attachments.dropAFileHereOr")}
            </span>
            <span className="max-w-md text-xs text-slate-500">
              {ALLOWED_EXTENSIONS}.{" "}
              {t("attachments.upToEachEveryFile", formatBytes(MAX_ATTACHMENT_BYTES))}
            </span>
          </>
        )}
      </button>

      {busy && (
        <div
          role="progressbar"
          aria-label={phase === "uploading" ? t("attachments.uploadProgress") : "Scanning"}
          aria-valuenow={phase === "uploading" ? Math.round(progress * 100) : undefined}
          aria-valuemin={0}
          aria-valuemax={100}
          className="h-1 w-full overflow-hidden rounded-full bg-slate-200"
        >
          {phase === "uploading" ? (
            <div
              className="h-full rounded-full bg-brand-600 transition-[width] duration-150"
              style={{ width: `${Math.round(progress * 100)}%` }}
            />
          ) : (
            <div className="h-full w-1/3 animate-indeterminate rounded-full bg-brand-600 motion-reduce:animate-none motion-reduce:w-full motion-reduce:opacity-60" />
          )}
        </div>
      )}

      <span className="sr-only" role="status" aria-live="polite">
        {phase === "uploading"
          ? `Uploading ${current}, ${Math.round(progress * 100)} percent`
          : phase === "scanning"
          ? `Scanning ${current}`
          : ""}
      </span>
    </div>
  );
}
