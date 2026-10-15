"use client";

import { useEffect, useState } from "react";
import { Plus, RotateCcw, X } from "lucide-react";

import { SchemaField, SchemaFieldType, SchemaViolation } from "@/types";
import { schemaPlaceholder, valueMatchesType } from "@/lib/transactionSchema";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

interface PayloadFormProps {
  fields: SchemaField[];
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  violations?: SchemaViolation[];
}

const describeShape = (field: SchemaField, type: SchemaFieldType): string => {
  const shape =
    type === "Array" ? `Array of ${field.elementType ?? "String"}` : type;

  return field.allowedValues?.length && type === "String"
    ? `${shape} · one of ${field.allowedValues.map((v) => `'${v}'`).join(", ")}`
    : shape;
};

const violationsAt = (
  violations: SchemaViolation[],
  path: string
): string[] =>
  violations.filter((v) => v.field === path).map((v) => v.message);

export function PayloadForm({
  fields,
  value,
  onChange,
  violations = [],
}: PayloadFormProps) {
  const undeclared = Object.keys(value).filter(
    (key) => !fields.some((field) => field.name === key)
  );

  return (
    <div className="space-y-3">
      <FieldList
        fields={fields}
        value={value}
        onChange={onChange}
        violations={violations}
        prefix=""
      />

      {undeclared.length > 0 && (
        <p className="text-xs text-slate-400">
          Also carrying{" "}
          <span className="font-mono">{undeclared.join(", ")}</span>, which the
          schema does not declare. They are sent as they are — edit them in the
          JSON view.
        </p>
      )}
    </div>
  );
}

interface FieldListProps {
  fields: SchemaField[];
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  violations: SchemaViolation[];
  prefix: string;
}

function FieldList({
  fields,
  value,
  onChange,
  violations,
  prefix,
}: FieldListProps) {
  return (
    <div className="space-y-3">
      {fields.map((field) => (
        <FieldRow
          key={field.name}
          field={field}
          type={field.type}
          path={prefix + field.name}
          label={field.name}
          value={value[field.name]}
          onChange={(next) => onChange({ ...value, [field.name]: next })}
          violations={violations}
        />
      ))}
    </div>
  );
}

interface FieldRowProps {
  field: SchemaField;
  type: SchemaFieldType;
  path: string;
  label: string;
  value: unknown;
  onChange: (next: unknown) => void;
  violations: SchemaViolation[];
  onRemove?: () => void;
}

function FieldRow({
  field,
  type,
  path,
  label,
  value,
  onChange,
  violations,
  onRemove,
}: FieldRowProps) {
  const t = useTranslation();
  const messages = violationsAt(violations, path);
  const clearable = !field.isRequired && onRemove === undefined;
  const isNull = value === null || value === undefined;

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-baseline gap-1.5">
          <span className="font-mono text-xs text-slate-700">{label}</span>
          {field.isRequired && (
            <span className="text-xs text-critical-700" title={t("evaluate.required")}>
              *
            </span>
          )}
          <span className="text-[11px] text-slate-400">
            {describeShape(field, type)}
          </span>
        </div>

        {isNull ? (
          <span className="font-mono text-[11px] text-slate-400">null</span>
        ) : (
          (clearable || onRemove) && (
            <Button
              type="button"
              size="sm"
              variant="ghost"
              className="h-6 gap-1 px-1.5 text-[11px] text-slate-400 hover:text-slate-700"
              onClick={() => (onRemove ? onRemove() : onChange(null))}
            >
              <X className="size-3" />
              {onRemove ? "Remove" : "Clear"}
            </Button>
          )
        )}
      </div>

      {!isNull && !valueMatchesType(value, type) ? (
        <TypeMismatch field={field} type={type} value={value} onChange={onChange} />
      ) : (
        <ValueEditor
          field={field}
          type={type}
          path={path}
          value={isNull ? emptyValue(type) : value}
          onChange={onChange}
          violations={violations}
          isNull={isNull}
        />
      )}

      {messages.map((message, index) => (
        <p key={index} className="text-xs text-critical-700">
          {message}
        </p>
      ))}
    </div>
  );
}

const emptyValue = (type: SchemaFieldType): unknown => {
  switch (type) {
    case "String":
    case "DateTime":
      return "";
    case "Number":
      return null;
    case "Boolean":
      return false;
    case "Object":
      return {};
    case "Array":
      return [];
  }
};

function TypeMismatch({
  field,
  type,
  value,
  onChange,
}: {
  field: SchemaField;
  type: SchemaFieldType;
  value: unknown;
  onChange: (next: unknown) => void;
}) {
  const t = useTranslation();
  return (
    <div className="flex items-center gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-1.5">
      <span className="font-mono text-xs text-attention-700">
        {JSON.stringify(value)}
      </span>
      <span className="text-[11px] text-attention-700">
        is not {type === "Array" ? "an array" : `a ${type.toLowerCase()}`}
      </span>
      <Button
        type="button"
        size="sm"
        variant="ghost"
        className="ml-auto h-6 gap-1 px-1.5 text-[11px]"
        onClick={() => onChange(schemaPlaceholder(field, type))}
      >
        <RotateCcw className="size-3" />
        {t("evaluate.replace")}
      </Button>
    </div>
  );
}

