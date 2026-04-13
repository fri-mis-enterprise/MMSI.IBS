using IBS.Models.Integrated;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.ViewModels
{
    public class CustomerOrderSlipForApprovalViewModel
    {
        #region Ops Manager Approval

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatProductCost { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatCosPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatFreightCharge { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatCommission { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal GrossMargin { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }

        #endregion

        public CustomerOrderSlip? CustomerOrderSlip { get; set; }

        #region Finance Approval

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal AvailableCreditLimit { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        #endregion

        public string Status { get; set; } = null!;

        public string PriceReference { get; set; } = null!;

        public List<COSFileInfo> UploadedFiles { get; set; } = null!;
    }
}
