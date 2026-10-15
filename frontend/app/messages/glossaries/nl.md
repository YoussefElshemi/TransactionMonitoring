# Glossary EN -> NL (Transaction Monitor console)

Register: u-form throughout, infinitive for button verbs, sentence case in headings.

## Core domain
| English | Dutch | Note |
|---|---|---|
| transaction | transactie | never "betaling" |
| payment (in EN source) | transactie | EN slips into "payment"; NL always transactie |
| party | partij | counterparty to a transactie |
| party mapping | partijmapping | |
| alert | alert / alerts | borrowed; working term in NL TM practice |
| case | dossier | "zaak" avoided (legal), "case" avoided |
| rule | regel | |
| rule set | regelset | |
| rule fires | regel slaat aan | fired = aangeslagen |
| queue | wachtrij | |
| desk | team | |
| untriaged | niet getrieerd | |
| unrouted | niet gerouteerd | |
| approval | goedkeuring | approve = goedkeuren, request = goedkeuringsverzoek |
| escalation | escalatie | escalate = escaleren |
| back-test | backtest | borrowed |
| hold | aanhouding | verb aanhouden, held = aangehouden |
| settle (a held transaction) | beslissen | NOT financial settlement: the analyst decides an
  aanhouding, choosing Release or Block inside the form. settled = beslist. Kept distinct from
  afhandelen (resolve a dossier) and blokkeren (the Block action). |
| block (rule action) | blokkeren | distinct from hold |
| review (rule action) | beoordelen | |
| allow (rule action) | toestaan | |
| suspicious activity report | melding ongebruikelijke transactie | short forms: melding |
| filing | melding | file (v) = melden, filed = gemeld |
| financial intelligence unit | financiële inlichtingeneenheid | |
| narrative | toelichting | SAR narrative |
| notification (system) | notificatie | keeps "melding" free for FIU filings |
| disposition | afdoening | |
| verdict | oordeel | |
| reason code | redencode | |
| resolve (case) | afhandelen | resolved = afgehandeld |
| reopen | heropenen | |
| breached (deadline) | overschreden | |
| deadline / due | deadline / uiterlijk | |
| assign / unassign | toewijzen / toewijzing opheffen | |
| claim, pick up | oppakken | |
| note | notitie | |
| evidence | bewijsmateriaal | |
| attachment | bijlage | |
| audit | audit / auditspoor | |
| invitation | uitnodiging | resend = opnieuw versturen |
| invite (ww.) | uitnodigen | |
| revoke / withdraw (uitnodiging) | intrekken | sluit aan op status.withdrawn |
| accept (uitnodiging) | accepteren | |
| offboard | uit het team halen | nooit "verwijderen": het account blijft bestaan |
| successor | opvolger | neemt de openstaande dossiers over |
| queue pool | de wachtrijen | |
| seat (aangeboden rol) | plek | |

## Severity & status
Severity = Ernst. Info / Laag / Middel / Hoog / Kritiek.
Severity floor = Minimale ernst. Status = Status. Open = Open, Closed = Gesloten,
Concluded = Afgerond, Paused = Gepauzeerd, Running = Actief, Pending = In afwachting,
Failed = Mislukt, Delivered = Afgeleverd, Enabled/Disabled = Ingeschakeld/Uitgeschakeld.

## Platform
| English | Dutch |
|---|---|
| deploy / deployment | uitrollen / uitrol |
| rollout | uitrol |
| retire / retired | uitfaseren / uitgefaseerd |
| payload | payload (borrowed) |
| webhook / endpoint | webhook / endpoint |
| signing secret | ondertekeningssleutel |
| rate limit | rate limit (borrowed) |
| override | uitzondering |
| schema | schema |
| field path | veldpad |
| threshold | drempelwaarde |
| metric | metriek |
| velocity profile | velocityprofiel |
| backfill | backfill |
| seed | seed |
| simulation | simulatie |
| credit (sandbox) | credit |
| uptime / latency | uptime / latency |
| incident / maintenance window | incident / onderhoudsvenster |
| post-incident review | evaluatie achteraf |
| API client | API-client |
| role / permission | rol / recht |
| grant | toekenning |

## Billing
| English | Dutch |
|---|---|
| plan | pakket |
| subscription | abonnement |
| invoice | factuur |
| billing entity | facturatie-entiteit |
| commitment | toezegging |
| adjustment | correctie |
| credit note | creditnota |
| platform fee | platformkosten |
| monthly minimum | maandelijks minimum |
| rate per request | tarief per verzoek |
| band | staffel |
| exchange rate | wisselkoers |
| tax registration | fiscaal nummer |
| payment terms | betalingstermijn |

## Kept English (borrowed / product names)
Transaction Monitor, Live, Test, Sandbox, alert, payload, webhook, endpoint, backtest,
rate limit, uptime, latency, seed, backfill, UTC, API.
