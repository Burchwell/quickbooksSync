using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zombie;

namespace SkarAudioQBSync
{
    //This order object will hold all the data we need for creating a new order
    class SalesReceipt : QBSync
    {
        private String laravelOrderId;
        private String remoteOrderId;
        private Customer customer;
        private string paymentMethod;
        private List <Product> lineItems = new List<Product>();
        private Decimal taxAmount = 0;
        private Decimal shippingAmount = 0;
        private Decimal orderTotal = 0;
        private DateTime orderDate;

        public void setOrderDate(DateTime date)
        {
            this.orderDate = date;
        }

        public void setOrderTotal(Decimal total)
        {
            this.orderTotal = total;
        }
        public void setTaxAmount(Decimal amount)
        {
            this.taxAmount = amount;
        }

        public void setLaravelOrderId(string id)
        {
            this.laravelOrderId = id;
        }
        public string getLaravelOrderId()
        {
            return this.laravelOrderId;
        }
        public void setShippingAmount(Decimal amount)
        {
            this.shippingAmount = amount;
        }
        public void setPaymentMethod(string paymentMethod)
        {
            this.paymentMethod = paymentMethod;
        }
        public string getPaymentMethod()
        {
            return this.paymentMethod;
        }

        public void setRemoteOrderId(String orderId)
        {
            this.remoteOrderId = orderId;

        }
        public String getRemoteOrderId()
        {
            return this.remoteOrderId;
        }
        public void setCustomer(Customer customer)
        {
            this.customer = customer;
        }
        public Customer getCustomer()
        {
            return this.customer;
        }

        public int lineItemCount()
        {
            return lineItems.Count;
        }

        public SalesReceipt(Zombie.SDKConnection cn) : base(cn)
        {

        }

        public void addLineItem(Product lineItem)
        {
            try
            {
                this.lineItems.Add(lineItem);
            }catch(Exception e)
            {
                Console.WriteLine("whatt?");
            }
        }

