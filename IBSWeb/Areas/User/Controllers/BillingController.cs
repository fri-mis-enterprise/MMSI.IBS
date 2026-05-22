using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using IBS.Services.Attributes;
using IBS.Services;
using IBS.Utility.Constants;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Controller for managing Billing in the MMSI system.
    /// </summary>
    [Area("User")]
    public class BillingController(
        IUnitOfWork unitOfWork,
        IBillingService billingService,
        UserManager<ApplicationUser> userManager,
        ILogger<BillingController> logger)
        : Controller
    {
        #region Index

        /// <summary>
        /// Displays the list of Billings.
        /// </summary>
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            try
            {
                ViewBag.FilterType = filterType;
                return Task.FromResult<IActionResult>(View(Enumerable.Empty<Billing>()));
            }
            catch (Exception exception)
            {
                return Task.FromException<IActionResult>(exception);
            }
        }

        #endregion

        #region Create

        /// <summary>
        /// Displays the form to create a new Billing.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = await billingService.PopulateBillingSelectListsAsync(new Billing(), cancellationToken);
            return View(model);
        }

        /// <summary>
        /// Processes the creation of a new Billing and posts it immediately.
        /// </summary>
        [HttpPost]
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Create(Billing model, CancellationToken cancellationToken)
        {
            try
            {
                var username = await GetUserNameAsync() ?? "System";
                var company = await GetCompanyClaimAsync() ?? SD.Company_MMSI;

                var result = await billingService.CreateBillingAsync(model, username, company, cancellationToken);

                if (result.IsSuccess)
                {
                    var msg = model.IsUndocumented ? $"Created. Control No: {model.MMSIBillingNumber}" : $"Billing created successfully.";
                    return Success(msg, new { redirectUrl = Url.Action(nameof(Index)) });
                }

                return Failure(null, result.Message);
            }
            catch (Exception ex)
            {
                return Failure(ex, "Failed to create billing.");
            }
        }

        #endregion

        #region Edit

        /// <summary>
        /// Displays the form to edit an existing Billing.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.EditBilling)]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await billingService.GetBillingByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            model = await billingService.PopulateBillingSelectListsAsync(model, cancellationToken);
            model.UnbilledDispatchTickets = await billingService.GetEditTicketsSelectListAsync(model.CustomerId, model.MMSIBillingId, cancellationToken);

            if (model.CustomerId != 0)
            {
                model.CustomerPrincipal = await billingService.GetPrincipalsSelectListAsync(model.CustomerId, cancellationToken);
            }

            model.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            ViewData["HasPrincipal"] = model.CustomerPrincipal is { Count: > 0 };

            ViewData["CustomerAddress"] = model.Customer.CustomerAddress;
            ViewData["CustomerTIN"] = model.Customer.CustomerTin;
            ViewData["CustomerTerms"] = model.Customer.CustomerTerms;
            ViewData["CustomerBusinessStyle"] = model.Customer.BusinessStyle ?? "-";
            ViewData["CustomerVatType"] = model.Customer.VatType;
            ViewData["CustomerType"] = model.Customer.Type;

            return View(model);
        }

        /// <summary>
        /// Processes the update of an existing Billing, including ticket reallocation.
        /// </summary>
        [HttpPost]
        [RequireAccess(ProcedureEnum.EditBilling)]
        public async Task<IActionResult> Edit([Bind("MMSIBillingId,Date,IsUndocumented,BilledTo,VoyageNumber,COSNumber,Amount,IsPrincipal,CustomerId,PrincipalId,VesselId,PortId,TerminalId,ToBillDispatchTickets,ApOtherTug,JobOrderId")] Billing model, IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                var result = await billingService.UpdateBillingAsync(model, await GetUserNameAsync() ?? "System", cancellationToken);

                if (result.IsSuccess)
                {
                    return Success(result.Message ?? "Entry edited successfully!", new { redirectUrl = Url.Action(nameof(Index)) });
                }

                if (result.Status == ServiceResultStatus.NotFound)
                {
                    return NotFound();
                }

                return Failure(null, result.Message);
            }
            catch (Exception ex)
            {
                return Failure(ex, "Failed to edit billing.");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes a specific Billing.
        /// </summary>
        [RequireAccess(ProcedureEnum.DeleteBilling)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await billingService.DeleteBillingAsync(id, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Post to Books

        /// <summary>
        /// Posts a specific Billing to the Sales Book and General Ledger.
        /// </summary>
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var result = await billingService.PostBillingAsync(id, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Preview & Print

        /// <summary>
        /// Displays a preview of the Billing, including associated tickets and tugboats.
        /// </summary>
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await billingService.GetBillingByIdAsync(id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            model.PaidDispatchTickets = await unitOfWork.Billing
                .GetPaidDispatchTicketsAsync(model.MMSIBillingId, cancellationToken);

            model.UniqueTugboats = await unitOfWork.Billing
                .GetUniqueTugboatsListAsync(model.MMSIBillingId, cancellationToken);

            unitOfWork.Billing.ProcessAddress(model, cancellationToken);
            return View(model);
        }

        /// <summary>
        /// Generates an Excel file for dot-matrix printing of the Billing.
        /// </summary>
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await billingService.GenerateExcelForPrintingAsync(id, cancellationToken);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DotMatrix_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to print billing.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region AJAX Endpoints

        /// <summary>
        /// Retrieves detailed information for a list of Dispatch Tickets.
        /// </summary>
        [HttpPost]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(t => intDispatchTicketIds.Contains(t.DispatchTicketId));

                return Json(new { success = true, data = dispatchTickets });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get dispatch tickets.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a paged and filtered list of Billings for DataTables.
        /// </summary>
        [HttpPost]
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> GetBillingList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var (data, filtered, total) = await billingService.GetPagedBillingsAsync(parameters, cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get billings.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Searches for customers matching a search term.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchCustomers(string? term, CancellationToken cancellationToken)
        {
            var result = await billingService.SearchCustomersAsync(term, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Searches for principals associated with a specific customer.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchPrincipals(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.SearchPrincipalsAsync(term, customerId, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Searches for Job Orders for a customer that have unbilled tickets and are ready for billing.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchJobOrders(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.SearchJobOrdersAsync(term, customerId, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Retrieves unbilled 'For Billing' tickets associated with a specific Job Order.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> GetDispatchTicketsByJobOrder(int jobOrderId, CancellationToken cancellationToken)
        {
            var result = await billingService.GetDispatchTicketsByJobOrderAsync(jobOrderId, cancellationToken);

            if (result is { IsSuccess: true, Data: not null })
            {
                return Json(new
                {
                    success = true,
                    header = result.Data.Header,
                    tickets = result.Data.Tickets
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Retrieves principals for a customer as a JSON list.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetPrincipalsJson(string customerId, CancellationToken cancellationToken)
        {
            var principalsList = await billingService.GetPrincipalsSelectListAsync(int.Parse(customerId), cancellationToken);
            return Json(principalsList);
        }

        /// <summary>
        /// Retrieves unbilled tickets for a customer as a JSON list.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetDispatchTicketsByCustomer(string customerId, CancellationToken cancellationToken)
        {
            var dispatchTickets = await unitOfWork.DispatchTicket
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId) && t.Status == SD.DispatchTicketStatus.ForBilling, cancellationToken);

            var ticketsList = dispatchTickets.Select(t => new SelectListItem
            {
                Value = t.DispatchTicketId.ToString(),
                Text = t.DispatchNumber
            }).ToList();

            return Json(ticketsList);
        }

        /// <summary>
        /// Retrieves detailed information for a specific customer.
        /// </summary>
        [HttpPost]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.GetCustomerDetailsAsync(customerId, cancellationToken);
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return result.Status == ServiceResultStatus.NotFound ? NotFound() : BadRequest(result.Message);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Returns a success JSON result with an optional redirect URL.
        /// </summary>
        private JsonResult Success(string message, object? data = null)
        {
            var redirectUrl = data?.GetType().GetProperty("redirectUrl")?.GetValue(data);
            return Json(new { success = true, message, redirectUrl });
        }

        /// <summary>
        /// Returns a failure JSON result with error message and logging.
        /// </summary>
        private JsonResult Failure(Exception? ex = null, string? message = null, object? data = null)
        {
            if (ex != null)
            {
                logger.LogError(ex, message ?? "An error occurred.");
            }

            var finalMessage = message ?? "Operation failed.";
            if (ex != null)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                if (errorMsg.Contains("unique") || errorMsg.Contains("23505"))
                {
                    finalMessage = "Billing number already exists.";
                }
                else if (errorMsg.Contains("foreign key") || errorMsg.Contains("23503"))
                {
                    finalMessage = "Invalid reference selected.";
                }
                else
                {
                    finalMessage = ex.Message;
                }
            }

            var errors = data?.GetType().GetProperty("errors")?.GetValue(data);
            return Json(new { success = false, message = finalMessage, errors });
        }

        /// <summary>
        /// Retrieves the company claim value for the current user.
        /// </summary>
        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        /// <summary>
        /// Retrieves the username of the current user.
        /// </summary>
        private async Task<string?> GetUserNameAsync()
        {
            var user = await userManager.GetUserAsync(User);
            return user?.UserName;
        }

        #endregion
    }
}
