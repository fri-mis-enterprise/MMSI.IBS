using IBS.Models;
using IBS.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IBS.Services.AccessControl
{
    /// <summary>
    /// Centralized user access control service for all modules
    /// </summary>
    public interface IAccessControlService
    {
        Task<bool> HasAccessAsync(string userId, params ProcedureEnum[] procedures);
        Task<bool> HasAnyAccessAsync(string userId, params ProcedureEnum[] procedures);
        Task<bool> HasAllAccessAsync(string userId, params ProcedureEnum[] procedures);
        Task<Dictionary<ProcedureEnum, bool>> GetAccessMapAsync(string userId, params ProcedureEnum[] procedures);
    }

    public class AccessControlService : IAccessControlService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserAccessService _userAccessService;

        public AccessControlService(UserManager<ApplicationUser> userManager, IUserAccessService userAccessService)
        {
            _userManager = userManager;
            _userAccessService = userAccessService;
        }

        /// <summary>
        /// Check if user has access to ANY of the specified procedures
        /// </summary>
        public async Task<bool> HasAnyAccessAsync(string userId, params ProcedureEnum[] procedures)
        {
            if (procedures == null || procedures.Length == 0)
                return false;

            foreach (var procedure in procedures)
            {
                if (await _userAccessService.CheckAccess(userId, procedure, default))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if user has access to ALL of the specified procedures
        /// </summary>
        public async Task<bool> HasAllAccessAsync(string userId, params ProcedureEnum[] procedures)
        {
            if (procedures == null || procedures.Length == 0)
                return false;

            foreach (var procedure in procedures)
            {
                if (!await _userAccessService.CheckAccess(userId, procedure, default))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check access - alias for HasAnyAccessAsync for backward compatibility
        /// </summary>
        public async Task<bool> HasAccessAsync(string userId, params ProcedureEnum[] procedures)
        {
            return await HasAnyAccessAsync(userId, procedures);
        }

        /// <summary>
        /// Get a map of procedure to access status
        /// </summary>
        public async Task<Dictionary<ProcedureEnum, bool>> GetAccessMapAsync(string userId, params ProcedureEnum[] procedures)
        {
            var accessMap = new Dictionary<ProcedureEnum, bool>();

            foreach (var procedure in procedures)
            {
                accessMap[procedure] = await _userAccessService.CheckAccess(userId, procedure, default);
            }

            return accessMap;
        }
    }

    /// <summary>
    /// Extension methods for easy access control in controllers
    /// </summary>
    public static class AccessControlExtensions
    {
        #region -- MSAP Workflow --

        public static async Task<bool> HasJobOrderAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateJobOrder,
                ProcedureEnum.EditJobOrder,
                ProcedureEnum.DeleteJobOrder,
                ProcedureEnum.CloseJobOrder);
        }

        public static async Task<bool> HasServiceRequestAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateServiceRequest,
                ProcedureEnum.PostServiceRequest);
        }

        public static async Task<bool> HasDispatchTicketAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateDispatchTicket,
                ProcedureEnum.EditDispatchTicket,
                ProcedureEnum.CancelDispatchTicket);
        }

        public static async Task<bool> HasBillingAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateBilling);
        }

        public static async Task<bool> HasCollectionAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateCollection);
        }

        public static async Task<bool> HasTariffAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.SetTariff,
                ProcedureEnum.ApproveTariff);
        }

        public static async Task<bool> HasMsapAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.CreateServiceRequest,
                ProcedureEnum.PostServiceRequest,
                ProcedureEnum.CreateDispatchTicket,
                ProcedureEnum.EditDispatchTicket,
                ProcedureEnum.CancelDispatchTicket,
                ProcedureEnum.SetTariff,
                ProcedureEnum.ApproveTariff,
                ProcedureEnum.CreateBilling,
                ProcedureEnum.CreateCollection,
                ProcedureEnum.CreateJobOrder,
                ProcedureEnum.EditJobOrder,
                ProcedureEnum.DeleteJobOrder,
                ProcedureEnum.CloseJobOrder);
        }

        #endregion -- MSAP Workflow --

        #region -- Treasury --

        public static async Task<bool> HasTreasuryAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.AccessTreasury,
                ProcedureEnum.CreateDisbursement);
        }

        public static async Task<bool> HasDisbursementAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.AccessTreasury,
                ProcedureEnum.CreateDisbursement);
        }

        #endregion -- Treasury --

        #region -- MSAP Import --

        public static async Task<bool> HasMsapImportAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.ManageMsapImport);
        }

        #endregion -- MSAP Import --

        #region -- Reports --

        public static async Task<bool> HasGeneralLedgerReportAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.ViewGeneralLedger);
        }

        public static async Task<bool> HasInventoryReportAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.ViewInventoryReport);
        }

        public static async Task<bool> HasMaritimeReportAccessAsync(this IAccessControlService accessControl, string userId)
        {
            return await accessControl.HasAnyAccessAsync(userId,
                ProcedureEnum.ViewMaritimeReport);
        }

        #endregion -- Reports --
    }
}
