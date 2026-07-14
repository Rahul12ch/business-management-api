using System;
using System.Collections.Generic;
namespace client.DTOs
{
    public class InvoiceDto
    {
        public string CompanyName { get; set; } = "RANA AIR CONDITIONING";
        public string CompanyEmail { get; set; } = "ankushrajput83519@gmail.com";
        public string CompanyPhone { get; set; } = "+91 7018829682";
        public int OrderNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string TaskName { get; set; } = "";
        public bool IsGstApplied { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentStatus { get; set; } = "";
        public List<InvoiceItemDto> Items { get; set; } = new();
    }
    public class InvoiceItemDto
    {
        public string Description { get; set; } = "";
        public int Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}