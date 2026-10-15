using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Sequence",
                schema: "Service",
                table: "AuditLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                schema: "Service",
                table: "AuditLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                schema: "Service",
                table: "AuditLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // The canonical form of an entry and the digest over it, stated once for the three callers
            // that need it: the insert trigger below, the backfill below that, and — mirrored in C# —
            // AuditChainHasher, which is what an auditor recomputes with. The mirror is the deliberate
            // part: a chain closed by the application would only hold while the application was the one
            // writing, and a chain checked by the code that wrote it has been checked by nobody.
            //
            // Every component is rendered the way both sides render it. The timestamp goes in at
            // microsecond resolution because that is what the column keeps; the snapshot goes in as
            // jsonb renders it, which is also what a client reading the column back receives.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION "Service"."ComputeAuditLogHash"(
                    "auditLogId" uuid,
                    "entityTypeId" integer,
                    "entityId" uuid,
                    "organizationId" uuid,
                    "operationId" integer,
                    "snapshot" jsonb,
                    "changedByUserId" uuid,
                    "changedByApiClientId" uuid,
                    "createdAt" timestamptz,
                    "sequence" bigint,
                    "previousHash" text)
                RETURNS text
                LANGUAGE sql
                AS $chain$
                SELECT encode(sha256(convert_to(concat_ws('|',
                    "auditLogId"::text,
                    "entityTypeId"::text,
                    "entityId"::text,
                    coalesce("organizationId"::text, ''),
                    "operationId"::text,
                    "snapshot"::text,
                    coalesce("changedByUserId"::text, ''),
                    coalesce("changedByApiClientId"::text, ''),
                    to_char("createdAt" AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                    "sequence"::text,
                    coalesce("previousHash", '')), 'UTF8')), 'hex')
                $chain$;
                """);

            // Chains what is already there, in the order it was written. A migration cannot make an
            // entry from last year tamper-evident — a chain built today proves only that nothing has
            // changed since today, which is equally true of the first entry on a fresh installation.
            // What it does buy is a trail with no seam in it: leaving the history unchained would mean
            // every export and every verification carrying a run of blank columns, and an auditor
            // reading a blank where a digest should be has to be told which of the two reasons it is.
            //
            // Written as one recursive statement rather than a loop, because a loop needs a function to
            // live in and a function needs calling, and the call would have to be valid both as plain
            // SQL and inside the DO block the idempotent script wraps every statement in.
            migrationBuilder.Sql(
                """
                WITH RECURSIVE "ordered" AS (
                    SELECT a."AuditLogId",
                           a."EntityTypeId",
                           a."EntityId",
                           a."OrganizationId",
                           a."OperationId",
                           a."Snapshot",
                           a."ChangedByUserId",
                           a."ChangedByApiClientId",
                           a."CreatedAt",
                           row_number() OVER (
                               PARTITION BY a."OrganizationId"
                               ORDER BY a."CreatedAt", a."AuditLogId") AS "Sequence"
                    FROM "Service"."AuditLogs" a
                ),
                "chained" AS (
                    SELECT o."AuditLogId",
                           o."OrganizationId",
                           o."Sequence",
                           NULL::text AS "PreviousHash",
                           "Service"."ComputeAuditLogHash"(
                               o."AuditLogId", o."EntityTypeId", o."EntityId", o."OrganizationId",
                               o."OperationId", o."Snapshot", o."ChangedByUserId", o."ChangedByApiClientId",
                               o."CreatedAt", o."Sequence", NULL) AS "Hash"
                    FROM "ordered" o
                    WHERE o."Sequence" = 1
                    UNION ALL
                    SELECT o."AuditLogId",
                           o."OrganizationId",
                           o."Sequence",
                           c."Hash" AS "PreviousHash",
                           "Service"."ComputeAuditLogHash"(
                               o."AuditLogId", o."EntityTypeId", o."EntityId", o."OrganizationId",
                               o."OperationId", o."Snapshot", o."ChangedByUserId", o."ChangedByApiClientId",
                               o."CreatedAt", o."Sequence", c."Hash") AS "Hash"
                    FROM "chained" c
                    JOIN "ordered" o
                      ON o."OrganizationId" IS NOT DISTINCT FROM c."OrganizationId"
                     AND o."Sequence" = c."Sequence" + 1
                )
                UPDATE "Service"."AuditLogs" a
                SET "Sequence" = c."Sequence",
                    "PreviousHash" = c."PreviousHash",
                    "Hash" = c."Hash"
                FROM "chained" c
                WHERE a."AuditLogId" = c."AuditLogId";
                """);

            // The defaults existed to get the columns onto rows that were already there, and they go
            // once those rows are chained. Left in place they would be a silent fallback: drop the
            // trigger and inserts would carry on succeeding with position zero and an empty digest,
            // which is the one failure mode a tamper-evident log must not have.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Service"."AuditLogs" ALTER COLUMN "Sequence" DROP DEFAULT;
                ALTER TABLE "Service"."AuditLogs" ALTER COLUMN "Hash" DROP DEFAULT;
                """);

            migrationBuilder.CreateTable(
                name: "AuditAnchors",
                schema: "Service",
                columns: table => new
                {
                    AuditAnchorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Signature = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditAnchors", x => x.AuditAnchorId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId_Sequence",
                schema: "Service",
                table: "AuditLogs",
                columns: new[] { "OrganizationId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditAnchors_OrganizationId_Sequence",
                schema: "Service",
                table: "AuditAnchors",
                columns: new[] { "OrganizationId", "Sequence" },
                unique: true);

            // Takes the position and closes the link, under a lock held for the rest of the inserting
            // transaction. The lock is per chain rather than per table, so two desks writing at once do
            // not queue behind each other, and it is what makes the position a fact rather than a race:
            // two entries that read the same head would both claim it and one of them would be wrong
            // about what came before it.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION "Service"."ChainAuditLog"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $chain$
                DECLARE
                    "headHash" text;
                    "headSequence" bigint;
                BEGIN
                    PERFORM pg_advisory_xact_lock(4505, hashtext(coalesce(NEW."OrganizationId"::text, '')));

                    IF NEW."OrganizationId" IS NULL THEN
                        SELECT a."Hash", a."Sequence" INTO "headHash", "headSequence"
                        FROM "Service"."AuditLogs" a
                        WHERE a."OrganizationId" IS NULL
                        ORDER BY a."Sequence" DESC
                        LIMIT 1;
                    ELSE
                        SELECT a."Hash", a."Sequence" INTO "headHash", "headSequence"
                        FROM "Service"."AuditLogs" a
                        WHERE a."OrganizationId" = NEW."OrganizationId"
                        ORDER BY a."Sequence" DESC
                        LIMIT 1;
                    END IF;

                    NEW."Sequence" := coalesce("headSequence", 0) + 1;
                    NEW."PreviousHash" := "headHash";
                    NEW."Hash" := "Service"."ComputeAuditLogHash"(
                        NEW."AuditLogId", NEW."EntityTypeId", NEW."EntityId", NEW."OrganizationId",
                        NEW."OperationId", NEW."Snapshot", NEW."ChangedByUserId", NEW."ChangedByApiClientId",
                        NEW."CreatedAt", NEW."Sequence", NEW."PreviousHash");

                    RETURN NEW;
                END;
                $chain$;
                """);

            // The second bullet. An audit trail whose value in a regulatory review is that it could not
            // have been edited afterwards cannot rest on the application declining to edit it: the
            // question an auditor is really asking is what somebody with the database password could
            // have done, and the honest answer has to be enforced where they would be standing.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION "Service"."RejectAuditTrailMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $chain$
                BEGIN
                    RAISE EXCEPTION 'The audit trail is append only: % on "%"."%" is not permitted.',
                        TG_OP, TG_TABLE_SCHEMA, TG_TABLE_NAME
                        USING ERRCODE = 'restrict_violation';
                END;
                $chain$;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "AuditLogs_Chain"
                    BEFORE INSERT ON "Service"."AuditLogs"
                    FOR EACH ROW EXECUTE FUNCTION "Service"."ChainAuditLog"();
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "AuditLogs_AppendOnly"
                    BEFORE UPDATE OR DELETE ON "Service"."AuditLogs"
                    FOR EACH ROW EXECUTE FUNCTION "Service"."RejectAuditTrailMutation"();
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "AuditAnchors_AppendOnly"
                    BEFORE UPDATE OR DELETE ON "Service"."AuditAnchors"
                    FOR EACH ROW EXECUTE FUNCTION "Service"."RejectAuditTrailMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The append-only triggers come off first, or the column drops below cannot run.
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "AuditAnchors_AppendOnly" ON "Service"."AuditAnchors";
                DROP TRIGGER IF EXISTS "AuditLogs_AppendOnly" ON "Service"."AuditLogs";
                DROP TRIGGER IF EXISTS "AuditLogs_Chain" ON "Service"."AuditLogs";
                DROP FUNCTION IF EXISTS "Service"."RejectAuditTrailMutation"();
                DROP FUNCTION IF EXISTS "Service"."ChainAuditLog"();
                DROP FUNCTION IF EXISTS "Service"."ComputeAuditLogHash"(
                    uuid, integer, uuid, uuid, integer, jsonb, uuid, uuid, timestamptz, bigint, text);
                """);

            migrationBuilder.DropTable(
                name: "AuditAnchors",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OrganizationId_Sequence",
                schema: "Service",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Hash",
                schema: "Service",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                schema: "Service",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Sequence",
                schema: "Service",
                table: "AuditLogs");
        }
    }
}
