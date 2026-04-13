using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.ViewModels
{
    public class CreateCollectionViewModel
    {
        public int? MMSICollectionId { get; set; }

        public string? MMSICollectionNumber { get; set; }

        public bool IsUndocumented { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public string? ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        public decimal CashAmount { get; set; }

        public DateOnly? CheckDate { get; set; }

        public string? CheckNumber { get; set; }

        public string? CheckBank { get; set; }

        public string? CheckBranch { get; set; }

        public decimal CheckAmount { get; set; }

        public int? BankId { get; set; }

        public decimal Amount { get; set; } // Total amount

        public decimal EWT { get; set; }

        public decimal WVAT { get; set; }

        public DateOnly? DepositDate { get; set; }

        [NotMapped]
        public List<string>? ToCollectBillings { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Billings { get; set; }

        [NotMapped]
        public List<SelectListItem>? BankAccounts { get; set; }
    }
}