interface ValueEditorProps {
  field: SchemaField;
  type: SchemaFieldType;
  path: string;
  value: unknown;
  onChange: (next: unknown) => void;
  violations: SchemaViolation[];
  isNull?: boolean;
}

function ValueEditor({
  field,
  type,
  path,
  value,
  onChange,
  violations,
  isNull,
}: ValueEditorProps) {
  switch (type) {
    case "String":
      return field.allowedValues?.length ? (
        <EnumerationEditor
          allowedValues={field.allowedValues}
          value={value as string}
          onChange={onChange}
        />
      ) : (
        <Input
          className="h-9"
          value={value as string}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    case "Number":
      return <NumberEditor value={value as number | null} onChange={onChange} />;

    case "Boolean":
      return (
        <label className="flex items-center gap-2 py-1">
          <input
            type="checkbox"
            checked={value as boolean}
            onChange={(e) => onChange(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 accent-brand-800"
          />
          <span
            className={
              isNull
                ? "font-mono text-sm text-slate-400"
                : "font-mono text-sm text-slate-700"
            }
          >
            {isNull ? "null" : String(value)}
          </span>
        </label>
      );

    case "DateTime":
      return <TimestampEditor value={value as string} onChange={onChange} />;

    case "Object":
      return (
        <div className="border-l-2 border-slate-200 pl-3">
          <FieldList
            fields={field.fields ?? []}
            value={value as Record<string, unknown>}
            onChange={(next) => onChange(next)}
            violations={violations}
            prefix={`${path}.`}
          />
        </div>
      );

    case "Array":
      return (
        <ArrayEditor
          field={field}
          path={path}
          value={value as unknown[]}
          onChange={onChange}
          violations={violations}
        />
      );
  }
}

function EnumerationEditor({
  allowedValues,
  value,
  onChange,
}: {
  allowedValues: string[];
  value: string;
  onChange: (next: unknown) => void;
}) {
  const t = useTranslation();
  const allowed = value === "" || allowedValues.includes(value);

  return (
    <div className="space-y-1">
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger className="h-9">
          <SelectValue placeholder={t("evaluate.pickAValue")} />
        </SelectTrigger>
        <SelectContent>
          {allowedValues.map((option) => (
            <SelectItem key={option} value={option}>
              {option}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {!allowed && (
        <p className="text-xs text-attention-700">
          {t("evaluate.carries")} <span className="font-mono">{JSON.stringify(value)}</span>,
          which the schema does not allow.
        </p>
      )}
    </div>
  );
}

function NumberEditor({
  value,
  onChange,
}: {
  value: number | null;
  onChange: (next: unknown) => void;
}) {
  const [draft, setDraft] = useState(value === null ? "" : String(value));

  useEffect(() => {
    setDraft((current) => {
      if (value === null) return current === "" ? current : "";
      return Number(current) === value ? current : String(value);
    });
  }, [value]);

  return (
    <Input
      className="h-9"
      inputMode="decimal"
      value={draft}
      onChange={(e) => {
        setDraft(e.target.value);
        const parsed = Number(e.target.value);
        if (e.target.value.trim() !== "" && Number.isFinite(parsed)) {
          onChange(parsed);
        }
      }}
      onBlur={() => setDraft(String(value))}
    />
  );
}

function TimestampEditor({
  value,
  onChange,
}: {
  value: string;
  onChange: (next: unknown) => void;
}) {
  const t = useTranslation();
  const parseable = value === "" || !Number.isNaN(Date.parse(value));

  return (
    <div className="space-y-1">
      <div className="flex gap-1.5">
        <Input
          className="h-9"
          value={value}
          placeholder={t("evaluate.20260817t120000z")}
          onChange={(e) => onChange(e.target.value)}
        />
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="h-9 shrink-0 text-xs"
          onClick={() => onChange(new Date().toISOString())}
        >
          {t("evaluate.now")}
        </Button>
      </div>
      {!parseable && (
        <p className="text-xs text-attention-700">
          {t("evaluate.notAnIso8601Timestamp")}
        </p>
      )}
    </div>
  );
}

function ArrayEditor({
  field,
  path,
  value,
  onChange,
  violations,
}: {
  field: SchemaField;
  path: string;
  value: unknown[];
  onChange: (next: unknown) => void;
  violations: SchemaViolation[];
}) {
  const t = useTranslation();
  const elementType = field.elementType ?? "String";

  return (
    <div className="space-y-2 border-l-2 border-slate-200 pl-3">
      {value.length === 0 && (
        <p className="text-xs text-slate-400">{t("evaluate.emptyNoElementsToCheck")}</p>
      )}

      {value.map((element, index) => (
        <FieldRow
          key={index}
          field={field}
          type={elementType}
          path={`${path}[${index}]`}
          label={`${field.name}[${index}]`}
          value={element}
          onChange={(next) =>
            onChange(value.map((item, i) => (i === index ? next : item)))
          }
          onRemove={() => onChange(value.filter((_, i) => i !== index))}
          violations={violations}
        />
      ))}

      <Button
        type="button"
        size="sm"
        variant="outline"
        className="h-7 gap-1 text-xs"
        onClick={() => onChange([...value, schemaPlaceholder(field, elementType)])}
      >
        <Plus className="size-3" />
        Add {field.name.replace(/s$/, "") || "element"}
      </Button>
    </div>
  );
}
