namespace IBS.Models.Enums
{
    public enum Enum
    {

    }

    /// <summary>
    /// Defines all access procedures for MMSI permission system.
    /// Grouped by module for easier management.
    /// </summary>
    public enum ProcedureEnum
    {
        #region -- MSAP Workflow --

        CreateServiceRequest,
        PostServiceRequest,
        CreateDispatchTicket,
        EditDispatchTicket,
        CancelDispatchTicket,
        SetTariff,
        ApproveTariff,
        CreateBilling,
        CreateCollection,
        CreateJobOrder,
        EditJobOrder,
        DeleteJobOrder,
        CloseJobOrder,

        #endregion -- MSAP Workflow --

        #region -- Treasury --

        AccessTreasury,
        CreateDisbursement,

        #endregion -- Treasury --

        #region -- MSAP Import --

        ManageMsapImport,

        #endregion -- MSAP Import --

        #region -- Reports --

        ViewGeneralLedger,
        ViewInventoryReport,
        ViewMaritimeReport,

        #endregion -- Reports --
    }
}
