using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EBillingSoft.Models
{
    public class InvoiceDetailViewModel
    {
        public string product_name { get; set; }
        public int quantity { get; set; }
        public decimal unit_price { get; set; }
        public decimal total_price { get; set; }
    }
}
