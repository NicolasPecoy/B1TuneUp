# B1TuneUp License Portal

Minimal commercial licenser for B1TuneUp.

## Run

```powershell
dotnet run --project B1TuneUp.LicensePortal\B1TuneUp.LicensePortal.csproj
```

The portal creates `license-private.xml`, `license-public.xml` and `license-store.json` in the application folder.

## Online activation

```http
POST /api/license/activate
Content-Type: application/json

{
  "customer": "My Company",
  "edition": "Premium",
  "companyDb": "SBODEMOUS",
  "installationNumber": "123456",
  "hardwareKey": "SBODEMOUS|123456|10.0",
  "modules": ["MacroEngine", "B1Search", "PrintDelivery"],
  "months": 12,
  "maxUsers": 25
}
```

Copy the returned `publicKeyXml` into B1TuneUp setting `PRODUCT_LICENSE_RSA_PUBLIC_KEY`, then paste the returned token in **Config Center > Lifecycle / Samples > License Key**.

## Offline activation

1. In B1TuneUp generate the offline request with `ProductLifecycleService.GenerateOfflineActivationRequest()`.
2. Send that request to `POST /api/license/offline/response`.
3. Paste the returned `B1TRSA...` token into the add-on.

## Operations

- `POST /api/license/revoke/{activationId}` marks an activation as revoked in the portal store.
- `POST /api/license/renew/{activationId}?months=12` renews and reissues a token.
- `GET /api/license/activations` lists activations for audit/support.
