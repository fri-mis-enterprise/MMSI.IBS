using IBS.Utility.Constants;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Services;
using IBS.Models.MMSI.ViewModels;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class BillingController(
        IUnitOfWork unitOfWork,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<BillingController> logger,
        IUserAccessService userAccessService)
        : Controller
    {
        private const string _filterTypeClaimType = "DispatchTicket.FilterType";

        public async Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            if (!await HasBillingAccessAsync(cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(Enumerable.Empty<Billing>());
        }

        private async Task UpdateFilterTypeClaim(string filterType)
        {
            var user = await userManager.GetUserAsync(User);

            if (user != null)
            {
                var existingClaim = (await userManager.GetClaimsAsync(user))
                    .FirstOrDefault(c => c.Type == _filterTypeClaimType);

                if (existingClaim != null)
                {
                    await userManager.RemoveClaimAsync(user, existingClaim);
                }

                if (!string.IsNullOrEmpty(filterType))
                {
                    await userManager.AddClaimAsync(user, new Claim(_filterTypeClaimType, filterType));
                }
            }
        }

        private async Task<string?> GetCurrentFilterType()
        {
            var user = await userManager.GetUserAsync(User);

            if (user != null)
            {
                var claims = await userManager.GetClaimsAsync(user);
                return claims.FirstOrDefault(c => c.Type == _filterTypeClaimType)?.Value;
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = await GetBillingSelectLists(new CreateBillingViewModel(), cancellationToken);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateModal(CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                return PartialView("_ErrorModal", new { message = "You don't have permission to create billings." });
            }

            var viewModel = await GetBillingSelectLists(new CreateBillingViewModel(), cancellationToken);
            return PartialView("_CreateModal", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBillingViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
                return Json(new { success = false, message = "Can't create entry, please review your input.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var model = CreateBillingVmToBillingModel(viewModel);

                if (model.CustomerId == null)
                {
                    throw new InvalidOperationException("Customer is required.");
                }

                model.Customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                if (model.Customer == null)
                {
                    throw new InvalidOperationException("Customer not found.");
                }

                model.IsVatable = model.Customer.VatType == "Vatable";
                model.Status = "For Collection";
                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                var datetimeNow = DateTimeHelper.GetCurrentPhilippineTime();
                model.CreatedDate = datetimeNow;

                if (model.IsUndocumented)
                {
                    model.MMSIBillingNumber = await unitOfWork.Billing.GenerateBillingNumber(cancellationToken);
                }
                else
                {
                    model.MMSIBillingNumber = viewModel.MMSIBillingNumber!;
                }

                if (model.ToBillDispatchTickets == null || !model.ToBillDispatchTickets.Any())
                {
                    throw new InvalidOperationException("At least one dispatch ticket must be selected.");
                }

                await unitOfWork.Billing.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                var newModel = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == model.MMSIBillingId, cancellationToken)
                    ?? throw new InvalidOperationException("Failed to retrieve the newly created billing record.");

                #region -- Audit Trail

                var audit = new AuditTrail(
                    await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    $"Create billing #{newModel.MMSIBillingNumber} for tickets #{string.Join(", #", model.ToBillDispatchTickets!)}",
                    "Billing",
                    await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                );

                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                #endregion -- Audit Trail

                decimal totalAmount = 0;

                logger.LogInformation("Processing ToBillDispatchTickets: {Tickets}",
                    string.Join(", ", model.ToBillDispatchTickets!));

                foreach (var billDispatchTicket in model.ToBillDispatchTickets!)
                {
                    if (!int.TryParse(billDispatchTicket, out int ticketId))
                    {
                        throw new InvalidOperationException($"Invalid dispatch ticket ID format: '{billDispatchTicket}'");
                    }

                    var dtEntry = await unitOfWork.DispatchTicket
                        .GetAsync(dt => dt.DispatchTicketId == ticketId, cancellationToken);
                    logger.LogInformation("Ticket {TicketId} result: {Result}", ticketId, dtEntry == null ? "NULL" : "FOUND");

                    if (dtEntry == null)
                    {
                        throw new InvalidOperationException($"Dispatch ticket #{ticketId} not found or has already been processed.");
                    }

                    totalAmount += dtEntry.TotalNetRevenue;
                    dtEntry.Status = "Billed";
                    dtEntry.BillingId = newModel.MMSIBillingId;
                    dtEntry.BillingNumber = newModel.MMSIBillingNumber;
                }

                newModel.Amount = totalAmount;
                newModel.Balance = totalAmount;
                newModel.IsPaid = false;
                newModel.Terms = newModel.PrincipalId != null
                    ? (await unitOfWork.Principal.GetAsync(p => p.PrincipalId == newModel.PrincipalId, cancellationToken))?.Terms
                    : newModel.Customer?.CustomerTerms;
                newModel.DueDate = await unitOfWork.Billing.ComputeDueDateAsync(newModel.Terms ?? "COD", newModel.Date, cancellationToken);
                newModel.Company = await GetCompanyClaimAsync() ?? "MMSI";

                await unitOfWork.SaveAsync(cancellationToken);

                await unitOfWork.Billing.PostAsync(newModel, cancellationToken);

                string message = model.IsUndocumented
                    ? $"Billing was successfully created. Control Number: {newModel.MMSIBillingNumber}"
                    : $"Billing #{newModel.MMSIBillingNumber} was successfully created.";

                return Json(new { success = true, message, redirectUrl = Url.Action(nameof(Index), new { filterType = await GetCurrentFilterType() }) });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create billing.");
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                if (errorMsg.Contains("unique") || errorMsg.Contains("23505"))
                    return Json(new { success = false, message = "Billing number already exists. Please use a different number." });
                if (errorMsg.Contains("foreign key") || errorMsg.Contains("23503"))
                    return Json(new { success = false, message = "Invalid reference. Please check your selections." });
                return Json(new { success = false, message = "Failed to save billing. Please try again or contact support." });
            }
        }

        public Billing CreateBillingVmToBillingModel(CreateBillingViewModel viewModel)
        {
            var model = new Billing
            {
                Date = viewModel.Date,
                IsUndocumented = viewModel.IsUndocumented,
                BilledTo = viewModel.BilledTo,
                VoyageNumber = viewModel.VoyageNumber,
                Amount = viewModel.Amount,
                IsPrincipal = viewModel.IsPrincipal,
                CustomerId = viewModel.CustomerId,
                PrincipalId = viewModel.PrincipalId,
                VesselId = viewModel.VesselId,
                PortId = viewModel.PortId,
                TerminalId = viewModel.TerminalId,
                ToBillDispatchTickets = viewModel.ToBillDispatchTickets,
                ApOtherTug = viewModel.ApOtherTug
            };

            if (viewModel.MMSIBillingId != null)
            {
                model.MMSIBillingId = viewModel.MMSIBillingId ?? 0;

                if (model.MMSIBillingId == 0)
                {
                    throw new NullReferenceException("MMSIBillingId cannot be zero.");
                }
            }

            return model;
        }

        public CreateBillingViewModel BillingModelToCreateBillingVm(Billing model)
        {
            var viewModel = new CreateBillingViewModel
            {
                MMSIBillingId = model.MMSIBillingId,
                MMSIBillingNumber = model.MMSIBillingNumber,
                Date = model.Date,
                IsUndocumented = model.IsUndocumented,
                BilledTo = model.BilledTo,
                VoyageNumber = model.VoyageNumber,
                Amount = model.Amount,
                IsPrincipal = model.IsPrincipal,
                CustomerId = model.CustomerId,
                PrincipalId = model.PrincipalId,
                VesselId = model.VesselId,
                PortId = model.PortId,
                TerminalId = model.TerminalId,
                ToBillDispatchTickets = model.ToBillDispatchTickets,
                ApOtherTug = model.ApOtherTug
            };

            return viewModel;
        }

        [HttpPost]
        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(t => intDispatchTicketIds.Contains(t.DispatchTicketId));

                return Json(new
                {
                    success = true,
                    data = dispatchTickets
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get dispatch tickets.");

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetBillingList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var filterTypeClaim = await GetCurrentFilterType();

                var queried = dbContext.Billings
                    .Include(b => b.Customer)
                    .Include(b => b.Terminal)
                    .ThenInclude(b => b!.Port)
                    .Include(b => b.Vessel)
                    .Where(b => b.Status != "For Posting" && b.Status != "Cancelled");

                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "ForPosting":
                            queried = queried.Where(dt =>
                                dt.Status == "For Posting");
                            break;
                        case "ForTariff":
                            queried = queried.Where(dt =>
                                dt.Status == "For Tariff");
                            break;
                        case "TariffPending":
                            queried = queried.Where(dt =>
                                dt.Status == "Tariff Pending");
                            break;
                        case "ForBilling":
                            queried = queried.Where(dt =>
                                dt.Status == "For Billing");
                            break;
                        case "ForCollection":
                            queried = queried.Where(dt =>
                                dt.Status == "For Collection");
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                        .Where(dt =>
                            dt.Date.Day.ToString().Contains(searchValue) == true ||
                            dt.Date.Month.ToString().Contains(searchValue) == true ||
                            dt.Date.Year.ToString().Contains(searchValue) == true ||
                            dt.MMSIBillingNumber.ToLower().Contains(searchValue) == true ||
                            dt.Amount.ToString().Contains(searchValue) == true ||
                            dt.Customer!.CustomerName.ToLower().Contains(searchValue) == true ||
                            dt.Terminal!.TerminalName!.ToLower().Contains(searchValue) == true ||
                            dt.Terminal.Port!.PortName!.ToLower().Contains(searchValue) == true ||
                            dt.Vessel!.VesselName.ToLower().Contains(searchValue) == true ||
                            dt.Status.ToLower().Contains(searchValue) == true
                        );
                }

                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();

                        switch (column.Data)
                        {
                            case "status":
                                if (searchValue == "for collection")
                                {
                                    queried = queried.Where(s => s.Status == "For Collection");
                                }
                                if (searchValue == "collected")
                                {
                                    queried = queried.Where(s => s.Status == "Collected");
                                }
                                break;
                        }
                    }
                }

                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = queried.Count();

                var pagedData = queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get billings.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await unitOfWork.Billing
                .GetAsync(b => b.MMSIBillingId == id, cancellationToken) ?? throw new NullReferenceException();

            var viewModel = BillingModelToCreateBillingVm(model);
            viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
            viewModel.UnbilledDispatchTickets = await GetEditTickets(viewModel.CustomerId, viewModel.MMSIBillingId ?? 0, cancellationToken);
            if(model.CustomerId != null)
            {
                viewModel.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);
            }

            viewModel.Terminals = await unitOfWork.Terminal
                .GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);

            viewModel.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            viewModel.Customers = await unitOfWork.Billing
                .GetMMSICustomersWithBillablesSelectList(viewModel.CustomerId, model.Customer!.Type, cancellationToken);

            if (viewModel.CustomerPrincipal?.Count == 0 || viewModel.CustomerPrincipal == null)
            {
                ViewData["HasPrincipal"] = false;
            }
            else
            {
                ViewData["HasPrincipal"] = true;
            }

            // Customer Details for display
            if (model.Customer != null)
            {
                ViewData["CustomerAddress"] = model.Customer.CustomerAddress ?? "-";
                ViewData["CustomerTIN"] = model.Customer.CustomerTin ?? "-";
                ViewData["CustomerTerms"] = model.Customer.CustomerTerms ?? "-";
                ViewData["CustomerBusinessStyle"] = model.Customer.BusinessStyle ?? "-";
                ViewData["CustomerVatType"] = model.Customer.VatType ?? "-";
                ViewData["CustomerType"] = model.Customer.Type ?? "-";
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditModal(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                return PartialView("_ErrorModal", new { message = "You don't have permission to edit billings." });
            }

            var model = await unitOfWork.Billing
                .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

            if (model == null)
            {
                return PartialView("_ErrorModal", new { message = "Billing not found." });
            }

            var viewModel = BillingModelToCreateBillingVm(model);
            viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
            viewModel.UnbilledDispatchTickets = await GetEditTickets(viewModel.CustomerId, viewModel.MMSIBillingId ?? 0, cancellationToken);
            if (model.CustomerId != null)
            {
                viewModel.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);
            }

            viewModel.Terminals = await unitOfWork.Terminal
                .GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);

            viewModel.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            viewModel.Customers = await unitOfWork.Billing
                .GetMMSICustomersWithBillablesSelectList(viewModel.CustomerId, model.Customer!.Type, cancellationToken);

            if (viewModel.CustomerPrincipal?.Count == 0 || viewModel.CustomerPrincipal == null)
            {
                ViewData["HasPrincipal"] = false;
            }
            else
            {
                ViewData["HasPrincipal"] = true;
            }

            return PartialView("_EditModal", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreateBillingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var model = CreateBillingVmToBillingModel(viewModel);

                    var currentModel = await unitOfWork.Billing
                        .GetAsync(b => b.MMSIBillingId == model.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                    var tempModel = await unitOfWork.DispatchTicket
                        .GetAllAsync(d => d.BillingNumber == model.MMSIBillingId.ToString(), cancellationToken);

                    var idsOfBilledTickets = tempModel.Select(d => d.DispatchTicketId.ToString()).OrderBy(x => x).ToList();
                    currentModel.ToBillDispatchTickets = idsOfBilledTickets;

                    if (model.CustomerId == null)
                    {
                        throw new InvalidOperationException("Customer is required.");
                    }

                    model.Customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                    if (model.Customer == null)
                    {
                        throw new InvalidOperationException("Customer not found.");
                    }

                    model.IsVatable = model.Customer.VatType == "Vatable";

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.CustomerId != model.CustomerId)
                    {
                        changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}");
                    }

                    if (currentModel.PrincipalId != model.PrincipalId)
                    {
                        changes.Add($"PrincipalId: #{string.Join(", #", currentModel.PrincipalId)} -> #{string.Join(", #", model.PrincipalId!)}");
                    }

                    if (currentModel.VoyageNumber != model.VoyageNumber)
                    {
                        changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}");
                    }

                    if (currentModel.Date != model.Date)
                    {
                        changes.Add($"Date: {currentModel.Date} -> {model.Date}");
                    }

                    if (currentModel.TerminalId != model.TerminalId)
                    {
                        changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}");
                    }

                    if (currentModel.VesselId != model.VesselId)
                    {
                        changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}");
                    }

                    if (currentModel.BilledTo != model.BilledTo)
                    {
                        changes.Add($"BilledTo: {currentModel.BilledTo} -> {model.BilledTo}");
                    }

                    if (currentModel.IsVatable != model.IsVatable)
                    {
                        changes.Add($"IsVatable: {currentModel.IsVatable} -> {model.IsVatable}");
                    }

                    if (!currentModel.ToBillDispatchTickets.OrderBy(x => x)
                            .SequenceEqual(model.ToBillDispatchTickets!.OrderBy(x => x)))
                    {
                        changes.Add($"ToBillDispatchTickets: #{string.Join(", #", currentModel.ToBillDispatchTickets)} -> #{string.Join(", #", model.ToBillDispatchTickets!)}");
                    }

                    #endregion -- Changes

                    #region -- Audit Trail

                    var activity = changes.Any()
                        ? $"Edit billing #{currentModel.MMSIBillingNumber} {string.Join(", ", changes)}"
                        : $"No changes detected for Billing #{currentModel.MMSIBillingNumber}";

                    var audit = new AuditTrail(
                        await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        activity,
                        "Billing",
                        await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                    );

                    await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.CustomerId = model.CustomerId;
                    currentModel.PrincipalId = model.PrincipalId;
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.Date = model.Date;
                    currentModel.PortId = model.PortId;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.BilledTo = model.BilledTo;
                    currentModel.Status = "For Collection";
                    currentModel.IsVatable = model.IsVatable;

                    model.UnbilledDispatchTickets = await unitOfWork.Billing
                        .GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

                    foreach (var dispatchTicket in model.UnbilledDispatchTickets)
                    {
                        var id = int.Parse(dispatchTicket.Value);

                        var dtModel = await unitOfWork.DispatchTicket
                            .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

                        dtModel!.Status = "For Billing";
                        dtModel.BillingId = null;
                        dtModel.BillingNumber = null;
                    }

                    await unitOfWork.DispatchTicket.SaveAsync(cancellationToken);
                    decimal totalAmount = 0;

                    foreach (var billDispatchTicket in model.ToBillDispatchTickets!)
                    {
                        var dtEntry = await unitOfWork.DispatchTicket
                            .GetAsync(dt => dt.DispatchTicketId == int.Parse(billDispatchTicket), cancellationToken);

                        totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                        dtEntry!.Status = "Billed";
                        dtEntry.BillingId = model.MMSIBillingId;
                        dtEntry.BillingNumber = model.MMSIBillingNumber;
                    }

                    currentModel.Amount = totalAmount;
                    await unitOfWork.SaveAsync(cancellationToken);
                    return Json(new { success = true, message = "Entry edited successfully!", redirectUrl = Url.Action(nameof(Index), new { filterType = await GetCurrentFilterType() }) });
                }
                else
                {
                    return Json(new { success = false, message = "Can't update entry, please review your input.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit billing.");
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                if (errorMsg.Contains("unique") || errorMsg.Contains("23505"))
                    return Json(new { success = false, message = "Billing number already exists. Please use a different number." });
                if (errorMsg.Contains("foreign key") || errorMsg.Contains("23503"))
                    return Json(new { success = false, message = "Invalid reference. Please check your selections." });
                return Json(new { success = false, message = "Failed to save changes. Please try again or contact support." });
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Billing
                    .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

                if (model != null)
                {
                    await unitOfWork.Billing.RemoveAsync(model, cancellationToken);
                    TempData["success"] = "Billing deleted successfully!";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    TempData["error"] = "Can't find entry.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete billing.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            model.PaidDispatchTickets = await unitOfWork.Billing
                .GetPaidDispatchTicketsAsync(model.MMSIBillingId, cancellationToken);

            model.UniqueTugboats = await unitOfWork.Billing
                .GetUniqueTugboatsListAsync(model.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

            model = unitOfWork.Billing.ProcessAddress(model, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await unitOfWork.Billing
                    .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

                if (billing == null)
                {
                    TempData["error"] = "Billing not found";
                    return RedirectToAction(nameof(Index));
                }

                billing.ToBillDispatchTickets = await unitOfWork.Billing
                    .GetToBillDispatchTicketListAsync(billing.MMSIBillingId, cancellationToken);

                billing.PaidDispatchTickets = await unitOfWork.Billing
                    .GetPaidDispatchTicketsAsync(billing.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                billing.UniqueTugboats = await unitOfWork.Billing
                    .GetUniqueTugboatsListAsync(billing.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Billing #{billing.MMSIBillingNumber}");
                worksheet.Cells.Style.Font.Name = "Calibri";
                worksheet.Cells["B2"].Value = $"{billing.Customer?.CustomerName}";
                worksheet.Cells["E2"].Value = $"{billing.Date}";
                worksheet.Cells["E2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells["B3"].Value = $"{billing.Customer?.CustomerAddress}                              TERMS: {billing.Customer?.CustomerTerms}";
                worksheet.Cells["B4"].Value = $"{billing.Customer?.CustomerTin}";
                worksheet.Cells["E4"].Value = $"VOYAGE NO. {billing.VoyageNumber}";
                worksheet.Cells["B6"].Value = $"FOR THE SERVICE RE: {billing.Vessel?.VesselName}";
                worksheet.Cells["B7"].Value = $"LOCATION PORT: {billing.Port!.PortName}";
                var rowStart = 9;
                var row = rowStart;

                foreach (var tugboat in billing.UniqueTugboats)
                {
                    worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: {tugboat}";
                    row++;

                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.Tugboat?.TugboatName == tugboat))
                    {
                        worksheet.Cells[row, 1].Value = "1";
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                        worksheet.Cells[row, 4].Value = $"{ticket.DispatchRate}";
                        worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 5].Value = $"{ticket.DispatchBillingAmount}";
                        worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        row++;
                    }

                    row++;
                }

                var ticketsWithBaf = billing.PaidDispatchTickets;

                if (ticketsWithBaf != null)
                {
                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                    {
                        worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: BUNKER ADJUSTMENT FACTOR";
                        row++;

                        foreach (var record in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                        {
                            worksheet.Cells[row, 1].Value = "1";
                            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                            worksheet.Cells[row, 4].Value = $"{ticket.BAFRate}";
                            worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, 5].Value = $"{ticket.BAFNetRevenue}";
                            worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            row++;
                        }

                        row++;
                    }
                }

                worksheet.Cells[1, 1, row, 7].Style.Font.Name = "Calibri";
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 53;
                worksheet.Column(3).Width = 9;
                worksheet.Column(4).Width = 8.5;
                worksheet.Column(5).Width = 16;
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DotMatrix_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to print billing.");
                TempData["error"] = ex.Message;
                logger.LogError(ex, "Error generating sales report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                ex.Message, ex.StackTrace, userManager.GetUserAsync(User));
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetCustomerDetails(int customerId)
        {
            var customerDetails = await unitOfWork.Customer
                .GetAsync(c => c.CustomerId == customerId);

            if (customerDetails == null)
            {
                return new JsonResult("Customer not found");
            }

            var principal = await unitOfWork.Principal
                .GetAsync(c => c.CustomerId == customerId);

            var hasPrincipal = principal != null;

            var customerDetailsJson = new
            {
                terms = customerDetails.CustomerTerms,
                address = customerDetails.CustomerAddress,
                tinNo = customerDetails.CustomerTin,
                businessStyle = customerDetails.BusinessStyle,
                hasPrincipal,
                vatType = customerDetails.VatType,
                isUndoc = customerDetails.Type
            };

            return Json(customerDetailsJson);
        }

        [HttpGet]
        public async Task<JsonResult> SearchCustomers(string term, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return Json(new List<object>());
            }

            var customers = await unitOfWork.Customer
                .GetAllAsync(c => c.CustomerName!.ToLower().Contains(term.ToLower()) ||
                                  c.CustomerCode!.ToLower().Contains(term.ToLower()),
                             cancellationToken);

            var result = customers.Select(c => new
            {
                value = c.CustomerId,
                name = c.CustomerName,
                hasPrincipal = unitOfWork.Principal.GetAllAsync(p => p.CustomerId == c.CustomerId, cancellationToken).Result.Any(),
                vatType = c.VatType,
                isUndoc = c.Type
            }).Take(10).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> SearchPrincipals(string term, int customerId, CancellationToken cancellationToken)
        {
            var principals = await unitOfWork.Principal
                .GetAllAsync(p => p.CustomerId == customerId, cancellationToken);

            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                principals = principals.Where(p =>
                    p.PrincipalName!.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.PrincipalNumber!.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var result = principals.Select(p => new
            {
                value = p.PrincipalId,
                name = p.PrincipalName
            }).Take(10).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetPrincipalDetails(int principalId)
        {
            var customerDetails = await unitOfWork.Principal
                .GetAsync(c => c.PrincipalId == principalId);

            if (customerDetails == null)
            {
                return new JsonResult("Principal not found");
            }

            var customerDetailsJson = new
            {
                terms = customerDetails.Terms,
                address = customerDetails.Address,
                tinNo = customerDetails.TIN,
                businessStyle = customerDetails.BusinessType
            };

            return Json(customerDetailsJson);
        }

        [HttpGet]
        public async Task<JsonResult> SearchJobOrders(string term, int customerId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
            {
                return Json(new List<object>());
            }

            var jobOrders = await unitOfWork.JobOrder
                .GetAllAsync(j => j.CustomerId == customerId &&
                    j.JobOrderNumber.ToLower().Contains(term.ToLower()),
                    cancellationToken);

            var result = jobOrders.Select(j => new
            {
                value = j.JobOrderId,
                name = j.JobOrderNumber,
                description = j.Remarks ?? ""
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetDispatchTicketsByJobOrder(int jobOrderId, CancellationToken cancellationToken)
        {
            var tickets = await unitOfWork.DispatchTicket
                .GetAllAsync(t => t.JobOrderId == jobOrderId &&
                    t.Status == "For Billing" &&
                    t.BillingId == null,
                    cancellationToken);

            var result = tickets.Select(t => new
            {
                dispatchTicketId = t.DispatchTicketId,
                dispatchNo = t.DispatchNumber,
                tugboat = t.Tugboat?.TugboatName ?? "N/A",
                service = t.Service?.ServiceName ?? "N/A",
                duration = t.TotalHours,
                dispatchAmount = t.DispatchBillingAmount,
                bafAmount = t.BAFBillingAmount,
                totalAmount = t.TotalBilling
            }).ToList();

            return Json(result);
        }

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

        private async Task<string?> GetUserNameAsync()
        {
            var user = await userManager.GetUserAsync(User);
            return user?.UserName;
        }

        [HttpGet]
        public async Task<IActionResult> GetPrincipalsJson(string customerId, CancellationToken cancellationToken)
        {
            var principalsList = await GetPrincipals(customerId, cancellationToken);
            return Json(principalsList);
        }

        [HttpGet]
        public async Task<List<SelectListItem>?> GetPrincipals(string? customerId, CancellationToken cancellationToken)
        {
            if (customerId == null)
            {
                return null;
            }

            var principals = await unitOfWork.Principal
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId), cancellationToken);

            var principalsList = principals.Select(t => new SelectListItem
            {
                Value = t.PrincipalId.ToString(),
                Text = t.PrincipalName
            }).ToList();

            return principalsList;
        }

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketsByCustomer(string customerId, CancellationToken cancellationToken)
        {
            //order by dispatch number
            var dispatchTickets = await unitOfWork.DispatchTicket
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId) && t.Status == "For Billing", cancellationToken);

            var principalsList = dispatchTickets.Select(t => new SelectListItem
            {
                Value = t.DispatchTicketId.ToString(),
                Text = t.DispatchNumber
            }).ToList();

            return Json(principalsList);
        }

        [HttpPost]
        public async Task<List<SelectListItem>?> GetEditTickets(int? customerId, int billingId, CancellationToken cancellationToken = default)
        {
            var listToReturn = await unitOfWork.Billing.GetMMSIUnbilledTicketsByCustomer(customerId, cancellationToken);
            IEnumerable<DispatchTicket>? billedTickets = null;

            if (billingId != 0)
            {
                billedTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == billingId, cancellationToken);
            }

            if (billedTickets != null && billedTickets.FirstOrDefault()?.CustomerId == customerId)
            {
                listToReturn?.AddRange(await unitOfWork.Billing.GetMMSIBilledTicketsById(billingId, cancellationToken));
            }

            return listToReturn;
        }

        public async Task<CreateBillingViewModel> GetBillingSelectLists(CreateBillingViewModel viewModel, CancellationToken cancellationToken = default)
        {
            viewModel.Vessels = await unitOfWork.Vessel.GetMMSIVesselsSelectList(cancellationToken);
            viewModel.Ports = await unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);

            if (viewModel.PortId != 0)
            {
                viewModel.Terminals = await unitOfWork.Terminal.GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);
            }

            return viewModel;
        }

        private async Task<bool> HasBillingAccessAsync(CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User)!;
            return await userAccessService.CheckAccess(userId, ProcedureEnum.CreateBilling, cancellationToken);
        }
    }
}
