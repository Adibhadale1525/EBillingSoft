using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using EBillingSoft.Models;
using System.Data.Entity;

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
        var products = db.Products.ToList();
        ViewBag.Products = new SelectList(products, "product_id", "product_name");
        return View();
    }

    // POST: Home/GenerateBill
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult GenerateBill(int[] product_id, int[] quantities, string customerName)
    {
        if (product_id == null || quantities == null || product_id.Length != quantities.Length || string.IsNullOrEmpty(customerName))
        {
            ModelState.AddModelError("", "Please select products and enter customer details.");
            var products = db.Products.ToList();
            ViewBag.Products = new SelectList(products, "product_id", "product_name");
            return View();
        }

        Invoice newInvoice = new Invoice
        {
            invoice_date = DateTime.Now,
            customer_name = customerName,
            total_amount = 0
        };

        db.Invoices.Add(newInvoice);
        db.SaveChanges();

        decimal total_amount = 0;
        bool stockError = false;

        for (int i = 0; i < product_id.Length; i++)
        {
            int productid = product_id[i];
            int quantity = quantities[i];

            var product = db.Products.Find(productid);
            if (product != null)
            {
                if (product.stock_quantity >= quantity)
                {
                    decimal unit_price = product.price;
                    decimal total_price = unit_price * quantity;

                    var invoiceDetail = new InvoiceDetail
                    {
                        invoice_id = newInvoice.invoice_id,
                        product_id = productid,
                        quantity = quantity,
                        unit_price = unit_price,
                        total_price = total_price
                    };

                    db.InvoiceDetails.Add(invoiceDetail);

                    product.stock_quantity -= quantity;
                    db.Entry(product).State = System.Data.Entity.EntityState.Modified;

                    total_amount += total_price;
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

        if (stockError)
        {
            db.Entry(newInvoice).State = System.Data.Entity.EntityState.Deleted;
            db.SaveChanges();
            var products = db.Products.ToList();
            ViewBag.Products = new SelectList(products, "product_id", "product_name");
            return View();
        }

        newInvoice.total_amount = total_amount;
        db.Entry(newInvoice).State = System.Data.Entity.EntityState.Modified;
        db.SaveChanges();

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

    // Get product details (price)
    public JsonResult GetProductDetails(int id)
    {
        var product = db.Products
            .Where(p => p.product_id == id)
            .Select(p => new
            {
                product = p.price
            })
            .FirstOrDefault();

        if (product != null)
        {
            return Json(product, JsonRequestBehavior.AllowGet);
        }

        return Json(null, JsonRequestBehavior.AllowGet);
    }
}