        public bool createSalesReceipt()
        {

            Console.WriteLine("create sale");
            var batch = this.cn.NewBatch();
            var createSalesReceiptRq = batch.MsgSet.AppendSalesReceiptAddRq();
            createSalesReceiptRq.CustomerRef.ListID.SetValue(customer.getListID());
            createSalesReceiptRq.CustomerRef.FullName.SetValue(customer.getFullName());
            createSalesReceiptRq.Memo.SetValue("Created with Skar QBSync");
            createSalesReceiptRq.PaymentMethodRef.FullName.SetValue("Cash");
            createSalesReceiptRq.RefNumber.SetValue("T" + remoteOrderId.Substring(Math.Max(0, this.remoteOrderId.Length - 10)));
            //createSalesReceiptRq.RefNumber.SetValue("T" + (this.remoteOrderId.Length > 10 ? this.remoteOrderId.Substring(0, 10) : this.remoteOrderId));
            
            createSalesReceiptRq.TxnDate.SetValue(this.orderDate);

            createSalesReceiptRq.ShipAddress.Addr1.SetValue(Safe.LimitedString(customer.getOriginalFullName(),41));
            createSalesReceiptRq.ShipAddress.Addr2.SetValue(Safe.LimitedString(customer.ShippingAddress1, 41));
            createSalesReceiptRq.ShipAddress.Addr3.SetValue(Safe.LimitedString(customer.ShippingAddress2, 41));
            createSalesReceiptRq.ShipAddress.PostalCode.SetValue(Safe.LimitedString(customer.ShippingPostalCode,13));
            createSalesReceiptRq.ShipAddress.City.SetValue(Safe.LimitedString(customer.ShippingCity,31));
            createSalesReceiptRq.ShipAddress.State.SetValue(customer.ShippingState);
            createSalesReceiptRq.ShipAddress.Country.SetValue(Safe.LimitedString(customer.ShippingCountry,31));

            createSalesReceiptRq.BillAddress.Addr1.SetValue(Safe.LimitedString(customer.getOriginalFullName(), 41));
            createSalesReceiptRq.BillAddress.Addr2.SetValue(Safe.LimitedString(customer.BillingAddress1,41));
            createSalesReceiptRq.BillAddress.Addr3.SetValue(Safe.LimitedString(customer.BillingAddress2,41));
            createSalesReceiptRq.BillAddress.PostalCode.SetValue(Safe.LimitedString(customer.BillingPostalCode,13));
            createSalesReceiptRq.BillAddress.City.SetValue(Safe.LimitedString(customer.BillingCity,31));
            createSalesReceiptRq.BillAddress.State.SetValue(customer.BillingState);
            createSalesReceiptRq.BillAddress.Country.SetValue(Safe.LimitedString(customer.BillingCountry,31));

            Decimal num1 = new Decimal();
            foreach (Product lineItem in this.lineItems)
            {
                num1 += lineItem.getPrice();
            }
            if (num1 + this.taxAmount + this.shippingAmount != this.orderTotal)
            {
                Decimal num2 = Math.Abs(this.orderTotal - (num1 + this.taxAmount + this.shippingAmount));
                Console.WriteLine("Add Bundle Placeholder (channeladvisor bundle)");
                var addBundleItem = createSalesReceiptRq.ORSalesReceiptLineAddList.Append();
                addBundleItem.SalesReceiptLineAdd.ItemRef.FullName.SetValue("Bundle Revenue");
                addBundleItem.SalesReceiptLineAdd.Amount.SetValue((double)num2);
                addBundleItem.SalesReceiptLineAdd.Quantity.SetValue(1);
                addBundleItem.SalesReceiptLineAdd.Desc.SetValue("Bundle");
            }
            //Loop over the products, and add to the salesreceipt
            foreach (Product product in this.lineItems)
            {
                Console.WriteLine("is inventory item? :" + product.getIsNonInventoryItem());
                if (product.getIsNonInventoryItem() == 0)
                {
                    var saleItemList = createSalesReceiptRq.ORSalesReceiptLineAddList.Append();
                    if (product.getListID() != "")
                    {
                        saleItemList.SalesReceiptLineAdd.ItemRef.ListID.SetValue(product.getListID());
                        saleItemList.SalesReceiptLineAdd.Amount.SetValue((double)product.getPrice());
                        Console.WriteLine(product.getQuantity() + " : " + product.getListID());
                        saleItemList.SalesReceiptLineAdd.Quantity.SetValue((double)product.getQuantity());
                        saleItemList.SalesReceiptLineAdd.InventorySiteRef.FullName.SetValue(product.LocationName);
                        saleItemList.SalesReceiptLineAdd.Desc.SetValue(product.getName());
                    }
                    else
                    {
                        saleItemList.SalesReceiptLineAdd.ItemRef.FullName.SetValue(product.getName());
                        saleItemList.SalesReceiptLineAdd.Amount.SetValue((double)product.getPrice());
                        Console.WriteLine(product.getQuantity() + " : " + product.getName());
                        saleItemList.SalesReceiptLineAdd.Quantity.SetValue((double)product.getQuantity());
                        saleItemList.SalesReceiptLineAdd.Desc.SetValue(product.getName());
                    }
                }
                else
                {
                    Console.WriteLine("Add Non inventory line");
                    var nonInvLine = createSalesReceiptRq.ORSalesReceiptLineAddList.Append();
                    
                    nonInvLine.SalesReceiptLineAdd.ItemRef.FullName.SetValue(product.getSku());
                    nonInvLine.SalesReceiptLineAdd.Amount.SetValue((double) product.getPrice());
                    nonInvLine.SalesReceiptLineAdd.Quantity.SetValue(1);
                    nonInvLine.SalesReceiptLineAdd.Desc.SetValue(product.getName());
                }
            }

            if(this.shippingAmount > 0)
            {
                Console.WriteLine("Add shipping amount");
                var shippingAmountLine = createSalesReceiptRq.ORSalesReceiptLineAddList.Append();
                shippingAmountLine.SalesReceiptLineAdd.ItemRef.FullName.SetValue("Shipping Charge");
                shippingAmountLine.SalesReceiptLineAdd.Amount.SetValue((double) this.shippingAmount);
                shippingAmountLine.SalesReceiptLineAdd.Quantity.SetValue(1);
                shippingAmountLine.SalesReceiptLineAdd.Desc.SetValue("Shipping");
            }

            if (this.taxAmount > 0)
            {
                Console.WriteLine("Add tax amount");
                var taxAmountLine = createSalesReceiptRq.ORSalesReceiptLineAddList.Append();
                taxAmountLine.SalesReceiptLineAdd.ItemRef.FullName.SetValue("State Sales Tax");
                taxAmountLine.SalesReceiptLineAdd.Amount.SetValue((double) this.taxAmount);
                taxAmountLine.SalesReceiptLineAdd.Desc.SetValue("Tax");
            }


            //Add transaction info as line item
            var orderDesc = createSalesReceiptRq.ORSalesReceiptLineAddList.Append();
            orderDesc.SalesReceiptLineAdd.Amount.SetValue(0);
            orderDesc.SalesReceiptLineAdd.Quantity.SetValue(0);
            orderDesc.SalesReceiptLineAdd.Desc.SetValue("Payment method: " + this.paymentMethod);

            return batch.Run();
        }

    }
}
