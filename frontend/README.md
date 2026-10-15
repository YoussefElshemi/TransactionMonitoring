This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

### Running over https

Adding a payment method sends the customer to the payment provider and needs an
absolute **https** address to return them to — they arrive back having just
typed a card number, so the API refuses anything else. `next dev` serves the
console over http, so that one flow cannot be exercised on the default setup.

To run the whole thing over https:

```bash
# the API, on https://localhost:7159 (it also keeps http://localhost:5253)
dotnet run --project ../backend/src/Presentation --launch-profile https

# this console, on https://localhost:3000 with a self-signed certificate
npm run dev:https
```

and point the console at the https API in `.env.local`:

```
NEXT_PUBLIC_API_BASE=https://localhost:7159
```

**Both halves are required.** Moving only the console to https and leaving the
API on http produces a 401 on every request, which looks like a login problem
and is not one: the auth cookie is `SameSite=Lax`, and under schemeful
same-site `https://localhost` and `http://localhost` are different *sites* — the
port is irrelevant, the scheme is not. The browser therefore treats every API
call as cross-site and withholds the cookie. Matching the schemes is the fix;
relaxing the cookie to `SameSite=None` would work too and would give away the
browser's own CSRF protection to do it.

Both certificates are self-signed, so the browser will ask you to accept them
once for each origin.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
