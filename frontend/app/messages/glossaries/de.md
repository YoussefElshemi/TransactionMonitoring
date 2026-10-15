# Glossar EN -> DE (Transaction Monitor, de)

Register: Sie-Form, Fachsprache Compliance/Geldwäscheprävention. Buttons im Infinitiv.
Keine Marketingsprache. Kurz halten (Tabellenköpfe, Badges).

## Kerndomäne
| EN | DE | Anmerkung |
|---|---|---|
| transaction (auch "payment" im Englischen) | Transaktion | nie "Zahlung" |
| party | Partei (die Parteien) | nie "Entität"; Rollen liegen an der Verknüpfung |
| party role: sender / beneficiary | Auftraggeber / Begünstigter | |
| alert | Alarm (die Alarme) | |
| alert policy | Alarmrichtlinie | |
| case | Fall (die Fälle) | |
| note | Notiz | |
| rule | Regel | |
| rule set | Regelsatz (Regelsätze) | |
| rule version / deployment | Regelversion / Bereitstellung | deploy = bereitstellen |
| queue | Warteschlange | Komposita mit Bindestrich: Warteschlangen-Dashboard |
| approval | Genehmigung | approve/reject = Genehmigen/Ablehnen |
| approval request | Genehmigungsanfrage | |
| escalation / escalate | Eskalation / eskalieren | |
| hold | Sperre | to hold = sperren; hold policy = Sperrrichtlinie |
| suspicious activity report | Verdachtsmeldung | |
| filing (the record) | Meldung | to file = abgeben; filed = abgegeben |
| filing deadline | Meldefrist | |
| financial intelligence unit | Financial Intelligence Unit / FIU | |
| back-test | Backtest | |
| evaluate / evaluation | auswerten / Auswertung | |
| rule pass | Regeldurchlauf | |
| simulation | Simulation | |
| triage / untriaged | Sichtung / ungesichtet | |
| reason code | Grundcode | |
| verdict | Bewertung | |
| disposition | Abschlussart | |
| resolve / reopen / move (case) | Abschließen / Wieder öffnen / Verschieben | |
| concluded | Abgeschlossen | |
| deadline / due / breached | Frist / Fällig / Überschritten | breach rate = Überschreitungsquote |
| severity | Schweregrad | Info, Niedrig, Mittel, Hoch, Kritisch |
| status / state | Status / Zustand | |
| desk | Team | |
| matcher | Kriterium | |
| payload | Payload | in DE-API-Praxis üblich |
| velocity profile | Velocity-Profil | |
| transaction schema | Transaktionsschema | |
| party mapping | Parteizuordnung | |
| backfill | Nachbefüllung | |
| attachment / evidence | Anhang / Nachweis | |
| audit trail | Protokoll | |

## Organisation, Zugriff
| EN | DE |
|---|---|
| organization / sub-organization | Organisation / Unterorganisation |
| installation | Installation |
| business unit | Geschäftsbereich |
| user / person | Benutzer / Person |
| role | Rolle |
| grant / granted / revoke | Zuweisung / Zugewiesen / Entziehen |
| reach (of a grant) | Geltungsbereich; "Reaches" -> "Gilt für" |
| permission | Berechtigung |
| invitation | Einladung |
| invite (Verb) | einladen |
| resend | erneut senden |
| revoke / withdraw (eine Einladung) | zurückziehen |
| accept (eine Einladung) | annehmen |
| offboard | aus dem Team nehmen |
| successor | Nachfolger |
| queue pool | Warteschlangen-Pool |
| seat (eine angebotene Rolle) | Platz |
| assign / unassign / assigned to | Zuweisen / Zuweisung aufheben / Zugewiesen an |
| admin (Bereich / Person) | Administration / Administrator |
| profile / preferences | Profil / Einstellungen |
| notification | Benachrichtigung |

## Technik, API
| EN | DE |
|---|---|
| webhook / delivery / replay | Webhook / Zustellung / erneut senden |
| API client / API key / client secret | API-Client / API-Schlüssel / Client-Secret |
| rate limit | Ratenlimit |
| endpoint / request / response | Endpunkt / Anfrage / Antwort |
| Sandbox / Test / Live | Sandbox / Test / Live (Umgebungsnamen bleiben) |
| dashboard | Dashboard |
| incident | Störung |
| maintenance window | Wartungsfenster |
| availability / uptime | Verfügbarkeit |
| component | Komponente |

## Abrechnung
| EN | DE |
|---|---|
| plan | Tarif |
| plan version / reprice | Tarifversion / Neu bepreisen |
| subscription | Abonnement |
| price band / tier | Preisstaffel |
| rate per request | Preis pro Anfrage |
| invoice / credit note | Rechnung / Gutschrift |
| billing entity | Abrechnungseinheit |
| usage | Nutzung |
| credit(s) | Credit(s) |
| allowance | Kontingent |
| exchange rate | Wechselkurs |
| tax registration | Steuernummer |

## Schaltflächen (Infinitiv)
Speichern, Abbrechen, Zurücksetzen, Hinzufügen, Entfernen, Löschen, Bearbeiten,
Schließen, Öffnen, Anwenden, Erneut versuchen, Anzeigen, Kopieren, Suchen,
Anmelden, Abmelden, Zurück, Weiter.

## Nicht übersetzen
Produktname "Transaction Monitor"; Codes (RE-01234, MULE); Pfadbeispiele
(payment.amount, accountId, amount, name, country); Regex; HTTP-Verben und -Status;
Header-Namen; Zahlenformate (0.00).

## Nachträge
- metric -> Kennzahl (auch organization.metrics)
- "Verdict on the money" -> "Entscheidung über das Geld" (Ausnahme zu verdict=Bewertung: liest sich im Genehmigungsdialog natürlicher)
- service credit / credit note -> Gutschrift; Sandbox-Credits bleiben "Credits"
- queue-Reihenfolgespalten ("Tried"/"Runs") -> "Reihenfolge"
- desk -> Team; "review queue" -> Prüfwarteschlange
