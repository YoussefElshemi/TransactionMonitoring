"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import EvaluationConsole from "@/components/EvaluationConsole";
import { Mark } from "@/components/Mark";
import { ThemeToggle } from "@/components/ThemeToggle";
import { useTranslation } from "@/context/LocalizationContext";

const capabilitiesFor = (t: (key: string) => string) => [
  {
    label: t("landingPage.capability.ruleTrees"),
    description: t("landingPage.capability.ruleTrees.description"),
    artifact: (
      <>
        <div className="text-brand-800">and</div>
        <div>
          <span className="text-slate-400">├</span> amount{" "}
          <span className="text-brand-800">&gt;</span> 5000
        </div>
        <div>
          <span className="text-slate-400">└</span>{" "}
          <span className="text-brand-800">or</span>
        </div>
        <div className="pl-4">
          <span className="text-slate-400">├</span> corridor{" "}
          <span className="text-brand-800">in</span> [NG, GH]
        </div>
        <div className="pl-4">
          <span className="text-slate-400">└</span> mcc{" "}
          <span className="text-brand-800">=</span> 6011
        </div>
      </>
    ),
  },
  {
    label: t("landingPage.capability.backTests"),
    description: t("landingPage.capability.backTests.description"),
    artifact: (
      <>
        <div className="text-slate-400">back-test · completed</div>
        <div>2,481 evaluations replayed</div>
        <div className="text-attention-700">34 newly flagged</div>
        <div className="text-settled-700">2 no longer flagged</div>
      </>
    ),
  },
  {
    label: t("landingPage.capability.gradualRollout"),
    description: t("landingPage.capability.gradualRollout.description"),
    artifact: (
      <>
        <div className="text-slate-400">deployment · gradual</div>
        <div className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-slate-200">
          <div className="w-1/4 bg-brand-600" />
        </div>
        <div className="mt-2">25% on v4 · stepping every 30 min</div>
      </>
    ),
  },
  {
    label: t("landingPage.capability.cases"),
    description: t("landingPage.capability.cases.description"),
    artifact: (
      <>
        <div className="text-slate-400">case · #1042</div>
        <div>{t("landingPage.highValueToANew")}</div>
        <div className="mt-2 flex flex-wrap gap-1.5">
          {["Pending", "Assigned", "Approved"].map((status) => (
            <span
              key={status}
              className="rounded border border-slate-200 bg-card px-1.5 py-0.5 text-[10px] text-slate-500"
            >
              {status}
            </span>
          ))}
        </div>
      </>
    ),
  },
];

