using System;
using System.Linq;
using System.Web.Mvc;
using EBillingSoft.Models;

public class HomeController : Controller
{
    private EBillingSoftEntities2 db = new EBillingSoftEntities2();

    // GET: Home
    public ActionResult Index()
    {
        var products = db.Products.ToList();
        return View(products);
    }

    // GET: Home/GenerateBill
    public ActionResult GenerateBill()
    {
        // Fetch all products from the database
        var products = db.Products.ToList();

        // Create a SelectList from the products, using the product_id as the value and product_name as the display text
        ViewBag.Products = new SelectList(products, "product_id", "product_name");

        // Return the view
        return View();
    }

   
    // POST: Home/GenerateBill
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult GenerateBill(int[] product_id, int[] quantities, string customerName)
    {
        // Validate input data
        if (product_id == null || quantities == null || product_id.Length != quantities.Length || string.IsNullOrEmpty(customerName))
        {
            ModelState.AddModelError("", "Please select products and enter customer details.");
            var products = db.Products.ToList();
            ViewBag.Products = new SelectList(products, "product_id", "product_name");
            return View();
        }

        // Create a new invoice
        Invoice newInvoice = new Invoice
        {
            invoice_date = DateTime.Now,
            customer_name = customerName,
            total_amount = 0
        };

        db.Invoices.Add(newInvoice);
        db.SaveChanges(); // Save the invoice to get the invoiceId

        decimal total_amount = 0;
        bool stockError = false;

        // Loop through selected products and calculate total amount
        for (int i = 0; i < product_id.Length; i++)
        {
            int productid = product_id[i];
            int quantity = quantities[i];

            var product = db.Products.Find(productid);
            if (product != null)
            {
                // Check if enough stock is available
                if (product.stock_quantity >= quantity)
                {
                    decimal unit_price = product.price;
                    decimal total_price = unit_price * quantity;

                    // Calculate tax for the product (if there's a tax associated)
                    decimal taxAmount = 0;
                    if (product.Tax != null && product.Tax.tax_percentage.HasValue)
                    {
                        // Assuming the product is linked to a tax, calculate the tax amount
                        taxAmount = (product.Tax.tax_percentage.Value / 100) * total_price;
                    }

                    // Calculate total price with tax
                    decimal totalWithTax = total_price + taxAmount;

                    // Add details to the InvoiceDetails table
                    var invoiceDetail = new InvoiceDetail
                    {
                        invoice_id = newInvoice.invoice_id,
                        product_id = productid,
                        quantity = quantity,
                        unit_price = unit_price,
                        total_price = total_price,
                        tax_amount = taxAmount,         // Add tax amount
                        total_with_tax = totalWithTax   // Add total price including tax
                    };

                    db.InvoiceDetails.Add(invoiceDetail);

                    // Update stock quantity
                    product.stock_quantity -= quantity;
                    db.Entry(product).State = System.Data.Entity.EntityState.Modified;

                    // Add to the total amount of the invoice (total with tax)
                    total_amount += totalWithTax;
                }
                else
                {
                    ModelState.AddModelError("", $"Insufficient stock for {product.product_name}. Current stock: {product.stock_quantity}");
                    stockError = true;
                }
            }
            else
            {
                ModelState.AddModelError("", $"Product with ID {productid} not found.");
                stockError = true;
            }
        }

        // Only save if no stock errors occurred
        if (stockError)
        {
            db.Entry(newInvoice).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();
            var products = db.Products.ToList();
            ViewBag.Products = new SelectList(products, "product_id", "product_name");
            return View();
        }

        // Update total amount in the invoice (including tax) and save changes
        newInvoice.total_amount = total_amount;
        db.Entry(newInvoice).State = System.Data.Entity.EntityState.Modified;
        db.SaveChanges();

        // Redirect to the invoice details page
        return RedirectToAction("InvoiceDetails", new { id = newInvoice.invoice_id });
    }



    // GET: Home/InvoiceDetails/5
    public ActionResult InvoiceDetails(int id)
    {
        var invoice = db.Invoices.Find(id);
        if (invoice == null)
        {
            return HttpNotFound();
        }

        var invoiceDetails = db.InvoiceDetails.Where(d => d.invoice_id == id).ToList();
        ViewBag.Invoice = invoice;
        return View(invoiceDetails);
    }

    public JsonResult GetProductDetails(int id)
    {
        var product = db.Products.Find(id); // Find product by ID from the database
        if (product != null)
        {
            // Return both price and tax percentage
            return Json(new { price = product.price, tax_percentage = product.Tax != null ? product.Tax.tax_percentage : (decimal?)null }, JsonRequestBehavior.AllowGet);
        }

        return Json(null, JsonRequestBehavior.AllowGet); // Return null if no product found
    }


}
