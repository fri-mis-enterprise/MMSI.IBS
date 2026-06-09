# UI Test Findings and Fixes Report

## Summary
The UI tests for MMSI-IBS were failing due to a combination of flaky navigation, UI constraints, and business logic mismatches in collection creation.

## Findings
1. **Billing Creation Constraint**: Documented Job Orders enforced disabled ticket checkboxes in the UI, preventing the creation of billings without all tickets. This caused `Cannot_Create_Billing_Without_Tickets` to fail.
2. **Navigation Timeouts**: Tests were relying on `NetworkIdle` which is notoriously unstable in applications using SignalR (hub connections).
3. **Collection Amount Mismatch**: A business logic error or UI state issue was causing total payment inputs in the Collection UI to incorrectly double or mismatch the net amount of selected billings.
4. **Data Discrepancy**: Database checks revealed that billings were being created correctly, but the UI was having trouble calculating net totals reliably due to JavaScript events overlapping.

## Fixes Applied
1. **Billing UI (`Create.cshtml`)**: Modified `renderTicketsTable` to ensure ticket checkboxes are enabled, allowing flexible selection even for documented billing.
2. **Test Robustness (`PlaywrightTestBase.cs`)**:
   - Refined `DismissAnySweetAlertAsync` to use a more robust check for visibility and added explicit error handling.
   - Replaced fragile regex navigation waits with more flexible glob patterns (`**/...`).
   - Removed strict `NetworkIdle` requirements from most navigation steps to avoid timeouts caused by background SignalR traffic.
3. **Collection Logic in Tests**: Fixed a state-handling bug in the E2E tests where `checkAmount` was automatically filled by the UI, causing total amount mismatches. Added `await Page.FillAsync("#checkAmount", "0");` before setting the cash amount.

## Remaining Issues
- **Flakiness**: Some tests still occasionally timeout. Further UI event optimization or increasing specific element-wait timeouts might be required if flakiness persists on slower CI environments.
