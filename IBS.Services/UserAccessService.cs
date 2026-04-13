using IBS.DataAccess.Data;
using IBS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.Services
{
    public interface IUserAccessService
    {
        Task<bool> CheckAccess(string id, ProcedureEnum procedure, CancellationToken cancellationToken = default);
    }

    public class UserAccessService(ApplicationDbContext dbContext): IUserAccessService
    {
        public async Task<bool> CheckAccess(string id, ProcedureEnum procedure, CancellationToken cancellationToken = default)
        {
            var userAccess = await dbContext.MMSIUserAccesses
                .FirstOrDefaultAsync(a => a.UserId == id, cancellationToken);

            if (userAccess == null)
            {
                return false;
            }

            switch (procedure)
            {
                #region -- MSAP Workflow --

                case ProcedureEnum.CreateServiceRequest:
                    return userAccess.CanCreateServiceRequest;
                case ProcedureEnum.PostServiceRequest:
                    return userAccess.CanPostServiceRequest;
                case ProcedureEnum.CreateDispatchTicket:
                    return userAccess.CanCreateDispatchTicket;
                case ProcedureEnum.EditDispatchTicket:
                    return userAccess.CanEditDispatchTicket;
                case ProcedureEnum.CancelDispatchTicket:
                    return userAccess.CanCancelDispatchTicket;
                case ProcedureEnum.SetTariff:
                    return userAccess.CanSetTariff;
                case ProcedureEnum.ApproveTariff:
                    return userAccess.CanApproveTariff;
                case ProcedureEnum.CreateBilling:
                    return userAccess.CanCreateBilling;
                case ProcedureEnum.CreateCollection:
                    return userAccess.CanCreateCollection;
                case ProcedureEnum.CreateJobOrder:
                    return userAccess.CanCreateJobOrder;
                case ProcedureEnum.EditJobOrder:
                    return userAccess.CanEditJobOrder;
                case ProcedureEnum.DeleteJobOrder:
                    return userAccess.CanDeleteJobOrder;
                case ProcedureEnum.CloseJobOrder:
                    return userAccess.CanCloseJobOrder;

                #endregion -- MSAP Workflow --

                #region -- Treasury --

                case ProcedureEnum.AccessTreasury:
                    return userAccess.CanAccessTreasury;
                case ProcedureEnum.CreateDisbursement:
                    return userAccess.CanCreateDisbursement;

                #endregion -- Treasury --

                #region -- MSAP Import --

                case ProcedureEnum.ManageMsapImport:
                    return userAccess.CanManageMsapImport;

                #endregion -- MSAP Import --

                #region -- Reports --

                case ProcedureEnum.ViewGeneralLedger:
                    return userAccess.CanViewGeneralLedger;
                case ProcedureEnum.ViewInventoryReport:
                    return userAccess.CanViewInventoryReport;
                case ProcedureEnum.ViewMaritimeReport:
                    return userAccess.CanViewMaritimeReport;

                #endregion -- Reports --

                default:
                    return false;
            }
        }
    }
}
