# Postman collection

`CMES-StaffHR.postman_collection.json` holds every Staff/HR endpoint (81 requests) plus the Budget and Treasury
Finance API calls the integration uses, in numbered folders. Two environments set the addresses:

| Environment | `baseUrl` |
| --- | --- |
| `CMES-StaffHR-Azure.postman_environment.json` | the deployed app on Azure |
| `CMES-StaffHR-Local.postman_environment.json` | `http://localhost:5058` |

Both set `budgetUrl` to Budget's test deployment.

**Import:** Postman → Import → drop the collection and both environment files → pick an environment (top right).

**Ids flow between requests.** Requests that create something save its id in a collection variable
(`newStaffNumber`, `requisitionNumber`, `fundingRequestId`, `assignmentId`, `credentialId`, `rosterEntryId`,
`terminationNumber`), and later requests use it. So within a folder, run requests top to bottom, and run
*03 Staff › Onboard employee* before folders 04, 07 and 09 that use `newStaffNumber`.

**Scenarios.** Folder 13 holds two end-to-end flows to run with the Collection Runner:
- *Hire funded by Budget*: raise, review, send to Budget, Budget approves, Staff/HR reads the decision, fill.
- *Termination*: onboard, terminate, check the person is no longer eligible.

**Careful:** folders 08, 12 (PUT/POST) and the *Hire funded by Budget* scenario create real funding requests and
expenses in Budget's shared test system. Tell the Budget group before running them.

Without sign-in (demo mode, or Development locally) no token is needed; folder 11 uses `X-Dev-*` headers to act as
other callers. With Entra ID turned on, add an `Authorization: Bearer <token>` header at the collection level.
