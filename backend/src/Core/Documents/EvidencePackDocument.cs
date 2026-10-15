using System.Globalization;
using Core.Models.Cases;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Core.Documents;

public class EvidencePackDocument(EvidencePackSource evidencePackSource, DateTimeOffset composedAt) : IDocument
{
    private const string NotRecorded = "not recorded";
    private const string TimestampFormat = "u";

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);

            page.Footer().AlignCenter().Text(text =>
            {
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column
                .Item().Text($"Evidence pack — case {evidencePackSource.Case.FriendlyName}")
                .FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);

            column.Item().Text(text =>
            {
                text.Span("Composed: ").SemiBold();
                text.Span(composedAt.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            });

            column.Item().PaddingTop(4).Text(
                    "Composed from the case as it stood at the time above. It is not a stored document and is not signed.")
                .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(18);

            column.Item().Element(ComposeCase);
            column.Item().Element(ComposePayment);
            column.Item().Element(ComposeRules);
            column.Item().Element(ComposeDecision);
            column.Item().Element(ComposeNotes);
            column.Item().Element(ComposeAttachments);
            column.Item().Element(ComposeTimeline);
        });
    }

    private void ComposeCase(IContainer container)
    {
        var @case = evidencePackSource.Case;

        container.Column(column =>
        {
            column.Item().Element(Heading("The case"));

            column.Item().Element(c => Field(c, "Status", @case.Status.ToString()));
            column.Item().Element(c => Field(c, "Severity", @case.Severity.ToString()));
            column.Item().Element(c => Field(c, "Opened", @case.CreatedAt.ToString(TimestampFormat)));
            column.Item().Element(c => Field(c, "Due", @case.DueAt.ToString(TimestampFormat)));
            column.Item().Element(c => Field(c, "Queue", @case.Queue is not null ? @case.Queue.Name.ToString() : "unrouted"));
            column.Item().Element(c => Field(c, "Assigned to",
                @case.AssignedToUserId.HasValue ? Describe(@case.AssignedToUserId.Value) : "nobody"));

            if (@case.EscalatedAt.HasValue)
            {
                column.Item().Element(c => Field(c, "Escalated", @case.EscalatedAt.Value.ToString(TimestampFormat)));
            }
        });
    }

    private void ComposePayment(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(Heading("The payment"));

            if (evidencePackSource.Payment is null)
            {
                column.Item().Text("No single payment underlies this case; it was raised from grouped alert activity.");

                return;
            }

            var payment = evidencePackSource.Payment;

            column.Item().Element(c => Field(c, "Payment", payment.FriendlyName.ToString()));
            column.Item().Element(c => Field(c, "Customer reference",
                payment.ExternalReference.HasValue ? payment.ExternalReference.Value.ToString() : NotRecorded));
            column.Item().Element(c => Field(c, "Received", payment.CreatedAt.ToString(TimestampFormat)));

            column.Item().PaddingTop(4).Text("The payment's contents are not reproduced here; they are shown by the reveal control, which records who asked and why.")
                .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeRules(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(Heading("Rules fired"));

            var matched = evidencePackSource.RuleEvaluations.Where(x => x.Matched).ToList();

            if (matched.Count == 0)
            {
                column.Item().Text("No monitoring rule matched; this case was raised by hand.");

                return;
            }

            foreach (var ruleEvaluation in matched)
            {
                var name = ruleEvaluation.Rule is not null
                    ? ruleEvaluation.Rule.FriendlyName.ToString()
                    : ruleEvaluation.RuleId.ToString();

                var version = ruleEvaluation.RuleVersion is not null
                    ? $" (version {ruleEvaluation.RuleVersion.VersionNumber})"
                    : string.Empty;

                column.Item().PaddingTop(4).Text($"{name}{version}").SemiBold();

                foreach (var conditionMatch in (ruleEvaluation.ConditionMatches ?? []).Where(x => x.Matched))
                {
                    column.Item().PaddingLeft(12).Text(Describe(conditionMatch)).FontSize(10);
                }
            }
        });
    }

    private void ComposeDecision(IContainer container)
    {
        var @case = evidencePackSource.Case;

        container.Column(column =>
        {
            column.Item().Element(Heading("Final decision"));

            if (@case.Disposition.HasValue)
            {
                var reasonCode = @case.ReasonCode is not null ? $" ({@case.ReasonCode.Code})" : string.Empty;

                column.Item().Element(c => Field(c, "Case closed as", $"{@case.Disposition.Value}{reasonCode}"));
                column.Item().Element(c => Field(c, "Closed",
                    @case.ResolvedAt.HasValue ? @case.ResolvedAt.Value.ToString(TimestampFormat) : NotRecorded));
            }
            else
            {
                column.Item().Text($"The case is still open with a status of {@case.Status}; no disposition has been recorded.");
            }

            if (evidencePackSource.Decision is not null)
            {
                var decision = evidencePackSource.Decision;

                column.Item().Element(c => Field(c, "The payment was", decision.Status.ToString()));
                column.Item().Element(c => Field(c, "Reason code", decision.ReasonCode.ToString()));
                column.Item().Element(c => Field(c, "Decided by",
                    decision.ResolvedByUserId.HasValue ? Describe(decision.ResolvedByUserId.Value) : "the platform"));
            }
        });
    }

    private void ComposeNotes(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(Heading($"Notes ({evidencePackSource.Notes.Count})"));

            if (evidencePackSource.Notes.Count == 0)
            {
                column.Item().Text("No notes were written on this case.");

                return;
            }

            foreach (var caseNote in evidencePackSource.Notes)
            {
                column.Item().PaddingTop(4).Text(
                        $"{caseNote.CreatedAt.ToString(TimestampFormat)} — {Describe(caseNote.CreatedByUserId)}")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                column.Item().Text(caseNote.Message.ToString());
            }
        });
    }

    private void ComposeAttachments(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(Heading($"Attachments ({evidencePackSource.Attachments.Count})"));

            if (evidencePackSource.Attachments.Count == 0)
            {
                column.Item().Text("No files were attached to this case.");

                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(5);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("File");
                    header.Cell().Element(HeaderCell).Text("Uploaded by");
                    header.Cell().Element(HeaderCell).Text("Scan");
                    header.Cell().Element(HeaderCell).Text("SHA-256");
                });

                foreach (var attachment in evidencePackSource.Attachments)
                {
                    table.Cell().Element(Cell).Text($"{attachment.FileName} ({(int)attachment.ByteCount} bytes)");
                    table.Cell().Element(Cell).Text(Describe(attachment.UploadedByUserId));
                    table.Cell().Element(Cell).Text(attachment.ScanStatus.ToString());
                    table.Cell().Element(Cell).Text(attachment.Checksum.ToString()).FontSize(7);
                }
            });
        });
    }

    private void ComposeTimeline(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(Heading($"Timeline ({evidencePackSource.Timeline.Count})"));

            if (evidencePackSource.Timeline.Count == 0)
            {
                column.Item().Text("Nothing audited has happened to this case.");

                return;
            }

            foreach (var auditLog in evidencePackSource.Timeline)
            {
                column.Item().PaddingTop(6).Text(
                    $"{auditLog.CreatedAt.ToString(TimestampFormat)} — {auditLog.Operation} {auditLog.EntityType} — " +
                    $"{DescribeActor(auditLog.ChangedByUserId, auditLog.ChangedByApiClientId)}").FontSize(9);

                foreach (var change in auditLog.Changes)
                {
                    column.Item().PaddingLeft(12).Text(
                            $"{change.Field}: {change.OldValue?.ToJsonString() ?? "—"} → {change.NewValue?.ToJsonString() ?? "—"}")
                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                }

                column.Item().PaddingLeft(12).Text($"entry {auditLog.Sequence} · {auditLog.Hash}")
                    .FontSize(6).FontColor(Colors.Grey.Medium);
            }
        });
    }

    private string DescribeActor(ApplicationUserId? userId, ApiClientId? apiClientId)
    {
        if (userId.HasValue)
        {
            return Describe(userId.Value);
        }

        return apiClientId.HasValue ? $"api client {apiClientId.Value}" : "the platform";
    }

    private string Describe(ApplicationUserId userId)
    {
        var actor = evidencePackSource.Actors.Find(x => x.UserId == userId);

        if (actor is null)
        {
            return userId.ToString();
        }

        return actor.Email.HasValue ? actor.Email.Value.ToString()
            : actor.UserName.HasValue ? actor.UserName.Value.ToString()
            : userId.ToString();
    }

    private static string Describe(ConditionMatch conditionMatch)
    {
        var condition = conditionMatch.Condition.HasValue ? conditionMatch.Condition.Value.ToString() : "matched";
        var expected = conditionMatch.Expected?.ToJsonString() ?? NotRecorded;
        var actual = conditionMatch.Actual?.ToJsonString() ?? NotRecorded;

        return $"{conditionMatch.Field} {condition} {expected}, observed {actual}";
    }

    private static Action<IContainer> Heading(string text)
    {
        return container => container
            .BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingBottom(3)
            .Text(text).FontSize(13).SemiBold();
    }

    private static void Field(IContainer container, string label, string value)
    {
        container.Text(text =>
        {
            text.Span($"{label}: ").SemiBold();
            text.Span(value);
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(3).DefaultTextStyle(x => x.SemiBold());
    }

    private static IContainer Cell(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);
    }
}