export default function LandingPage() {
  const t = useTranslation();
  return (
    <div className="min-h-screen bg-card">
      <header className="mx-auto flex max-w-6xl items-center justify-between px-6 py-5">
        <span className="flex items-center gap-2.5">
          <Mark className="size-5 text-brand-800" />
          <span className="font-mono text-sm font-medium tracking-tight text-slate-900">
            {t("landingPage.transactionMonitor")}
          </span>
        </span>
        <nav className="flex items-center gap-2">
          <ThemeToggle />
          <Button variant="ghost" asChild>
            <Link href="/auth/login">{t("common.signIn")}</Link>
          </Button>
          <Button asChild>
            <Link href="/auth/register">{t("landingPage.createAccount")}</Link>
          </Button>
        </nav>
      </header>

      <main>
        <section className="mx-auto max-w-6xl px-6 pb-20 pt-10 lg:pb-28 lg:pt-16">
          <div className="grid items-center gap-12 lg:grid-cols-[1fr_minmax(0,26rem)] lg:gap-16">
            <div>
              <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("landingPage.rulesEngineForTransactions")}
              </p>
              <h1 className="mt-4 font-mono text-[2rem] font-medium leading-[1.15] tracking-tight text-slate-900 sm:text-[2.75rem]">
                {t("landingPage.everyTransactionGetsAVerdict")}
                <span className="block text-brand-800">
                  {t("landingPage.youCanReadTheRule")}
                </span>
              </h1>
              <p className="mt-6 max-w-lg text-base leading-relaxed text-slate-500">
                Send a payment to the API and it comes back checked against your
                rules. Anything that matches becomes a case your analysts can
                work — with the payload and the rule that fired attached to it.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <Button size="lg" asChild>
                  <Link href="/auth/register">{t("landingPage.createAccount")}</Link>
                </Button>
                <Button size="lg" variant="outline" asChild>
                  <Link href="/auth/login">{t("common.signIn")}</Link>
                </Button>
              </div>
            </div>

            <EvaluationConsole />
          </div>
        </section>

        <section
          id="what-you-work-with"
          className="border-t border-slate-200 bg-slate-50"
        >
          <div className="mx-auto max-w-6xl px-6 py-20">
            <h2 className="font-mono text-xl font-medium tracking-tight text-slate-900">
              {t("landingPage.whatYouWorkWith")}
            </h2>
            <p className="mt-2 max-w-lg text-sm text-slate-500">
              Four things, and they fit together in that order: write a rule,
              prove it on history, roll it out carefully, work what it catches.
            </p>

            <div className="mt-10 grid gap-5 sm:grid-cols-2">
              {capabilitiesFor(t).map((capability) => (
                <div
                  key={capability.label}
                  className="flex flex-col rounded-xl border border-slate-200 bg-card p-6"
                >
                  <h3 className="font-mono text-sm font-medium text-slate-900">
                    {capability.label}
                  </h3>
                  <p className="mt-2 flex-1 text-sm leading-relaxed text-slate-500">
                    {capability.description}
                  </p>
                  <div className="mt-5 rounded-lg border border-slate-200 bg-slate-50 p-4 font-mono text-[11px] leading-relaxed text-slate-600">
                    {capability.artifact}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section id="integrate" className="border-t border-slate-200">
          <div className="mx-auto grid max-w-6xl gap-10 px-6 py-20 lg:grid-cols-2 lg:items-center">
            <div>
              <h2 className="font-mono text-xl font-medium tracking-tight text-slate-900">
                {t("landingPage.twoCallsToWireIt")}
              </h2>
              <p className="mt-3 max-w-md text-sm leading-relaxed text-slate-500">
                Create an API client, swap its credentials for an access token,
                then post transactions with that token. Tokens last an hour —
                cache one and fetch a new one when it expires, rather than
                asking per request.
              </p>
              <p className="mt-3 max-w-md text-sm leading-relaxed text-slate-500">
                Set{" "}
                <code className="rounded bg-slate-100 px-1 py-0.5 font-mono text-[13px] text-slate-700">
                  {t("landingPage.xCreateCase")}
                </code>{" "}
                when you want a match to raise a case for review, and point a
                webhook at your own systems to hear about it.
              </p>
            </div>

            <div className="overflow-x-auto rounded-xl border border-console-border bg-console p-5 font-mono text-[11px] leading-relaxed sm:text-xs">
              <div className="min-w-[19rem] text-console-300">
                <div>
                  <span className="text-brand-400">{t("landingPage.post")}</span> /oauth/token
                </div>
                <div className="text-console-500">
                  grant_type=client_credentials
                </div>
                <div className="text-console-500">
                  &amp;client_id=tmc_…&amp;client_secret=tmsec_…
                </div>
                <div className="mt-2 text-settled-500">200 OK</div>
                <div className="text-console-400">
                  {'{ "access_token": "…", "expires_in": 3600 }'}
                </div>

                <div className="mt-5">
                  <span className="text-brand-400">{t("landingPage.post")}</span>{" "}
                  /rule-sets/evaluate
                </div>
                <div className="text-console-500">
                  Authorization: Bearer {"<access_token>"}
                </div>
                <div className="text-console-500">{t("landingPage.xCreateCaseTrue")}</div>
                <div className="mt-2">
                  {"{ "}
                  <span className="text-console-400">&quot;payload&quot;</span>
                  {": { "}
                  <span className="text-console-400">&quot;amount&quot;</span>
                  {": 9400, "}
                  <span className="text-console-400">&quot;currency&quot;</span>
                  {': "GBP" } }'}
                </div>
                <div className="mt-2 text-settled-500">200 OK</div>
                <div className="text-console-400">
                  {'{ "results": [{ "result": true }], "case": {…} }'}
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="border-t border-slate-200 bg-slate-50">
          <div className="mx-auto max-w-6xl px-6 py-20 text-center">
            <h2 className="font-mono text-2xl font-medium tracking-tight text-slate-900">
              {t("landingPage.startWithOneRule")}
            </h2>
            <p className="mx-auto mt-3 max-w-md text-sm leading-relaxed text-slate-500">
              Create an account, and an admin adds you to your organization.
              From there you can write a rule, back-test it, and put it in front
              of traffic.
            </p>
            <div className="mt-8 flex justify-center gap-3">
              <Button size="lg" asChild>
                <Link href="/auth/register">{t("landingPage.createAccount")}</Link>
              </Button>
              <Button size="lg" variant="outline" asChild>
                <Link href="/auth/login">{t("common.signIn")}</Link>
              </Button>
            </div>
          </div>
        </section>
      </main>

      <footer className="border-t border-slate-200">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-8">
          <span className="flex items-center gap-2.5">
            <Mark className="size-4 text-slate-400" />
            <span className="font-mono text-xs text-slate-500">
              {t("landingPage.transactionMonitor")}
            </span>
          </span>
          <div className="flex gap-5 font-mono text-xs text-slate-500">
            <Link href="#what-you-work-with" className="hover:text-slate-900">
              {t("landingPage.whatYouWorkWith")}
            </Link>
            <Link href="#integrate" className="hover:text-slate-900">
              {t("landingPage.integrate")}
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
}
